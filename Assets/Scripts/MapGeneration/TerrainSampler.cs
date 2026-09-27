using UnityEngine;

public class TerrainSampler : MonoBehaviour
{
    [Header("Logic Texture")]
    [Tooltip("Readable texture containing every terrain color used by the campaign map.")]
    [SerializeField] private Texture2D logicLayerTexture;
    [Tooltip("World-space position of the texture's bottom-left pixel.")]
    [SerializeField] private Vector2 logicTextureWorldOrigin;
    [Tooltip("Flip the texture vertically when its source image uses a top-left origin.")]
    [SerializeField] private bool flipTextureY;
    [Min(0.001f)] [SerializeField] private float campaignPixelsPerUnit = 16f;

    [Tooltip("How much tolerance in an RGB value counts for terrain sampling.")]
    [SerializeField] private float colorTolerance = 15f;
    [SerializeField] private int testSampleSizeN;

    [Header("Map Seed")]
    [Tooltip("Biome stored with the next MapSeedObject created from this sampler.")]
    [SerializeField] private BiomePalette seedBiome;

    public bool showPixelGridLines = true;
    public bool showSampledDataOverlay = true;
    [Range(0.1f, 0.9f)] public float overlayOpacity = 0.45f;

    private int[,] _lastSampledGrid;
    private Vector2 _lastSampledCenter;
    private int _lastSampledN;

    [ContextMenu("Test Sample At Current Position")]
    public void TestSampleContextMenu()
    {
        _lastSampledGrid = SampleSquareToGrid(transform.position, testSampleSizeN);
        int landCount = 0;
        for (int y = 0; y < _lastSampledN; y++)
            for (int x = 0; x < _lastSampledN; x++)
                if (_lastSampledGrid[x, y] == 1) landCount++;

        Debug.Log($"[MapSampler] Sampled {_lastSampledN}x{_lastSampledN}: " +
                  $"{landCount} Land (1) pixels, " +
                  $"{(_lastSampledN * _lastSampledN) - landCount} other pixels.");
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    [ContextMenu("Clear Sampled Gizmo Overlay")]
    public void ClearSampledOverlay()
    {
        _lastSampledGrid = null;
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    [ContextMenu("Save Most Recent Sample As Map Seed")]
    private void SaveMostRecentSampleAsMapSeed()
    {
        if (_lastSampledGrid == null || _lastSampledN <= 0)
        {
            Debug.LogWarning("There is no recent terrain sample to save.");
            return;
        }
#if UNITY_EDITOR
        const string folderPath = "Assets/GeneratedMapSeeds";
        if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
            UnityEditor.AssetDatabase.CreateFolder("Assets", "GeneratedMapSeeds");

        MapSeedObject mapSeed = ScriptableObject.CreateInstance<MapSeedObject>();
        mapSeed.SetSeedData(new BattleMapSeedData(_lastSampledGrid, seedBiome, null));
        string assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
            $"{folderPath}/MapSeed_{_lastSampledN}x{_lastSampledN}.asset");
        UnityEditor.AssetDatabase.CreateAsset(mapSeed, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.EditorUtility.FocusProjectWindow();
        UnityEditor.Selection.activeObject = mapSeed;
        Debug.Log($"Saved map seed as {assetPath}.");
#else
        Debug.LogWarning("Saving MapSeedObject assets is only available in the Unity Editor.");
#endif
    }

    private void OnDrawGizmos()
    {
        if (campaignPixelsPerUnit <= 0f || testSampleSizeN <= 0) return;

        float pixelSize = 1f / campaignPixelsPerUnit;
        float sampleSize = testSampleSizeN * pixelSize;
        Vector3 center = transform.position;
        Vector3 bottomLeft = center - new Vector3(sampleSize * 0.5f, sampleSize * 0.5f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(sampleSize, sampleSize, 0.01f));

        if (showPixelGridLines && testSampleSizeN <= 128)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            for (int i = 1; i < testSampleSizeN; i++)
            {
                float offset = i * pixelSize;
                Gizmos.DrawLine(bottomLeft + new Vector3(offset, 0f), bottomLeft + new Vector3(offset, sampleSize));
                Gizmos.DrawLine(bottomLeft + new Vector3(0f, offset), bottomLeft + new Vector3(sampleSize, offset));
            }
        }

        if (!showSampledDataOverlay || _lastSampledGrid == null || _lastSampledN <= 0) return;

        float sampledSize = _lastSampledN * pixelSize;
        Vector3 sampledBottomLeft = new Vector3(
            _lastSampledCenter.x - sampledSize * 0.5f,
            _lastSampledCenter.y - sampledSize * 0.5f,
            0f);
        Vector3 cellSize = new Vector3(pixelSize * 0.9f, pixelSize * 0.9f, 0.01f);

        for (int y = 0; y < _lastSampledN; y++)
        {
            for (int x = 0; x < _lastSampledN; x++)
            {
                Gizmos.color = GetOverlayColor(_lastSampledGrid[x, y]);
                Vector3 cellCenter = sampledBottomLeft + new Vector3(
                    (x + 0.5f) * pixelSize, (y + 0.5f) * pixelSize, 0f);
                Gizmos.DrawCube(cellCenter, cellSize);
            }
        }
    }

    public int[,] TestTerrainSampleAtPosition(Vector2 position, int sampleSize)
    {
        return SampleSquareToGrid(position, sampleSize);
    }

    public int[,] SampleSquareToGrid(Vector2 worldCenter, int n)
    {
        n = Mathf.Max(1, n);
        int[,] grid = new int[n, n];
        if (logicLayerTexture == null)
        {
            Debug.LogError("Assign a logicLayerTexture before sampling terrain.");
            return grid;
        }

        float pixelsPerUnit = Mathf.Max(0.001f, campaignPixelsPerUnit);
        int outOfBoundsPixels = 0;

        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float worldX = worldCenter.x + (x - n * 0.5f + 0.5f) / pixelsPerUnit;
                float worldY = worldCenter.y + (y - n * 0.5f + 0.5f) / pixelsPerUnit;
                int textureX = Mathf.FloorToInt((worldX - logicTextureWorldOrigin.x) * pixelsPerUnit);
                int textureY = Mathf.FloorToInt((worldY - logicTextureWorldOrigin.y) * pixelsPerUnit);
                bool outsideTexture = textureX < 0 || textureY < 0 ||
                    textureX >= logicLayerTexture.width || textureY >= logicLayerTexture.height;
                if (outsideTexture)
                {
                    outOfBoundsPixels++;
                    grid[x, y] = 0;
                    continue;
                }

                if (flipTextureY) textureY = logicLayerTexture.height - 1 - textureY;
                grid[x, y] = ClassifyPixel(logicLayerTexture.GetPixel(textureX, textureY));
            }
        }

        if (outOfBoundsPixels > 0)
        {
            Debug.LogWarning($"[MapSampler] {outOfBoundsPixels}/{n * n} sampled pixels were outside " +
                             "logicLayerTexture and defaulted to water. Check logicTextureWorldOrigin.");
        }

        _lastSampledGrid = grid;
        _lastSampledCenter = worldCenter;
        _lastSampledN = n;
        return grid;
    }

    //determines what type of gen based on the color of the pixel
    private int ClassifyPixel(Color pixel)
    {
        float tolerance = colorTolerance / 255f;
        bool red = pixel.r >= 1f - tolerance && pixel.g <= tolerance && pixel.b <= tolerance;
        bool green = pixel.r <= tolerance && pixel.g >= 1f - tolerance && pixel.b <= tolerance;
        bool blue = pixel.r <= tolerance && pixel.g <= tolerance && pixel.b >= 1f - tolerance;
        bool white = pixel.r >= 1f - tolerance && pixel.g >= 1f - tolerance && pixel.b >= 1f - tolerance;
        bool black = pixel.r <= tolerance && pixel.g <= tolerance && pixel.b <= tolerance;
        bool yellow = pixel.r >= 1f - tolerance && pixel.g >= 1f - tolerance && pixel.b <= tolerance;
        bool pink = pixel.r >= 1f - tolerance && pixel.g <= tolerance && pixel.b >= 1f - tolerance;

        if (blue) return 0; //water
        if (white) return 1; //land
        if (black) return 2; //road
        if (red) return 3; //city origin
        if (green) return 4; //farm
        if (yellow) return 5; //mountain
        if (pink) return 6; //bridge
        return 1;
    }
    
    
    private Color GetOverlayColor(int terrainType)
    {
        Color color = terrainType switch
        {
            0 => new Color(0f, 0.5f, 1f),
            1 => new Color(0.1f, 0.9f, 0.2f),
            2 => Color.gray,
            3 => Color.red,
            4 => Color.green,
            5 => Color.yellow,
            6 => new Color(1f, 0f, 1f),
            _ => Color.white
        };
        color.a = overlayOpacity;
        return color;
    }
}
