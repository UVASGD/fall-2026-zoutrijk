using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generates a procedural city layout on a virtual grid, then uses StructurePlacer to build it.
/// </summary>
[RequireComponent(typeof(StructurePlacer))]
public class ProceduralCityGenerator : MonoBehaviour
{
    public enum CellType { Empty, River, Wall, Gate, Road, Building, Tower }
    public enum RiverDirection { North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest }

    [Header("City Dimensions (Must be multiples of 4)")]
    public int cityWidth = 64;
    public int cityLength = 64;

    [Header("Outskirt and Terrain Settings")]
    [SerializeField] Vector2Int landSize;
    [SerializeField] BiomePalette biomePalette;

    [Header("River Settings")]
    [SerializeField] bool riverEnabled = true;
    public RiverDirection riverDirection = RiverDirection.North;
    [Range(0f, 1f)]
    [Tooltip("Probability of the river turning perpendicularly. Diagonals meander automatically.")]
    public float riverMeanderChance = 0.2f;

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

    [ContextMenu("Generate City")]
    public void GenerateCity()
    {
        if (placer == null) placer = GetComponent<StructurePlacer>();

        // Initialize empty grid
        cityGrid = new CellType[cityWidth, cityLength];
        placementQueue.Clear();

        // Clear existing tilemaps before generating
        foreach (var tm in placer.targetTilemaps)
        {
            if (tm != null) tm.ClearAllTiles();
        }

        // Generate Layout Math
        if (riverEnabled) GenerateRiver();
        if (walledCity) GenerateWallsAndGates();
        
        if (biomePalette != null && biomePalette.GroundComposition != null && biomePalette.GroundComposition.tilePatterns != null)
        {
            GenerateGround();
        }

        GenerateMainRoads();
        GenerateBuildings();

        // Execute Placements visually
        foreach (PlacementJob job in placementQueue)
        {
            placer.PlaceStructure(job.data, job.position);
        }

        Debug.Log($"City generated with {placementQueue.Count} total structures.");
    }

    private void GenerateGround()
    {
        if (biomePalette == null || biomePalette.GroundComposition == null) return;

        var patterns = biomePalette.GroundComposition.tilePatterns;
        var percentages = biomePalette.GroundComposition.percentages;

        // Safety check to ensure arrays align
        if (patterns == null || percentages == null || patterns.Count == 0 || patterns.Count != percentages.Count)
        {
            Debug.LogWarning("BiomePalette tile patterns and percentages are mismatched or empty.");
            return;
        }

        // If landSize is left at (0,0), safely fallback to the city bounds
        int width = landSize.x > 0 ? landSize.x : cityWidth;
        int length = landSize.y > 0 ? landSize.y : cityLength;

        // Create a temporary list to hold ground jobs
        List<PlacementJob> groundJobs = new List<PlacementJob>();

        // Iterate through the terrain footprint in 4x4 chunks
        for (int x = 0; x < width; x += 4)
        {
            for (int y = 0; y < length; y += 4)
            {
                // Ensure we don't check outside the internal city bounds
                bool isRiver = false;
                if (x < cityWidth && y < cityLength)
                {
                    // Since both ground and river chunks are aligned to the 4x4 grid, 
                    // checking the origin tile of the chunk is sufficient.
                    if (cityGrid[x, y] == CellType.River)
                    {
                        isRiver = true;
                    }
                }

                // If a river exists here, do not place ground so the lower layer shows through
                if (isRiver) continue;

                // Roll a weighted random number (0 to 99)
                int roll = Random.Range(0, 100);
                int cumulative = 0;
                StructureData selectedPattern = patterns[0]; // Fallback to first pattern

                for (int i = 0; i < percentages.Count; i++)
                {
                    cumulative += percentages[i];
                    if (roll < cumulative)
                    {
                        selectedPattern = patterns[i];
                        break;
                    }
                }

                if (selectedPattern != null)
                {
                    // Add to our temporary ground queue
                    groundJobs.Add(new PlacementJob
                    {
                        data = selectedPattern,
                        position = new Vector3Int(x, y, 0)
                    });
                }
            }
        }

        //Stamp the ground first, then add the rest of the placement queue on top of it. This ensures that ground tiles are always below other structures.
        groundJobs.AddRange(placementQueue);
        placementQueue = groundJobs;
    }

    private void GenerateRiver()
    {
        if (!riverEnabled) return;

        int startX = 0, startY = 0;
        
        // Calculate the maximum chunk index limits
        int maxW = (cityWidth / 4) - 1;
        int maxH = (cityLength / 4) - 1;

        // Determine starting location based on overall flow direction
        switch (riverDirection)
        {
            case RiverDirection.North:
                startX = Random.Range(2, maxW - 1) * 4;
                startY = 0;
                break;
            case RiverDirection.South:
                startX = Random.Range(2, maxW - 1) * 4;
                startY = maxH * 4;
                break;
            case RiverDirection.East:
                startX = 0;
                startY = Random.Range(2, maxH - 1) * 4;
                break;
            case RiverDirection.West:
                startX = maxW * 4;
                startY = Random.Range(2, maxH - 1) * 4;
                break;
            case RiverDirection.NorthEast:
                if (Random.value < 0.5f) { startX = Random.Range(0, maxW / 4) * 4; startY = 0; }
                else { startX = 0; startY = Random.Range(0, maxH / 4) * 4; }
                break;
            case RiverDirection.NorthWest:
                if (Random.value < 0.5f) { startX = Random.Range(maxW - (maxW / 4), maxW) * 4; startY = 0; }
                else { startX = maxW * 4; startY = Random.Range(0, maxH / 4) * 4; }
                break;
            case RiverDirection.SouthEast:
                if (Random.value < 0.5f) { startX = Random.Range(0, maxW / 4) * 4; startY = maxH * 4; }
                else { startX = 0; startY = Random.Range(maxH - (maxH / 4), maxH) * 4; }
                break;
            case RiverDirection.SouthWest:
                if (Random.value < 0.5f) { startX = Random.Range(maxW - (maxW / 4), maxW) * 4; startY = maxH * 4; }
                else { startX = maxW * 4; startY = Random.Range(maxH - (maxH / 4), maxH) * 4; }
                break;
        }

        int x = startX;
        int y = startY;
        Vector2Int lastStep = Vector2Int.zero;
        int failsafe = 0;

        // Plot the river course until it wanders completely off the grid limits
        while (x >= 0 && x < cityWidth && y >= 0 && y < cityLength && failsafe < 1000)
        {
            failsafe++;

            // Claim the 4x4 spot if it hasn't been claimed by overlapping river bends
            if (cityGrid[x, y] != CellType.River)
            {
                MarkGridAndQueue(x, y, 4, 4, CellType.River, riverChunk);
            }

            Vector2Int step = Vector2Int.zero;
            int attempt = 0;
            do
            {
                step = GetNextRiverStep(riverDirection);
                attempt++;
            }
            while (attempt < 10 && step == -lastStep); // Prevent it from instantly turning 180 degrees back on itself

            lastStep = step;
            x += step.x;
            y += step.y;
        }
    }

    private Vector2Int GetNextRiverStep(RiverDirection dir)
    {
        float r = Random.value;

        switch (dir)
        {
            case RiverDirection.North:
                if (r < riverMeanderChance / 2f) return new Vector2Int(-4, 0); // Wobble Left
                if (r < riverMeanderChance) return new Vector2Int(4, 0);      // Wobble Right
                return new Vector2Int(0, 4);                                  // Main direction

            case RiverDirection.South:
                if (r < riverMeanderChance / 2f) return new Vector2Int(-4, 0);
                if (r < riverMeanderChance) return new Vector2Int(4, 0);
                return new Vector2Int(0, -4);

            case RiverDirection.East:
                if (r < riverMeanderChance / 2f) return new Vector2Int(0, -4);
                if (r < riverMeanderChance) return new Vector2Int(0, 4);
                return new Vector2Int(4, 0);

            case RiverDirection.West:
                if (r < riverMeanderChance / 2f) return new Vector2Int(0, -4);
                if (r < riverMeanderChance) return new Vector2Int(0, 4);
                return new Vector2Int(-4, 0);

            // Diagonals meander naturally by randomly deciding which orthogonal step to take next.
            // This naturally produces a "staircase" path moving in the diagonal direction!
            case RiverDirection.NorthEast:
                return Random.value < 0.5f ? new Vector2Int(4, 0) : new Vector2Int(0, 4);
            case RiverDirection.NorthWest:
                return Random.value < 0.5f ? new Vector2Int(-4, 0) : new Vector2Int(0, 4);
            case RiverDirection.SouthEast:
                return Random.value < 0.5f ? new Vector2Int(4, 0) : new Vector2Int(0, -4);
            case RiverDirection.SouthWest:
                return Random.value < 0.5f ? new Vector2Int(-4, 0) : new Vector2Int(0, -4);
        }

        return new Vector2Int(0, 4); // Fallback
    }

    private void GenerateWallsAndGates()
    {
        //Calculate macro chunk boundary coordinates (Multiples of 4)
        int minMacroX = 4;
        int maxMacroX = (cityWidth / 4) - 2; // Last chunk index available for walls
        int minMacroY = 4;
        int maxMacroY = (cityLength / 4) - 2;

        //Convert those chunk origins into absolute tile coordinates
        int minX = minMacroX * 4;
        int maxX = maxMacroX * 4;
        int minY = minMacroY * 4;
        int maxY = maxMacroY * 4;

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
                if (x < 0 || y < 0 || x >= cityWidth || y >= cityLength) return;

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
        // Calculate macro chunk boundary coordinates (identical to walls)
        int minMacroX = 4;
        int maxMacroX = (cityWidth / 4) - 2;
        int minMacroY = 4;
        int maxMacroY = (cityLength / 4) - 2;

        // Find the exact same midpoints
        int midX = ((minMacroX + maxMacroX) / 2) * 4;
        int midY = ((minMacroY + maxMacroY) / 2) * 4;

        // Roads start just inside the gates
        int minX = (minMacroX + 1) * 4;
        int maxX = (maxMacroX - 1) * 4;
        int minY = (minMacroY + 1) * 4;
        int maxY = (maxMacroY - 1) * 4;

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

            bool placed = false;

            // Try to find a spot N times
            for (int attempt = 0; attempt < placementAttemptsPerBuilding; attempt++)
            {
                // Pick a random internal coordinate (inside the walls)
                int x = Random.Range(8, cityWidth - 8 - w);
                int y = Random.Range(8, cityLength - 8 - h);

                if (CheckAreaEmpty(x, y, w, h))
                {
                    // Success! It fits perfectly.
                    MarkGridAndQueue(x, y, w, h, CellType.Building, building);
                    placed = true;
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
        if (startX < 0 || startY < 0 || startX + width > cityWidth || startY + height > cityLength) return;

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