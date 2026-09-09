using System.Collections.Generic;
using UnityEngine;

public class MapFaction
{
    public MapFactionBase fBase {get; private set;}
    public string DisplayName => fBase.name;
    public List<Region> ownedRegions {get; private set;}
    public List<FieldArmy> fieldArmies {get; private set;}
    /// <summary>
    /// Initializes a MapFaction using its base.
    /// </summary>
    /// <param name="fBase"></param>
    public MapFaction(MapFactionBase fBase)
    {
        this.fBase = fBase;
        ownedRegions = new List<Region>();
        fieldArmies = new List<FieldArmy>();
    }

    public void OccupyRegion(Region region)
    {
        ownedRegions.Add(region); //add to the list of owned regions
        region.UpdateOwner(this); //update the owner on the region's end
    }
    
    public void LoseRegion(Region region)
    {
        if (ownedRegions.Contains(region))
        {
            ownedRegions.Remove(region);
        }
        else
        {
            Debug.LogError("Tried to remove a region that does not belong to this faction");
        }
    }
}