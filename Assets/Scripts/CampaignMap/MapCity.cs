using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MapCity is the map depiction of each major settlement. All cities are procedurally generated on the battlefield. MapCity stores population info, buildings, etc. This is a vital class to the campaign architecture.
/// </summary>
public class MapCity : MonoBehaviour
{
    public int recruitablePopulation { get; private set; }
    public string cityName;
    [SerializeField] string homeRegionCode; //the region the settlement is in

    [Tooltip("Level of development. 0 = hamlet, 1 = town, 2 = manor, 3 = castle, 4 = fortress, 5 = city")]
    [SerializeField] int developmentLevel;

    [Tooltip("If occupation of this settlement leads to a flip in the political ownership of the entire region. Conquering a capital also leads to a progressive occupation of bordering towns.")]
    [SerializeField] bool regionalCapital;
    public Region HomeRegion { get; private set; }
    public MapFaction owner { get; private set; }
    [SerializeField] public List<CityBuilding> buildings;
    [SerializeField] public List<CityBuilding> availableToBuildBuildings; //determined by a turnEndEffect from the previous turn.
    [SerializeField] public List<UnitRecruitmentCapability> recruitableUnits; //determined by another turnEndEffect.
    private List<Unit> garrison; //town watch that defends the city under siege.
    public List<Unit> Garrison => garrison;
    private Noble governor;
    public Noble Governor => governor;
    private List<Noble> boardingNobles; //nobles currently staying in this city

    //queues for buildings and unit recruitment
    private List<BuildingConstruction> constructionQueue;//not done in a C# Queue due to serialization limitations
    private List<UnitRecruiment> recruitmentQueue; //as unit recruitment is done in batches, no queue needed

    /// <summary>
    /// Sets up a city for use on the campaign map
    /// </summary>
    public void InitCity()
    {
        HomeRegion = CampaignMapManager.i.getRegionByCode(homeRegionCode); //assign its home region
        owner = HomeRegion.owner;
    }

    public void OnEndTurn()
    {
        //progress the construction queue
        if (constructionQueue[0].CheckConstructionProgress())
        {
            buildings.Add(constructionQueue[0].buildingToConstruct); //add the building
            constructionQueue.Remove(constructionQueue[0]);//now remove it from the front of the list
        }

        //now do the recruitment queue
        if (recruitmentQueue[0].CheckRecruitmentProgress())
        {
            //same logic as buildings, just adding multiple copies of the unit we just added using a for-loop
            for (int i = 0; i < recruitmentQueue[0].batchSize; i++)
            {
                garrison.Add(recruitmentQueue[0].unitToRecruit);
            }
            recruitmentQueue.Remove(recruitmentQueue[0]);
        }

        foreach (var building in buildings)
        {
            building.EndTurnImpact(this);
        }

    }

    /// <summary>
    /// Logic that fires if this settlement is conquered.
    /// </summary>
    public void OnCityOccupation(MapFaction occupier)
    {
        if (developmentLevel > 5 || developmentLevel < 0)
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

    public void AddToGarrison(Unit newUnit)
    {
        garrison.Add(newUnit);
    }

    public void AddToBuildingQueue(CityBuilding building)
    {
        constructionQueue.Add(new BuildingConstruction(building));
    }

    public void AddToRecruitmentQueue(UnitBase unitBase, int quantity)
    {
        recruitmentQueue.Add(new UnitRecruiment(new Unit(unitBase), quantity));
    }

    /// <summary>
    /// checks if conditions are met for adding a recruitment batch to the list. May be expanded to become more complex (citizen qualifications, equipment, etc.) later
    /// </summary>
    /// <param name="unitBase"></param>
    /// <param name="quantity"></param>
    public bool RequestAdditionToRecruitmentQueue(UnitBase unitBase, int quantity)
    {
        //check if the player has enough money, and recruitable population.
        if (CampaignMapManager.i.PlayerFaction.CurrentCurrency > unitBase.RecruitmentCost * quantity && recruitablePopulation > quantity)
        {
            AddToRecruitmentQueue(unitBase, quantity);
            return true;
        }
        else return false; //unit recruitment could not be completed
    }

    public bool RequestAdditionToBuildingQueue(CityBuilding building)
    {
        if(CampaignMapManager.i.PlayerFaction.CurrentCurrency > building.ConstructionCost)
        {
            AddToBuildingQueue(building);
            return false;
        }
        else return false;
    }
}