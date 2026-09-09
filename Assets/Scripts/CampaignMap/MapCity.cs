using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MapCity is the map depiction of each major settlement. All cities are procedurally generated on the battlefield. MapCity stores population info, buildings, etc. This is a vital class to the campaign architecture.
/// </summary>
public class MapCity : MonoBehaviour
{
    public int recruitablePopulation {get; private set;}
    public string cityName;
    [SerializeField] string homeRegionCode; //the region the settlement is in
    
    [Tooltip("Level of development. 0 = hamlet, 1 = town, 2 = manor, 3 = castle, 4 = fortress, 5 = city")]
    [SerializeField] int developmentLevel;

    [Tooltip("If occupation of this settlement leads to a flip in the political ownership of the entire region. Conquering a capital also leads to a progressive occupation of bordering towns.")]
    [SerializeField] bool regionalCapital;
    public Region HomeRegion {get; private set;}
    public MapFaction owner {get; private set;}
    [SerializeField] public List<CityBuilding> buildings;
    private List<Unit> garrison; //town watch that defends the city under siege.
    public List<Unit> Garrison => garrison;

    /// <summary>
    /// sets up a city for use on the campaign map
    /// </summary>
    public void InitCity()
    {
        buildings = new List<CityBuilding>(); //initialize the list of buildings
        HomeRegion = CampaignMapManager.i.getRegionByCode(homeRegionCode); //assign its home region
        owner = HomeRegion.owner;
    }

    /// <summary>
    /// Logic that fires if this settlement is conquered.
    /// </summary>
    public void OnCityOccupation(MapFaction occupier)
    {
        if(developmentLevel > 5 || developmentLevel < 0)
        {
            Debug.LogError("Development Level for this city is out of range 0-5.");
            return;
        }
        if (regionalCapital)
        {
            //flip regional ownership to the new overlord
            HomeRegion.UpdateOwner(occupier);
        }
    }
}