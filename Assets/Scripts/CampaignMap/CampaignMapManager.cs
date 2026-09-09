using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CampaignMapManager : MonoBehaviour
{
    #region fields
    public static CampaignMapManager i;
    [SerializeField] List<Region> regions;
    [SerializeField] List<MapFactionBase> factionBases;
    [SerializeField] List<FieldArmy> startingArmies;
    [SerializeField] List<MapCity> startingCities;
    private List<MapFaction> factions;
    private MapFaction playerFaction;
    private int turnCount = 0; //starts at zero
    private Region highlightedRegion;
    private FieldArmy highlightedArmy;
    private MapCity highlightedCity;

    [SerializeField] Transform unitParent;
    [SerializeField] Transform cityParent;

    [Header("Globally Accessible Fields")]
    public List<Region> regionList => regions;
    [SerializeField] Material defaultRegionMaterial; //should just be sprite unlit default
    public Material DefaultRegionMaterial => defaultRegionMaterial;
    public FieldArmy selectedArmy { get; private set; } //an army that is currently selected
    public MapCity selectedCity { get; private set; } //a settlement that is currently selected
    public List<FieldArmy> fieldArmies { get; private set; }
    public List<MapCity> cities { get; private set; }
    #endregion

    #region Helper Scripts
    [SerializeField] CampaignUI campaignUI;
    #endregion

    void Awake()
    {
        if (i == null)
            i = this;

        SetupCampaign("BRE");

        campaignUI.UpdateYearText(turnCount);
    }

    void Update()
    {
        if (Mouse.current == null || Camera.main == null)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        Collider2D objectCollider = Physics2D.OverlapPoint(worldPosition);

        //check for: army, then city, then region in that order.
        FieldArmy hoveredArmy = objectCollider != null ? objectCollider.GetComponent<FieldArmy>() : null;

        MapCity hoveredCity = hoveredArmy == null && (objectCollider != null) ? objectCollider.GetComponent<MapCity>() : null;

        Region hoveredRegion = hoveredCity == null && (objectCollider != null) ? objectCollider.GetComponent<Region>() : null;

        if (hoveredArmy != null && hoveredArmy != highlightedArmy)
        {
            UpdateArmy(hoveredArmy);
        }
        else if (hoveredCity != null && hoveredCity != highlightedCity)
        {
            UpdateCity(hoveredCity);
        }
        else if (hoveredRegion != null && hoveredRegion != highlightedRegion)
        {
            UpdateRegion(hoveredRegion);
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (hoveredRegion != null && selectedArmy == null && selectedCity == null)
            {
                campaignUI.ShowRegionDetails(hoveredRegion);
            }
            else if (hoveredArmy != null)
            {
                campaignUI.ShowGeneralDetails(hoveredArmy);
            }
            else
            {
                campaignUI.HideRegionDetails();
                campaignUI.HideGeneralDetails();
            }
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            campaignUI.HideRegionDetails(); //should never show details after a left click

            //check if either an army or city are selected. If so, select the army.
            if (highlightedArmy != null)
            {
                selectedArmy = highlightedArmy;
                //null out the selected city in this scenario
                selectedCity = null;
                campaignUI.PlaceHighlightCursor(selectedArmy.transform);
            }
            else if (highlightedCity != null)
            {
                selectedCity = highlightedCity;
                campaignUI.PlaceHighlightCursor(selectedCity.transform);
            }
            else
            {
                campaignUI.DisableHighlightCursor();
            }
        }
    }

    /// <summary>
    /// updates the currently cached "highlighted region" inside of the CampaignMapManager
    /// </summary>
    /// <param name="region"></param>
    public void UpdateRegion(Region region)
    {
        highlightedRegion = region;
        highlightedArmy = null;
        highlightedCity = null;
        campaignUI.UpdateHighlightedRegionUI(region, MapmodeManager.i.mapmode);
    }

    /// <summary>
    /// updates the cached hovered army, in similar function to UpdateRegion.
    /// </summary>
    /// <param name="army"></param>
    public void UpdateArmy(FieldArmy army)
    {
        highlightedArmy = army;
        highlightedRegion = null;
        highlightedCity = null;
        campaignUI.UpdateHighlightedArmyUI(army);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="city"></param>
    public void UpdateCity(MapCity city)
    {
        highlightedCity = city;
        highlightedRegion = null;
        highlightedRegion = null;
        campaignUI.UpdateHighlightedCityUI(city);
    }

    /// <summary>
    /// sets up a campaign, passing the factionCode of the player faction.
    /// </summary>
    /// <param name="playerFaction"></param>
    public void SetupCampaign(string playerFaction)
    {
        i = this;

        //initializing various lists
        factions = new List<MapFaction>();
        mapRegions = new Dictionary<string, Region>();
        factionDictionary = new Dictionary<string, MapFaction>();

        fieldArmies = new List<FieldArmy>();
        fieldArmies.AddRange(startingArmies); //add all starting armies to the field armies list.
        cities = new List<MapCity>();

        foreach (var region in regions)
        {
            region.InitializeRegion();
            mapRegions.Add(region.RegionCode, region);
        }

        foreach (var faction in factionBases)
        {
            MapFaction mFaction = new MapFaction(faction); //construct a new faction from its base

            //check if this faction is the player faction
            if (playerFaction.Equals(faction.FactionTag))
            {
                this.playerFaction = mFaction; //store it as the playerFaction in memory
            }

            factions.Add(mFaction); //add it to the factions list
            factionDictionary.Add(mFaction.fBase.FactionTag, mFaction);//also add it to the faction dictionary

            //now assign owned regions to the faction
            foreach (string region in mFaction.fBase.StartingRegions)
            {
                mFaction.OccupyRegion(mapRegions[region]); //update using the dictionary
            }
        }

        foreach (var city in startingCities)
        {
            city.InitCity();
            cities.Add(city);
        }

        //call init on all FieldArmies on the map.
        foreach (var army in fieldArmies)
        {
            army.InitializeArmy();
        }
    }

    /// <summary>
    /// Dictionary that returns a Region from its regionCode.
    /// </summary>
    private Dictionary<string, Region> mapRegions;

    /// <summary>
    /// Dictionary that returns a MapFaction from its factionCode. 
    /// </summary>
    private Dictionary<string, MapFaction> factionDictionary;

    //null-checked getters for the dictionaries:
    public MapFaction getFactionByCode(string code)
    {
        if (factionDictionary.ContainsKey(code))
            return factionDictionary[code];
        else return null;
    }

    public Region getRegionByCode(string code)
    {
        if (mapRegions.ContainsKey(code))
            return mapRegions[code];
        else return null;
    }
}