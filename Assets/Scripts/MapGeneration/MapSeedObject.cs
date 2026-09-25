using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapSeed", menuName = "Map Generation/Map Seed")]
public class MapSeedObject : ScriptableObject
{
    [SerializeField] private BiomePalette biome;
    [SerializeField] private int sampleWidth;
    [SerializeField] private int sampleHeight;
    [SerializeField] private List<int> terrainValues = new List<int>();

    public void SetSeedData(BattleMapSeedData seedData)
    {
        biome = seedData != null ? seedData.biome : null;
        terrainValues.Clear();
        sampleWidth = 0;
        sampleHeight = 0;

        if (seedData == null || seedData.terrainSample == null) return;

        sampleWidth = seedData.terrainSample.GetLength(0);
        sampleHeight = seedData.terrainSample.GetLength(1);

        for (int x = 0; x < sampleWidth; x++)
        {
            for (int y = 0; y < sampleHeight; y++)
            {
                terrainValues.Add(seedData.terrainSample[x, y]);
            }
        }
    }

    public BattleMapSeedData CreateSeedData()
    {
        if (sampleWidth <= 0 || sampleHeight <= 0 || terrainValues == null ||
            terrainValues.Count != sampleWidth * sampleHeight)
        {
            return null;
        }

        int[,] terrainSample = new int[sampleWidth, sampleHeight];
        int valueIndex = 0;

        for (int x = 0; x < sampleWidth; x++)
        {
            for (int y = 0; y < sampleHeight; y++)
            {
                terrainSample[x, y] = terrainValues[valueIndex++];
            }
        }

        return new BattleMapSeedData(terrainSample, biome, null);
    }
}