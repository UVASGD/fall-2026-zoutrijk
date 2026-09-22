using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A city building is stored inside of a city, and makes up the blocks of a city's infrastructure and recuitment capabilities.
/// </summary>
[CreateAssetMenu(menuName = "CityBuilding/Create New CityBuilding")]
public class CityBuilding : ScriptableObject
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
    /// The StructureData of the CityBuilding.
    /// </summary>
    [SerializeField] StructureData buildingData;

    [Header("List of effects that happen after a turn is ended")]
    [SerializeReference] List<GenericTurnEndEffect> TurnEndEffects = new List<GenericTurnEndEffect>(); //the list of effects cycled through when a turn is ended.

    /// <summary>
    /// Called at the end of a turn to see if it has some passive impact. Used for population growth, etc.
    /// </summary>
    public void EndTurnImpact(MapCity parentCity)
    {
        foreach(var effect in TurnEndEffects)
        {
            effect.OnEndTurnEffect(parentCity);
        }
    }

    public Sprite BuildingIcon {get {return buildingIcon;}}
    public int ConstructionCost {get {return constructionCost;}}
    public int ConstructionTime {get {return constructionTime;}}
    public StructureData BuildingData {get {return buildingData;}}

    public void AddOnTurnEndCause(GenericTurnEndEffect effect)
    {
        TurnEndEffects.Add(effect);
    }

    public string GetPanelDisplayText()
    {
        string output = $"{name}:";
        foreach(var effect in TurnEndEffects)
        {
            output += effect.EffectName + "\n";
        }

        return output;
    }
}