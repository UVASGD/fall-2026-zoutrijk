using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// The manager responsible for placing structures and painting them onto a tilemap, the heaviest part of world generation.
/// </summary>
public class StructurePlacer : MonoBehaviour
{
    public static StructurePlacer i;

    [Header("Tilemaps")]
    [Tooltip("Place the scene tilemaps here, where index 0 is structure layer 0, etc.")]
    [SerializeField] public List<Tilemap> targetTilemaps;

    //A dictionary for mapping sprites to Unity tiles quickly.
    private Dictionary<Sprite, Tile> tileCache = new Dictionary<Sprite, Tile>();

    /// <summary>
    /// Places a baked structure onto a scene's tilemaps.
    /// </summary>
    /// <param name="structure">The StructureData that is to be converted onto the tilemap</param>
    /// <param name="origin">The point that is assigned to the StructureData's (0,0) position.</param>
    public void PlaceStructure(StructureData structure, Vector3Int origin)
    {
        //quick null check before things get messy
        if (structure == null)
        {
            Debug.LogWarning("Structure data is null!");
            return;
        }

        //warning if the data has more layers than the scene
        if (targetTilemaps.Count < structure.layers.Count)
        {
            Debug.LogWarning("Top layers of structure are cut off due to scene not containing as many");
        }

        int layerCount = Mathf.Min(structure.layers.Count, targetTilemaps.Count);

        for (int i = 0; i < layerCount; i++)
        {
            Tilemap currentMap = targetTilemaps[i];

            LayerData layerData = structure.layers[i];

            // Iterate over the X axis (Columns)
            for (int x = 0; x < layerData.columns.Count; x++)
            {
                ColumnData columnData = layerData.columns[x];

                // Iterate over the Y axis (Rows)
                for (int y = 0; y < columnData.rows.Count; y++)
                {
                    int paletteIndex = columnData.rows[y];

                    // -1 means there is no tile painted at this local coordinate
                    if (paletteIndex != -1)
                    {
                        Sprite sprite = structure.tilePalette[paletteIndex];
                        Tile tileToPlace = GetOrCreateTile(sprite);

                        // Calculate absolute world grid position
                        Vector3Int targetPos = new Vector3Int(origin.x + x, origin.y + y, 0);

                        currentMap.SetTile(targetPos, tileToPlace);
                    }
                }
            }

#if UNITY_EDITOR
            //marking dirty so ctrl+z can be used to undo progress
            UnityEditor.EditorUtility.SetDirty(currentMap);
#endif
        }
    }

    /// <summary>
    /// retrieves a cached tile based on a sprite, or creates one if it doesn't exist yet.
    /// </summary>
    private Tile GetOrCreateTile(Sprite sprite)
    {
        if (sprite == null) return null;

        // If we've already generated a tile for this sprite, return it instantly
        if (tileCache.TryGetValue(sprite, out Tile existingTile))
        {
            return existingTile;
        }

        // Or create a new lightweight tile in memory
        Tile newTile = ScriptableObject.CreateInstance<Tile>();
        newTile.sprite = sprite;

        tileCache.Add(sprite, newTile);

        return newTile;
    }

    [Header("Testing")]
    [SerializeField] StructureData testStructure;
    [SerializeField] Vector3Int testOrigin;

    /// <summary>
    /// A test function in the inspector to place a structure
    /// </summary>
    [ContextMenu("Test Place Structure")]
    private void TestPlacement()
    {
        PlaceStructure(testStructure, testOrigin);
    }
    
    [ContextMenu("Clear All Layers")]
    public void ClearTestTilemaps()
    {
        if (targetTilemaps == null || targetTilemaps.Count == 0)
        {
            Debug.LogWarning("No target tilemaps assigned to clear.");
            return;
        }

        foreach (Tilemap tilemap in targetTilemaps)
        {
            if (tilemap == null)
            {
                continue;
            }

            tilemap.ClearAllTiles();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(tilemap);
#endif
        }
    }
    void Awake()
    {
        if (i == null) i = this;
        else
        {
            this.enabled = false; //disable this component if another one already exists.
            Debug.LogWarning("Duplicate StructurePlacer instance found. Please delete the second StructurePlacer, developer!");
        }
    }
}