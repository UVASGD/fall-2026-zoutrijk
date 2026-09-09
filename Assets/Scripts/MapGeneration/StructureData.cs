using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "StructureData/Create new StructureData")]
public class StructureData : ScriptableObject
{
    /// <summary>
    /// Tiles that are used in this building. Allows for palette swapping easily to make a building a different culture.
    /// </summary>
    [SerializeField] public List<Sprite> tilePalette;

    /// <summary>
    /// List of 2d arrays that represents the indices of the associated tiles on a layer. A development tool will likely be required to accomodate easier construction.
    /// </summary>
    [SerializeField] public List<LayerData> layers;
    
    /// <summary>
    /// Dimensions of the building, in tiles.
    /// </summary>
    [SerializeField] public Vector2 footprint;
}

//Some structs to get around Unity inspector quirks

[System.Serializable]
public struct ColumnData {
    public List<int> rows;
}

[System.Serializable]
public struct LayerData {
    public List<ColumnData> columns;
}