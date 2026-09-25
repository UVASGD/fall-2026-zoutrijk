using System.Collections.Generic;
using UnityEngine;

public class TerrainSampler : MonoBehaviour
{
    private string samplerLayerName = "Sampler";

    public string[] targetSortingLayers = new string[] { "RegionSprite" };

    [SerializeField] private float campaignPixelsPerUnit = 16f;

    [Tooltip("How much tolerance in an RGB value counts for terrain sampling")]
    [SerializeField] private float colorTolerance = 15f;

    [SerializeField] private int testSampleSizeN;

    private Camera _sampleCam; //auto-created in script

    public bool showPixelGridLines = true;

    [Tooltip("Draw the last sampled 0 (Water) and 1 (Land) results as colored tiles.")]
    public bool showSampledDataOverlay = true;

    [Range(0.1f, 0.9f)]
    public float overlayOpacity = 0.45f;

    // Cached state from the most recent sample so OnDrawGizmos can render it in Edit Mode
    private int[,] _lastSampledGrid;
    private Vector2 _lastSampledCenter;
    private int _lastSampledN;

    [ContextMenu("Test Sample At Current Position")]
    public void TestSampleContextMenu()
    {
        Vector2 center = transform.position;
        _lastSampledGrid = SampleSquareToGrid(center, testSampleSizeN);
        _lastSampledCenter = center;
        _lastSampledN = testSampleSizeN;

        int landCount = 0;
        for (int y = 0; y < testSampleSizeN; y++)
            for (int x = 0; x < testSampleSizeN; x++)
                if (_lastSampledGrid[x, y] == 1) landCount++;

        Debug.Log($"[MapSampler] Sampled {testSampleSizeN}x{testSampleSizeN} at {center}: " +
                  $"{landCount} Land (1) pixels, {(testSampleSizeN * testSampleSizeN) - landCount} Water (0) pixels.");

#if UNITY_EDITOR
        // force a repaint so new gizmos appear if in editor
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

    private void OnDrawGizmos()
    {
        if (campaignPixelsPerUnit <= 0f || testSampleSizeN <= 0) return;

        float pixelWorldSize = 1f / campaignPixelsPerUnit;
        float totalWorldWidth = testSampleSizeN * pixelWorldSize;

        Vector3 liveCenter = new Vector3(transform.position.x, transform.position.y, 0f);
        Vector3 boxSize = new Vector3(totalWorldWidth, totalWorldWidth, 0.01f);

        //draw yellow outer bounds
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(liveCenter, boxSize);

        //draw light white gridlines
        if (showPixelGridLines && testSampleSizeN <= 128) // Cap at 128 so Scene View doesn't lag
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            Vector3 bottomLeft = liveCenter - new Vector3(totalWorldWidth * 0.5f, totalWorldWidth * 0.5f, 0f);

            for (int i = 1; i < testSampleSizeN; i++)
            {
                float offset = i * pixelWorldSize;

                Gizmos.DrawLine(
                    bottomLeft + new Vector3(offset, 0f, 0f),
                    bottomLeft + new Vector3(offset, totalWorldWidth, 0f)
                );

                Gizmos.DrawLine(
                    bottomLeft + new Vector3(0f, offset, 0f),
                    bottomLeft + new Vector3(totalWorldWidth, offset, 0f)
                );
            }
        }

        //draw the cached [0, 1] classification overlay from the last sample
        if (showSampledDataOverlay && _lastSampledGrid != null && _lastSampledN > 0)
        {
            float sampledWorldWidth = _lastSampledN * pixelWorldSize;
            Vector3 sampledBottomLeft = new Vector3(
                _lastSampledCenter.x - sampledWorldWidth * 0.5f,
                _lastSampledCenter.y - sampledWorldWidth * 0.5f,
                0f
            );

            //inset the cube so each cell is visually distinct
            Vector3 cellCubeSize = new Vector3(pixelWorldSize * 0.9f, pixelWorldSize * 0.9f, 0.01f);

            for (int y = 0; y < _lastSampledN; y++)
            {
                for (int x = 0; x < _lastSampledN; x++)
                {
                    bool isLand = _lastSampledGrid[x, y] == 1;

                    // land is green, color is blue
                    Gizmos.color = isLand
                        ? new Color(0.1f, 0.9f, 0.2f, overlayOpacity)
                        : new Color(0.0f, 0.5f, 1.0f, overlayOpacity);

                    // center of pixel (x, y) is (x + 0.5, y + 0.5) * pixelWorldSize from bottom-left
                    Vector3 cellCenter = sampledBottomLeft + new Vector3(
                        (x + 0.5f) * pixelWorldSize,
                        (y + 0.5f) * pixelWorldSize,
                        0f
                    );

                    Gizmos.DrawCube(cellCenter, cellCubeSize);
                }
            }
        }
    }


    [ContextMenu("Test Terrain Sampling")]
    private void TestTerrainSampleAtPosition()
    {
        int[,] results = SampleSquareToGrid(transform.position, testSampleSizeN);
    }

    /// <summary>
    /// Main driver function that takes a position, samples a square n pixels around it, and returns the water/land distribution around it. Incredibly complicated with a ton of camera setup boilerplate.
    /// </summary>
    /// <param name="worldCenter"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    public int[,] SampleSquareToGrid(Vector2 worldCenter, int n) //where n is the dimension of the array
    {
        int sampleLayer = LayerMask.NameToLayer(samplerLayerName);
        if (sampleLayer == -1)
        {
            Debug.LogError($"Layer {samplerLayerName} could not be found for terrain sampling");
            return new int[n, n];
        }

        EnsureSampleCamera(sampleLayer);

        //convert the sorting layers to a hashset for convenience. May remove during optimization if the small overhead isn't worth it
        HashSet<int> targetSortingLayerIDs = new HashSet<int>();
        foreach (string layerName in targetSortingLayers)
        {
            targetSortingLayerIDs.Add(SortingLayer.NameToID(layerName));
        }

        //remove all renderers on the target temporarily for clean sample
        Renderer[] allRenderers = FindObjectsByType<Renderer>();
        Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();

        foreach (Renderer rend in allRenderers)
        {
            if (rend.enabled && targetSortingLayerIDs.Contains(rend.sortingLayerID))
            {
                GameObject go = rend.gameObject;
                if (!originalLayers.ContainsKey(go))
                {
                    originalLayers[go] = go.layer;
                    go.layer = sampleLayer;
                }
            }
        }

        //now configure the camera's position
        _sampleCam.transform.position = new Vector3(worldCenter.x, worldCenter.y, -10f);
        _sampleCam.orthographicSize = (n / campaignPixelsPerUnit) * 0.5f;

        //now render a renderTex from that camera
        RenderTexture rt = RenderTexture.GetTemporary(n, n, 16, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Point; //with point mode for no bilinear filtering (if some designer has the wrong import setting)

        RenderTexture previousActive = RenderTexture.active;
        _sampleCam.targetTexture = rt;

        //aaaaand render!
        _sampleCam.Render();

        //now read the pixels
        RenderTexture.active = rt;
        Texture2D readTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        readTex.ReadPixels(new Rect(0, 0, n, n), 0, 0);
        readTex.Apply();

        //and clean up, restoring GO layers
        _sampleCam.targetTexture = null;
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(rt);

        foreach (var kvp in originalLayers) //clear the hashset
        {
            if (kvp.Key != null)
                kvp.Key.layer = kvp.Value;
        }

        //now solve each pixel's state and return
        Color32[] pixels = readTex.GetPixels32();
        DestroyImmediate(readTex); //prevent a mem leak if we do this a lot

        int[,] grid = new int[n, n];

        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                Color32 c = pixels[y * n + x];
                grid[x, y] = ClassifyPixel(c);
            }
        }

        return grid;
    }

    /// <summary>
    /// Sorts a pixel's value into either water or land. May be expanded to add mountain or road later. Note that use of a Color32, as this is a texture sampling operation and not a Unity colored one.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    private int ClassifyPixel(Color32 c)
    {
        //bear with me for the weird bool formatting, it indents nice and cleanly

        // transparent is water
        if (c.a <= colorTolerance)
            return 0;

        // perfect blue is also water (0,0,255)
        bool isBlue = c.r <= colorTolerance &&
                      c.g <= colorTolerance &&
                      c.b >= (255 - colorTolerance);
        if (!isBlue) isBlue = c.b - c.g > 10 && c.b - c.r > 10; //if the blue check fails, see if blue is 20 or greater than the other ones.
        if (isBlue)
            return 0;

        // white is land
        bool isWhite = c.r >= (255 - colorTolerance) &&
                       c.g >= (255 - colorTolerance) &&
                       c.b >= (255 - colorTolerance);
        if (isWhite)
            return 1;

        // black is also land
        bool isBlack = c.r <= colorTolerance &&
                       c.g <= colorTolerance &&
                       c.b <= colorTolerance;
        if (isBlack)
            return 1;

        // finally, default any unrecognized color to land
        return 1;
    }

    /// <summary>
    /// Creates a temporary, culled, square camera pointed at a set position for terrain sampling
    /// </summary>
    /// <param name="sampleLayer"></param>
    private void EnsureSampleCamera(int sampleLayer)
    {
        if (_sampleCam != null) return;

        GameObject camObj = new GameObject("HiddenMapSampleCamera");
        camObj.transform.SetParent(this.transform);

        _sampleCam = camObj.AddComponent<Camera>();
        _sampleCam.orthographic = true;
        _sampleCam.aspect = 1f; // Forces a 1:1 aspect ratio
        _sampleCam.nearClipPlane = 0.1f;
        _sampleCam.farClipPlane = 50f;

        // Make transparent so blank registers properly
        _sampleCam.clearFlags = CameraClearFlags.SolidColor;
        _sampleCam.backgroundColor = new Color(0f, 0f, 0f, 0f);

        _sampleCam.cullingMask = 1 << sampleLayer;

        _sampleCam.enabled = false;
    }
}