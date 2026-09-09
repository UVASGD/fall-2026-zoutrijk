using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))] //require a spriterenderer on every Region.
public class Region : MonoBehaviour
{
    [SerializeField] private string regionCode;
    [SerializeField] private string regionName;
    [SerializeField] List<Region> borderingRegions; //the list of regions that are bordering this one.
    public FieldArmy occupyingArmy { get; private set; }
    private SpriteRenderer regionRenderer;
    public string RegionCode => regionCode;
    public string RegionName => regionName;
    public MapFaction owner { get; private set; }
    [SerializeField] RegionalTerrain regionalTerrain;
    public RegionalTerrain RegionalTerrain => regionalTerrain;

    [SerializeField] int startingPopulation;
    [SerializeField] Sprite regionalBanner;
    public Sprite RegionalBanner => regionalBanner;
    public int currentPopulation;
    [SerializeField] Noble localNoble;
    public Noble LocalNoble => localNoble;

    /// <summary>
    /// sets up the region for use in the campaign map.
    /// </summary>
    public void InitializeRegion()
    {
        regionRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        InitializeRegion();
    }

    /// <summary>
    /// updates the color of the region's SpriteRenderer.
    /// </summary>
    /// <param name="color"></param>
    public void UpdateRegionColor(Color color)
    {
        regionRenderer.color = color;
    }

    /// <summary>
    /// setter for who owns this region.
    /// </summary>
    /// <param name="faction"></param>
    public void UpdateOwner(MapFaction faction)
    {
        if (owner != null)
            owner.LoseRegion(this);

        owner = faction;

        UpdateRegionColor(owner.fBase.MapColor);
    }

    /// <summary>
    /// recolor this region to match its realm's primary color.
    /// </summary>
    public void RecolorPolitical()
    {
        UpdateRegionColor(owner.fBase.MapColor);
    }

    /// <summary>
    /// recolor this region to match appropriate terrain graphics.
    /// </summary>
    public void RecolorTerrain()
    {
        UpdateRegionColor(regionalTerrain.BackgroundColor);

        //apply the material associated with the terrain
        regionRenderer.material = regionalTerrain.RegionalMaterial;
    }
}