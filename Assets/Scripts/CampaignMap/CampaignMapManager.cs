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
    public MapFaction PlayerFaction => playerFaction;
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

        ResolveHoveredObject(objectCollider, out FieldArmy hoveredArmy, out MapCity hoveredCity, out Region hoveredRegion);
        UpdateHoveredObject(hoveredArmy, hoveredCity, hoveredRegion);

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (selectedArmy != null && hoveredRegion != null)
            {
                CloseContextMenus();
                selectedArmy.MoveArmy(hoveredRegion, worldPosition);
            }
            else
            {
                if (hoveredArmy == null && hoveredCity == null)
                {
                    ClearSelectedObject();
                }
                OpenContextMenu(hoveredArmy, hoveredCity, hoveredRegion);
            }
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            CloseContextMenus();
            SelectHoveredObject();
        }
    }

    /// <summary>
    /// Resolves the object under the cursor using army, city, then region priority.
    /// </summary>
    /// <param name="objectCollider">The collider currently under the cursor.</param>
    /// <param name="army">The hovered army, if one was found.</param>
    /// <param name="city">The hovered city, if no army was found.</param>
    /// <param name="region">The hovered region, if no army or city was found.</param>
    private void ResolveHoveredObject(Collider2D objectCollider, out FieldArmy army, out MapCity city, out Region region)
    {
        army = objectCollider != null ? objectCollider.GetComponent<FieldArmy>() : null;
        city = army == null && objectCollider != null ? objectCollider.GetComponent<MapCity>() : null;
        region = city == null && objectCollider != null ? objectCollider.GetComponent<Region>() : null;
    }

    /// <summary>
    /// Updates the cached object under the cursor and clears stale hover state.
    /// </summary>
    /// <param name="army">The hovered army, if one was found.</param>
    /// <param name="city">The hovered city, if one was found.</param>
    /// <param name="region">The hovered region, if one was found.</param>
    private void UpdateHoveredObject(FieldArmy army, MapCity city, Region region)
    {
        if (army != null && army != highlightedArmy)
        {
            UpdateArmy(army);
        }
        else if (city != null && city != highlightedCity)
        {
            UpdateCity(city);
        }
        else if (region != null && region != highlightedRegion)
        {
            UpdateRegion(region);
        }
        else if (army == null && city == null && region == null &&
                 (highlightedArmy != null || highlightedCity != null || highlightedRegion != null))
        {
            ClearHoveredObject();
        }
    }

    /// <summary>
    /// Closes existing context menus and opens the highest-priority menu for the hovered object.
    /// </summary>
    /// <param name="army">The hovered army, if one was found.</param>
    /// <param name="city">The hovered city, if one was found.</param>
    /// <param name="region">The hovered region, if one was found.</param>
    private void OpenContextMenu(FieldArmy army, MapCity city, Region region)
    {
        CloseContextMenus();

        if (army != null)
        {
            campaignUI.ShowGeneralDetails(army);
        }
        else if (city != null)
        {
            campaignUI.ShowCityPanel(true, city);
        }
        else if (region != null)
        {
            campaignUI.ShowRegionDetails(region);
        }
    }

    /// <summary>
    /// Closes the city, region, and army context menus.
    /// </summary>
    private void CloseContextMenus()
    {
        campaignUI.ShowCityPanel(false);
        campaignUI.HideRegionDetails();
        campaignUI.HideGeneralDetails();
    }

    /// <summary>
    /// Selects the cached army or city, or clears the current selection on empty space or a region.
    /// </summary>
    private void SelectHoveredObject()
    {
        if (highlightedArmy != null)
        {
            selectedArmy = highlightedArmy;
            selectedCity = null;
            campaignUI.PlaceHighlightCursor(selectedArmy.transform);
        }
        else if (highlightedCity != null)
        {
            selectedArmy?.OnUnitSelected(false);
            selectedCity = highlightedCity;
            selectedArmy = null;
            campaignUI.PlaceHighlightCursor(selectedCity.transform);
        }
        else
        {
            ClearSelectedObject();
        }
    }

    /// <summary>
    /// Clears the selected army and city and disables the selection cursor.
    /// </summary>
    private void ClearSelectedObject()
    {
        selectedArmy?.OnUnitSelected(false);
        selectedArmy = null;
        selectedCity = null;
        campaignUI.DisableHighlightCursor();
    }

    /// <summary>
    /// Clears all cached hover targets and disables the army hover indicator.
    /// </summary>
    private void ClearHoveredObject()
    {
        highlightedArmy?.OnUnitSelected(false);
        highlightedArmy = null;
        highlightedCity = null;
        highlightedRegion = null;
    }

    /// <summary>
    /// updates the currently cached "highlighted region" inside of the CampaignMapManager
    /// </summary>
    /// <param name="region"></param>
    public void UpdateRegion(Region region)
    {
        highlightedArmy?.OnUnitSelected(false);
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
        army.OnUnitSelected(true);
    }

    /// <summary>
    /// caches the hovered upon city
    /// </summary>
    /// <param name="city"></param>
    public void UpdateCity(MapCity city)
    {
        highlightedCity = city;
        highlightedRegion = null;
        highlightedArmy?.OnUnitSelected(false);
        highlightedArmy = null;
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

    public void OnEndTurn()
    {
        foreach(var army in fieldArmies)
        {
            army.OnTurnStart();
        }
        
        foreach(var city in cities)
        {
            foreach(var building in city.buildings)
            {
                building.EndTurnImpact(city);
            }
        }
    }

    public int IncrementTurnCounter()
    {
        turnCount++;
        return turnCount;
    }
}