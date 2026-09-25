using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

/// <summary>
/// Generates a procedural city layout on a virtual grid, then uses StructurePlacer to build it.
/// </summary>
[RequireComponent(typeof(StructurePlacer))]
public class ProceduralCityGenerator : MonoBehaviour
{
    public enum CellType { Empty, River, Wall, Gate, Road, Building, Tower }

    [Header("Map Dimensions (Must be multiples of 4)")]
    [FormerlySerializedAs("cityWidth")] public int mapWidth = 64;
    [FormerlySerializedAs("cityLength")] public int mapLength = 64;

    [Header("City Bounds (Must be multiples of 4)")]
    public int cityMinX = 16;
    public int cityMaxX = 48;
    public int cityMinY = 16;
    public int cityMaxY = 48;

    [Header("Terrain Settings")]
    [Tooltip("Drag a MapSeedObject here to test a saved terrain sample.")]
    public MapSeedObject mapSeedObject;
    [Tooltip("Number of tilemap tiles represented by one terrain-sample pixel.")]
    [Min(1)] public int terrainPpu = 4;
    [Tooltip("Terrain variants are selected with smooth Perlin noise within each sampled value.")]
    [Min(0.001f)] public float terrainNoiseScale = 0.15f;
    public int terrainNoiseSeed;
    public List<StructureData> waterTerrainChunks = new List<StructureData>();
    public List<StructureData> landTerrainChunks = new List<StructureData>();
    [Tooltip("Fallback used when the seed has no biome and landTerrainChunks is empty.")]
    public StructureData defaultLandChunk;

    [Header("4x4 Chunk Data")]
    public StructureData riverChunk; //4x4 water chunk for the river
    public StructureData wallChunk; //1x1 chunk for wall segments
    public StructureData towerChunk; //4x4 chunk used for wall towers
    public StructureData gatehouseChunk; //chunk used for the gatehouse structure
    public StructureData invertedGatehouseChunk; //chunk used on the left/right gatehouses.
    public StructureData roadChunk; //4x4 chunk for the large road
    public StructureData sideRoadChunk; //1x1 chunk that connects the main roads to placed structures.
    public StructureData bridgeChunk; //4x4 for the road crossing over the river.

    [Tooltip("How many tiles to skip between wall tower placements. Values are snapped to the 4x4 grid.")]
    [Min(4)] public int towerSpacing = 16;

    [Tooltip("If enabled, places additional towers at regular intervals along the walls.")]
    public bool enableIncrementalTowers = true;

    [Header("Wall Outcroppings")]
    public bool enableOutcroppings = true;
    [Tooltip("How many outcroppings to attempt to place across all walls.")]
    [Range(0,10)] public int numOutcroppings = 8;
    [Range(0,32)] public int minOutcroppingLength = 6;
    [Range(0, 32)]public int maxOutcroppingLength = 14;
    [Range(0,16)] public int minOutcroppingDepth = 3;
    [Range(0, 16)] public int maxOutcroppingDepth = 6;

    [Header("Buildings")]
    [SerializeField] bool walledCity = true;
    public List<StructureData> buildingPrefabs;
    [Tooltip("How many times to attempt placing a building before giving up.")]
    public int placementAttemptsPerBuilding = 50;
    public int totalBuildingsToSpawn = 30;

    private StructurePlacer placer;
    private CellType[,] cityGrid;
    private BiomePalette seedBiome;

    [SerializeField] private MapSeedObject testSeed;

    // A queue of instructions to hand to the Placer once the math is done
    private struct PlacementJob
    {
        public StructureData data;
        public Vector3Int position;
    }
    private List<PlacementJob> placementQueue = new List<PlacementJob>();

    private void Awake()
    {
        placer = GetComponent<StructurePlacer>();
    }
    public void GenerateCity(BattleMapSeedData seedData)
    {
        if (placer == null) placer = GetComponent<StructurePlacer>();

        if (seedData == null || seedData.terrainSample == null)
        {
            Debug.LogWarning("Cannot generate a city without BattleMapSeedData.terrainSample.");
            return;
        }

        seedBiome = seedData.biome;

        // Initialize empty grid
        cityGrid = new CellType[mapWidth, mapLength];
        placementQueue.Clear();

        // Clear existing tilemaps before generating
        foreach (var tm in placer.targetTilemaps)
        {
            if (tm != null) tm.ClearAllTiles();
        }

        // Terrain must claim water before walls, roads, and buildings are generated.
        GenerateTerrain(seedData);

        // Generate Layout Math
        if (walledCity) GenerateWallsAndGates();

        GenerateMainRoads();
        GenerateBuildings();

        // Execute Placements visually
        foreach (PlacementJob job in placementQueue)
        {
            placer.PlaceStructure(job.data, job.position);
        }

        Debug.Log($"City generated with {placementQueue.Count} total structures.");
    }

    [ContextMenu("Test Current Map Seed Object")]
    private void GenerateCityFromInspector()
    {
        MapSeedObject seedToTest = mapSeedObject != null ? mapSeedObject : testSeed;
        if (seedToTest == null)
        {
            Debug.LogWarning("Assign a MapSeedObject to either mapSeedObject or testSeed before testing the current map seed.");
            return;
        }

        Debug.Log($"Testing map seed object '{seedToTest.name}'.");
        BattleMapSeedData seedData = seedToTest.CreateSeedData();
        if (seedData == null)
        {
            Debug.LogWarning($"Map seed object '{seedToTest.name}' does not contain a valid terrain sample.");
            return;
        }

        GenerateCity(seedData);
    }

    private void GenerateTerrain(BattleMapSeedData seedData)
    {
        int[,] terrainSample = seedData.terrainSample;
        int sampleScale = Mathf.Max(1, terrainPpu);
        int sampleRegionCountX = Mathf.Max(1, Mathf.CeilToInt(mapWidth / (float)sampleScale));
        int sampleRegionCountY = Mathf.Max(1, Mathf.CeilToInt(mapLength / (float)sampleScale));
        List<StructureData> waterChunks = GetWaterTerrainChunks();
        List<StructureData> landChunks = GetLandTerrainChunks();

        if (waterChunks.Count == 0)
        {
            Debug.LogWarning("No water terrain chunks are assigned. Assign waterTerrainChunks or riverChunk.");
        }
        if (landChunks.Count == 0)
        {
            Debug.LogWarning("No land terrain chunks are assigned. Assign landTerrainChunks, a biome on the seed, or defaultLandChunk.");
        }

        // The city dimensions are independent from the sample dimensions. Each
        // sample value covers terrainPpu tiles, split into adjacent 4x4 chunks.
        for (int x = 0; x < mapWidth; x += 4)
        {
            for (int y = 0; y < mapLength; y += 4)
            {
                int regionX = x / sampleScale;
                int regionY = y / sampleScale;
                int sampleX = Mathf.Min(
                    Mathf.FloorToInt(regionX * terrainSample.GetLength(0) / (float)sampleRegionCountX),
                    terrainSample.GetLength(0) - 1);
                int sampleY = Mathf.Min(
                    Mathf.FloorToInt(regionY * terrainSample.GetLength(1) / (float)sampleRegionCountY),
                    terrainSample.GetLength(1) - 1);
                int terrainValue = terrainSample[sampleX, sampleY];
                bool isWater = terrainValue == 0;
                StructureData terrainChunk = SelectTerrainChunk(isWater, sampleX, sampleY, waterChunks, landChunks);

                if (isWater)
                {
                    ClaimGrid(x, y, 4, 4, CellType.River);
                    if (terrainChunk != null)
                    {
                        placementQueue.Add(new PlacementJob
                        {
                            data = terrainChunk,
                            position = new Vector3Int(x, y, 0)
                        });
                    }
                }
                else if (terrainChunk != null)
                {
                    placementQueue.Add(new PlacementJob
                    {
                        data = terrainChunk,
                        position = new Vector3Int(x, y, 0)
                    });
                }
            }
        }
    }

    private List<StructureData> GetLandTerrainChunks()
    {
        if (landTerrainChunks != null && landTerrainChunks.Count > 0)
        {
            return landTerrainChunks;
        }

        if (seedBiome != null && seedBiome.GroundComposition != null &&
            seedBiome.GroundComposition.tilePatterns != null &&
            seedBiome.GroundComposition.tilePatterns.Count > 0)
        {
            return seedBiome.GroundComposition.tilePatterns;
        }

        List<StructureData> fallback = new List<StructureData>();
        if (defaultLandChunk != null) fallback.Add(defaultLandChunk);

        if (fallback.Count == 0)
        {
            StructureData[] resourceChunks = Resources.LoadAll<StructureData>("StructureData/TerrainElements");
            foreach (StructureData resourceChunk in resourceChunks)
            {
                if (resourceChunk != null &&
                    resourceChunk.name.IndexOf("water", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    fallback.Add(resourceChunk);
                }
            }
        }

        return fallback;
    }

    private List<StructureData> GetWaterTerrainChunks()
    {
        if (waterTerrainChunks != null && waterTerrainChunks.Count > 0)
        {
            return waterTerrainChunks;
        }

        List<StructureData> fallback = new List<StructureData>();
        if (riverChunk != null) fallback.Add(riverChunk);

        if (fallback.Count == 0)
        {
            StructureData resourceChunk = Resources.Load<StructureData>(
                "StructureData/TerrainElements/WaterChunk_Data");
            if (resourceChunk != null) fallback.Add(resourceChunk);
        }

        return fallback;
    }

    private StructureData SelectTerrainChunk(
        bool isWater,
        int sampleX,
        int sampleY,
        List<StructureData> waterChunks,
        List<StructureData> landChunks)
    {
        List<StructureData> chunks = isWater ? waterChunks : landChunks;
        if (chunks == null || chunks.Count == 0) return null;

        float noise = Mathf.PerlinNoise(
            (sampleX + terrainNoiseSeed) * terrainNoiseScale,
            (sampleY + terrainNoiseSeed) * terrainNoiseScale);
        int index = Mathf.Clamp(Mathf.FloorToInt(noise * chunks.Count), 0, chunks.Count - 1);
        return chunks[index];
    }

    private void ClaimGrid(int startX, int startY, int width, int height, CellType type)
    {
        if (startX < 0 || startY < 0 || startX + width > mapWidth || startY + height > mapLength) return;

        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                cityGrid[x, y] = type;
            }
        }
    }

    private void GetCityBounds(out int minX, out int maxX, out int minY, out int maxY)
    {
        int maximumX = Mathf.Max(4, mapWidth - 4);
        int maximumY = Mathf.Max(4, mapLength - 4);

        minX = Mathf.Clamp(cityMinX - cityMinX % 4, 0, maximumX - 4);
        maxX = Mathf.Clamp(cityMaxX - cityMaxX % 4, minX + 4, maximumX);
        minY = Mathf.Clamp(cityMinY - cityMinY % 4, 0, maximumY - 4);
        maxY = Mathf.Clamp(cityMaxY - cityMaxY % 4, minY + 4, maximumY);
    }

    private void GenerateWallsAndGates()
    {
        GetCityBounds(out int minX, out int maxX, out int minY, out int maxY);

        int minMacroX = minX / 4;
        int maxMacroX = maxX / 4;
        int minMacroY = minY / 4;
        int maxMacroY = maxY / 4;

        //Find the midpoints for the gates (must remain snapped to 4x4)
        int midX = ((minMacroX + maxMacroX) / 2) * 4;
        int midY = ((minMacroY + maxMacroY) / 2) * 4;

        //Generate the Offset Arrays for the dynamic bastions/outcroppings
        int[] topOffsets = new int[maxX - minX + 1];
        int[] bottomOffsets = new int[maxX - minX + 1];
        int[] leftOffsets = new int[maxY - minY + 1];
        int[] rightOffsets = new int[maxY - minY + 1];

        if (enableOutcroppings)
        {
            for (int i = 0; i < numOutcroppings; i++)
            {
                int side = Random.Range(0, 4); // 0=Bottom, 1=Top, 2=Left, 3=Right
                int length = Random.Range(minOutcroppingLength, maxOutcroppingLength + 1);
                int depth = Random.Range(minOutcroppingDepth, maxOutcroppingDepth + 1);
                
                int arrayLen = (side == 0 || side == 1) ? bottomOffsets.Length : leftOffsets.Length;
                
                // Keep 4 tiles clear from the corners to prevent weird overlaps
                if (arrayLen - length - 4 <= 4) continue; // Array too small for this feature
                int startIdx = Random.Range(4, arrayLen - length - 4);
                
                // Protect the gatehouses! (Give them a 2 tile buffer)
                int gateStart = (side == 0 || side == 1) ? midX - minX : midY - minY;
                int gateEnd = gateStart + 4;
                if (startIdx < gateEnd + 2 && startIdx + length > gateStart - 2) continue;
                
                // Apply the depth offset to the array
                int[] targetArray = side == 0 ? bottomOffsets : side == 1 ? topOffsets : side == 2 ? leftOffsets : rightOffsets;
                for (int j = startIdx; j < startIdx + length; j++)
                {
                    targetArray[j] = Mathf.Max(targetArray[j], depth);
                }
            }
        }

        // bottom wall
        int prevY_Bottom = minY - bottomOffsets[0];
        for (int x = minX; x <= maxX; x++) 
        {
            int currentY = minY - bottomOffsets[x - minX];

            if (x >= midX && x < midX + 4)
            {
                if (x == midX) MarkGridAndQueue(x, minY, 4, 4, CellType.Gate, gatehouseChunk);
                prevY_Bottom = minY;
                continue;
            }

            // Draw vertical 90-degree connecting walls if the offset changed
            if (currentY < prevY_Bottom) 
            {
                // Stepped outward (down)
                for (int stepY = prevY_Bottom - 1; stepY >= currentY; stepY--)
                    if (cityGrid[x - 1, stepY] != CellType.River) MarkGridAndQueue(x - 1, stepY, 1, 1, CellType.Wall, wallChunk);
            }
            else if (currentY > prevY_Bottom) 
            {
                // Stepped inward (up)
                for (int stepY = prevY_Bottom; stepY < currentY; stepY++)
                    if (cityGrid[x, stepY] != CellType.River) MarkGridAndQueue(x, stepY, 1, 1, CellType.Wall, wallChunk);
            }

            // Draw horizontal wall segment
            if (cityGrid[x, currentY] != CellType.River) MarkGridAndQueue(x, currentY, 1, 1, CellType.Wall, wallChunk);
            prevY_Bottom = currentY;
        }

        // top wall
        int prevY_Top = maxY + topOffsets[0];
        for (int x = minX; x <= maxX; x++) 
        {
            int currentY = maxY + topOffsets[x - minX];

            if (x >= midX && x < midX + 4)
            {
                if (x == midX) MarkGridAndQueue(x, maxY, 4, 4, CellType.Gate, gatehouseChunk);
                prevY_Top = maxY;
                continue;
            }

            if (currentY > prevY_Top) 
            {
                for (int stepY = prevY_Top + 1; stepY <= currentY; stepY++)
                    if (cityGrid[x - 1, stepY] != CellType.River) MarkGridAndQueue(x - 1, stepY, 1, 1, CellType.Wall, wallChunk);
            }
            else if (currentY < prevY_Top) 
            {
                for (int stepY = prevY_Top; stepY > currentY; stepY--)
                    if (cityGrid[x, stepY] != CellType.River) MarkGridAndQueue(x, stepY, 1, 1, CellType.Wall, wallChunk);
            }

            if (cityGrid[x, currentY] != CellType.River) MarkGridAndQueue(x, currentY, 1, 1, CellType.Wall, wallChunk);
            prevY_Top = currentY;
        }

        //left wall
        int prevX_Left = minX; // Safe to start at minX because corners are protected from offsets
        for (int y = minY + 1; y < maxY; y++) // Start at +1 to avoid corner overlap
        {
            int currentX = minX - leftOffsets[y - minY];

            if (y >= midY && y < midY + 4)
            {
                if (y == midY) MarkGridAndQueue(minX, y, 4, 4, CellType.Gate, invertedGatehouseChunk != null ? invertedGatehouseChunk : gatehouseChunk);
                prevX_Left = minX;
                continue;
            }

            if (currentX < prevX_Left)
            {
                for (int stepX = prevX_Left - 1; stepX >= currentX; stepX--)
                    if (cityGrid[stepX, y - 1] != CellType.River) MarkGridAndQueue(stepX, y - 1, 1, 1, CellType.Wall, wallChunk);
            }
            else if (currentX > prevX_Left)
            {
                for (int stepX = prevX_Left; stepX < currentX; stepX++)
                    if (cityGrid[stepX, y] != CellType.River) MarkGridAndQueue(stepX, y, 1, 1, CellType.Wall, wallChunk);
            }

            if (cityGrid[currentX, y] != CellType.River) MarkGridAndQueue(currentX, y, 1, 1, CellType.Wall, wallChunk);
            prevX_Left = currentX;
        }

        //right wall
        int prevX_Right = maxX; 
        for (int y = minY + 1; y < maxY; y++) 
        {
            int currentX = maxX + rightOffsets[y - minY];

            if (y >= midY && y < midY + 4)
            {
                if (y == midY) MarkGridAndQueue(maxX, y, 4, 4, CellType.Gate, invertedGatehouseChunk != null ? invertedGatehouseChunk : gatehouseChunk);
                prevX_Right = maxX;
                continue;
            }

            if (currentX > prevX_Right)
            {
                for (int stepX = prevX_Right + 1; stepX <= currentX; stepX++)
                    if (cityGrid[stepX, y - 1] != CellType.River) MarkGridAndQueue(stepX, y - 1, 1, 1, CellType.Wall, wallChunk);
            }
            else if (currentX < prevX_Right)
            {
                for (int stepX = prevX_Right; stepX > currentX; stepX--)
                    if (cityGrid[stepX, y] != CellType.River) MarkGridAndQueue(stepX, y, 1, 1, CellType.Wall, wallChunk);
            }

            if (cityGrid[currentX, y] != CellType.River) MarkGridAndQueue(currentX, y, 1, 1, CellType.Wall, wallChunk);
            prevX_Right = currentX;
        }

        GenerateWallTowers(minX, maxX, minY, maxY, midX, midY);
    }

    private void GenerateWallTowers(int minX, int maxX, int minY, int maxY, int midX, int midY)
    {
        if (towerChunk == null) return;

        int spacing = Mathf.Max(4, towerSpacing);
        spacing -= spacing % 4;
        if (spacing < 4) spacing = 4;

        PlaceTower(minX, minY);
        PlaceTower(maxX, minY);
        PlaceTower(minX, maxY);
        PlaceTower(maxX, maxY);

        if (!enableIncrementalTowers) return;

        for (int x = minX + spacing; x < maxX; x += spacing)
        {
            if (x >= midX && x < midX + 4) continue;
            PlaceTower(x, minY);
            PlaceTower(x, maxY);
        }

        for (int y = minY + spacing; y < maxY; y += spacing)
        {
            if (y >= midY && y < midY + 4) continue;
            PlaceTower(minX, y);
            PlaceTower(maxX, y);
        }
    }

    private void PlaceTower(int startX, int startY)
    {
        if (towerChunk == null) return;

        for (int x = startX; x < startX + 4; x++)
        {
            for (int y = startY; y < startY + 4; y++)
            {
                if (x < 0 || y < 0 || x >= mapWidth || y >= mapLength) return;

                if (cityGrid[x, y] == CellType.River || cityGrid[x, y] == CellType.Gate || cityGrid[x, y] == CellType.Road || cityGrid[x, y] == CellType.Building || cityGrid[x, y] == CellType.Tower)
                {
                    return;
                }
            }
        }

        MarkGridAndQueue(startX, startY, 4, 4, CellType.Tower, towerChunk);
    }

    private void GenerateMainRoads()
    {
        GetCityBounds(out int cityMin, out int cityMax, out int cityBottom, out int cityTop);
        int midX = ((cityMin / 4 + cityMax / 4) / 2) * 4;
        int midY = ((cityBottom / 4 + cityTop / 4) / 2) * 4;

        // Roads start just inside the gates
        int minX = cityMin + 4;
        int maxX = cityMax - 4;
        int minY = cityBottom + 4;
        int maxY = cityTop - 4;

        // Vertical Road
        for (int y = minY; y <= maxY; y += 4)
        {
            if (cityGrid[midX, y] == CellType.River)
                MarkGridAndQueue(midX, y, 4, 4, CellType.Road, bridgeChunk); // Bridge over river
            else if (cityGrid[midX, y] == CellType.Empty)
                MarkGridAndQueue(midX, y, 4, 4, CellType.Road, roadChunk);
        }

        // Horizontal Road
        for (int x = minX; x <= maxX; x += 4)
        {
            if (cityGrid[x, midY] == CellType.River)
                MarkGridAndQueue(x, midY, 4, 4, CellType.Road, bridgeChunk); // Bridge over river
            else if (cityGrid[x, midY] == CellType.Empty)
                MarkGridAndQueue(x, midY, 4, 4, CellType.Road, roadChunk);
        }
    }

    private void GenerateBuildings()
    {
        if (buildingPrefabs == null || buildingPrefabs.Count == 0) return;

        // Clean out any empty Inspector slots to prevent Null Reference Exceptions
        buildingPrefabs.RemoveAll(prefab => prefab == null);

        // Sort buildings from largest footprint to smallest. 
        // Placing big buildings first is the secret to good procedural generation!
        buildingPrefabs.Sort((StructureData a, StructureData b) => 
        {
            float areaA = a.footprint.x * a.footprint.y;
            float areaB = b.footprint.x * b.footprint.y;
            return areaB.CompareTo(areaA);
        });

        int buildingsPlaced = 0;

        for (int i = 0; i < totalBuildingsToSpawn; i++)
        {
            // Pick a random building from our list
            StructureData building = buildingPrefabs[Random.Range(0, buildingPrefabs.Count)];
            int w = (int)building.footprint.x;
            int h = (int)building.footprint.y;

            // Try to find a spot N times
            for (int attempt = 0; attempt < placementAttemptsPerBuilding; attempt++)
            {
                // Pick a random internal coordinate (inside the walls)
                int x = Random.Range(cityMinX + 4, cityMaxX - 4 - w + 1);
                int y = Random.Range(cityMinY + 4, cityMaxY - 4 - h + 1);

                if (CheckAreaEmpty(x, y, w, h))
                {
                    // Success! It fits perfectly.
                    MarkGridAndQueue(x, y, w, h, CellType.Building, building);
                    buildingsPlaced++;
                    break; 
                }
            }
            
            // If we couldn't place it after many attempts, the city might be getting full.
        }
    }

    /// <summary>
    /// Generates side roads that connect to main and lead to buildings. May also connect to another side path, creating alleyways.
    /// </summary>
    private void GenerateSmallRoads(List<RectInt> buildings)
    {
        if(sideRoadChunk == null) return;

        //foreach(RectInt)
    }

    /// <summary>
    /// Checks if a mathematical rectangle on the grid is entirely empty.
    /// </summary>
    private bool CheckAreaEmpty(int startX, int startY, int width, int height)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                if (cityGrid[x, y] != CellType.Empty) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Claims the space on the mathematical grid, and adds the structure to the placement queue.
    /// </summary>
    private void MarkGridAndQueue(int startX, int startY, int width, int height, CellType type, StructureData data)
    {
        if (data == null) return;

        // Prevent arrays from going out of bounds if an extreme offset pushes placement too far!
        if (startX < 0 || startY < 0 || startX + width > mapWidth || startY + height > mapLength) return;

        //Claim the area on our virtual math grid so nothing else spawns here
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                cityGrid[x, y] = type;
            }
        }

        //Add to queue for the Placer to handle visually later
        placementQueue.Add(new PlacementJob
        {
            data = data,
            position = new Vector3Int(startX, startY, 0)
        });
    }
}