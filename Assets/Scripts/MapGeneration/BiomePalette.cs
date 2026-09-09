using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "BiomePalette", menuName = "Map Generation/Biome Palette")]
public class BiomePalette : ScriptableObject
{
    [SerializeField] TileComposition groundComposition;
    
    [Header("Ground props")]
    [SerializeField] List<GameObject> rocks;
    [SerializeField] List<GameObject> trees;
    
    public TileComposition GroundComposition => groundComposition;
}

[Serializable]
public class TileComposition
{
    [Header("Tile Patterns: 4x4 structuredata that are randomly placed throughout the map")]
    public List<StructureData> tilePatterns;

    [Header("Composition percentages: Must add up to 100% and have same n as tiles")]
    public List<int> percentages;
}