using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;

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

            structureData.icon = CreateStructureIcon(tempInstance, uniquePath);

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

    private static Sprite CreateStructureIcon(GameObject structureInstance, string structureDataPath)
    {
        Renderer[] renderers = structureInstance.GetComponentsInChildren<Renderer>();
        Bounds structureBounds = new Bounds();
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer is TilemapRenderer && renderer.enabled)
            {
                if (!hasBounds)
                {
                    structureBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    structureBounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (!hasBounds)
        {
            Debug.LogWarning("Could not create structure icon because the prefab has no visible tilemap bounds.");
            return null;
        }

        const int maximumTextureSize = 256;
        const float boundsPadding = 1.1f;
        float width = Mathf.Max(structureBounds.size.x, 0.01f);
        float height = Mathf.Max(structureBounds.size.y, 0.01f);
        float aspect = width / height;
        int textureWidth = aspect >= 1f ? maximumTextureSize : Mathf.Max(1, Mathf.RoundToInt(maximumTextureSize * aspect));
        int textureHeight = aspect >= 1f ? Mathf.Max(1, Mathf.RoundToInt(maximumTextureSize / aspect)) : maximumTextureSize;

        GameObject cameraObject = new GameObject("Structure Icon Camera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        Camera iconCamera = cameraObject.AddComponent<Camera>();
        iconCamera.clearFlags = CameraClearFlags.SolidColor;
        iconCamera.backgroundColor = Color.clear;
        iconCamera.orthographic = true;
        iconCamera.aspect = aspect;
        iconCamera.orthographicSize = Mathf.Max(height, width / aspect) * boundsPadding / 2f;
        iconCamera.transform.position = new Vector3(structureBounds.center.x, structureBounds.center.y, structureBounds.center.z - 10f);
        iconCamera.cullingMask = ~0;

        RenderTexture renderTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        iconCamera.targetTexture = renderTexture;
        iconCamera.Render();

        RenderTexture previousActiveTexture = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D iconTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        iconTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        iconTexture.Apply();
        RenderTexture.active = previousActiveTexture;

        const string iconFolder = "Assets/Resources/StructureIcons";
        if (!AssetDatabase.IsValidFolder(iconFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "StructureIcons");
        }

        string iconFileName = Path.GetFileNameWithoutExtension(structureDataPath) + ".png";
        string iconPath = AssetDatabase.GenerateUniqueAssetPath(iconFolder + "/" + iconFileName);
        File.WriteAllBytes(iconPath, iconTexture.EncodeToPNG());

        Object.DestroyImmediate(iconTexture);
        iconCamera.targetTexture = null;
        renderTexture.Release();
        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(cameraObject);

        AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
    }

    [MenuItem("Assets/Bake Prefab to Structure Data", true)]
    public static bool ValidateBakeSelectedPrefab()
    {
        return Selection.activeGameObject != null;
    }
}