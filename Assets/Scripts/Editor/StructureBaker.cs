using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// An editor evelopment tool used to bake a prefab building into a useable format for procedural buildings.
/// </summary>
public class StructureBaker
{
    /// <summary>
    /// Bakes the highlighted tilemap prefab into a palette and 3d integer array for easier use in procedural battlemap generation.
    /// </summary>
    [MenuItem("Assets/Bake StructureData")]
    public static void BakeSelectedPrefab()
    {
        GameObject selectedPrefab = Selection.activeGameObject;

        if (selectedPrefab == null)
        {
            Debug.LogWarning("No Prefab Selected");
            return;
        }

        // Create a temporary, hidden instance of the prefab in the scene.
        // This forces Unity to initialize the Tilemap bounds properly.
        GameObject tempInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
        if (tempInstance == null)
        {
            tempInstance = Object.Instantiate(selectedPrefab); // Fallback
        }
        tempInstance.hideFlags = HideFlags.HideAndDontSave;

        //run inside of a try block
        try
        {
            Tilemap[] tilemaps = tempInstance.GetComponentsInChildren<Tilemap>();

            if (tilemaps.Length == 0)
            {
                Debug.LogWarning("No tilemap found in prefab children");
                return;
            }

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (Tilemap tilemap in tilemaps)
            {
                tilemap.CompressBounds();
                BoundsInt bounds = tilemap.cellBounds;

                if (bounds.xMin < minX) minX = bounds.xMin;
                if (bounds.yMin < minY) minY = bounds.yMin;
                if (bounds.xMax > maxX) maxX = bounds.xMax;
                if (bounds.yMax > maxY) maxY = bounds.yMax;
            }

            int width = maxX - minX;
            int length = maxY - minY;

            if (width <= 0 || length <= 0)
            {
                Debug.LogWarning("Prefab tilemaps are empty");
                return; //terminate flow early
            }

            //Create the structureData
            StructureData structureData = ScriptableObject.CreateInstance<StructureData>();
            structureData.tilePalette = new List<Sprite>();
            structureData.layers = new List<LayerData>();
            structureData.footprint = new Vector2(width, length);

            //now parse each tilemap into layers
            foreach (Tilemap tilemap in tilemaps)
            {
                //create the layerdata struct
                LayerData layerData = new LayerData { columns = new List<ColumnData>() };

                bool isLayerEmpty = (tilemap.cellBounds.size.x == 0 || tilemap.cellBounds.size.y == 0);

                for (int x = 0; x < width; x++)
                {
                    ColumnData columnData = new ColumnData { rows = new List<int>() };

                    for (int y = 0; y < length; y++)
                    {
                        if (isLayerEmpty)
                        {
                            columnData.rows.Add(-1);
                            continue; 
                        }

                        //find the tilemap position on the grid
                        Vector3Int gridPos = new Vector3Int(minX + x, minY + y, 0);

                        //get the sprite at that position
                        Sprite sprite = tilemap.GetSprite(gridPos);

                        if (sprite != null)
                        {
                            //check if the sprite is already in the palette. If not, add it
                            int paletteIndex = structureData.tilePalette.IndexOf(sprite);

                            if (paletteIndex == -1)
                            {
                                //add sprite to palette
                                structureData.tilePalette.Add(sprite);
                                paletteIndex = structureData.tilePalette.Count - 1;
                            }

                            columnData.rows.Add(paletteIndex);
                        }
                        else
                        {
                            //-1 represents an empty tile, as it is not on the palette array
                            columnData.rows.Add(-1);
                        }
                    }
                    //add the completed column to the layerdata
                    layerData.columns.Add(columnData);
                }
                //add the completed layer to the structuredata
                structureData.layers.Add(layerData);
            }

            //finally, save the asset in the game files.
            string defaultPath = "Assets/Resources/StructureData/" + selectedPrefab.name + "_Data.asset";
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(defaultPath); //create a unique path for the asset

            AssetDatabase.CreateAsset(structureData, uniquePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Successfully baked {width}x{length} building with {structureData.layers.Count} layers and {structureData.tilePalette.Count} unique sprites");
        }
        finally
        {
            //destroy the temporary instance, especially if an error happens.
            Object.DestroyImmediate(tempInstance);
        }
    }

    [MenuItem("Assets/Bake Prefab to Structure Data", true)]
    public static bool ValidateBakeSelectedPrefab()
    {
        return Selection.activeGameObject != null;
    }
}