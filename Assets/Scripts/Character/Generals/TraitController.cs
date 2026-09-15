using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TraitController
{
    public event Action OnTraitsChanged;
    [SerializeField] public Dictionary<string, Trait> activeTraits = new Dictionary<string, Trait>(); //key is the trait ID
    private Dictionary<StatType, int> baseStats = new Dictionary<StatType, int>();

    /// <summary>
    /// Adds points to a particular type of trait. If the general does not have this trait, it is added to the active traits dictionary.
    /// </summary>
    /// <param name="traitDef"></param>
    /// <param name="points"></param>
    public void AddTraitPoints(string traitID, int points)
    {
        //check if the traitID is already contained. If not, make a call to the TraitDB to get a copy of the traitDef.
        if (activeTraits.ContainsKey(traitID))
        {
            activeTraits[traitID].AddPoints(points);
        }
        else
        {
            //call the singleton TraitDB
            TraitDefinition newDef = TraitDB.i.GetTraitByID(traitID);
            activeTraits.Add(traitID, new Trait(newDef, points)); //add to dictionary and apply the required points.
        }

        RecalculateStats();
    }

    /// <summary>
    /// Gets the general's total stat bonus for a particular stat. Written modularly to allow for multiple stat types.
    /// </summary>
    /// <param name="stat"></param>
    public int GetTotalStatVal(StatType stat)
    {
        int total = baseStats.ContainsKey(stat) ? baseStats[stat] : 0;

        foreach (var trait in activeTraits.Values)
        {
            if (trait.CurrentLevel != null)
            {
                foreach (var mod in trait.CurrentLevel.Modifiers)
                {
                    if (mod.Stat == stat)
                    {
                        total += mod.Value;
                        Debug.Log($"{stat} modified by {mod.Value} due to trait {trait.CurrentLevel.levelName}");
                    }
                }
            }
        }

        return total;
    }
    
    /// <summary>
    /// Recalculates the stats for a general, often after changing traits.
    /// </summary>
    private void RecalculateStats()
    {

        OnTraitsChanged?.Invoke();
    }
}

/// <summary>
/// Can be used to refer to a trait without any overhead. Used in the SpeechSnippet logic
/// </summary>
[Serializable]
public struct TraitBuilder
{
    public string traitName; //the string associated with the TraitDef
    public int traitPoints; //the number of points on this trait
}