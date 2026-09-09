using UnityEngine;

/// <summary>
/// A (abstract) city building is stored inside of a city, and makes up the blocks of a city's infrastructure and recuitment capabilities.
/// </summary>
public abstract class CityBuilding : ScriptableObject
{
    /// <summary>
    /// The visual icon of the building in the city building view.
    /// </summary>
    [SerializeField] Sprite buildingIcon;

    /// <summary>
    /// Upfront cost to constructing the building
    /// </summary>
    [SerializeField] int constructionCost;

    /// <summary>
    /// How many turns construction takes
    /// </summary>
    [SerializeField] int constructionTime;
    
    /// <summary>
    /// Money cost per turn, deducted during the end turn balance changes
    /// </summary>
    [SerializeField] int buildingMaintenance;

    /// <summary>
    /// The StructureData of the building.
    /// </summary>
    [SerializeField] StructureData buildingData;

    /// <summary>
    /// Called at the end of a turn to see if it has some passive impact. Used for population growth, etc.
    /// </summary>
    public abstract void endTurnImpact();

    /// <summary>
    /// called on a building when checking what it unlocks.
    /// </summary>
    public abstract void checkBuildingUnlocks();

    public Sprite BuildingIcon {get {return buildingIcon;}}
    public int ConstructionCost {get {return constructionCost;}}
    public int ConstructionTime {get {return constructionTime;}}
    public int BuildingMaintenance {get {return buildingMaintenance;}}
    public StructureData BuildingData {get {return buildingData;}}
}