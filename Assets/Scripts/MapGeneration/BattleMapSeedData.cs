using System;
/// <summary>
/// This class is what the campaign map passes to the battle scene in order to generate the map. This includes the MapCity, BiomePalette, and terrain SampleGrid.
/// </summary>
[Serializable]
public class BattleMapSeedData
{
    public int[,] terrainSample;
    public BiomePalette biome;
    private MapCity localCity; //crucially, NEEDs to change if we ever have twin cities in the game (which would be insanely cool)
    public MapCity city => localCity; //we use a getter because accidentally editing a localCity would be really bad

    public BattleMapSeedData(int[,] terrainSample, BiomePalette biome, MapCity localCity)
    {
        this.terrainSample = terrainSample;
        this.biome = biome;
        this.localCity = localCity;
    }
}