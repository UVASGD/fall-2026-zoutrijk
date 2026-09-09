using System.Collections.Generic;
using UnityEngine;

public class MapmodeManager : MonoBehaviour
{
    public static MapmodeManager i;
    void Awake()
    {
        if (i == null) i = this;
    }
    public Mapmode mapmode { get; private set; }
    public List<Region> gameRegions => CampaignMapManager.i.regionList;

    /// <summary>
    /// branching if-statement that handles a change in mapmode.
    /// </summary>
    /// <param name="mode"></param>
    public void UpdateMapmode(Mapmode mode)
    {
        this.mapmode = mode;

        if (this.mapmode == Mapmode.political)
        {
            foreach (var region in gameRegions)
            {
                region.RecolorPolitical(); //recolor each region by owner.
            }
        }
        else if (mapmode == Mapmode.terrain)
        {
            foreach (var region in gameRegions)
            {
                region.RecolorTerrain();
            }
        }
    }
}

public enum Mapmode
{
    political,
    terrain
}