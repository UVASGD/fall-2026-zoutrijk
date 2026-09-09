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
    public void AddTraitPoints(TraitDefinition traitDef, int points)
    {
        //Check mutual exclusivity
        foreach (var exclusive in traitDef.mutallyExclusiveTraits)
        {
            if (activeTraits.ContainsKey(exclusive))
            {
                //Don't add the exclusive trait point if another trait overrides it
                return;
            }
        }

        if (!activeTraits.ContainsKey(traitDef.TraitID))
        {
            activeTraits[traitDef.TraitID] = new Trait(traitDef);
        }

        activeTraits[traitDef.TraitID].AddPoints(points);

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

    private void RecalculateStats()
    {

        OnTraitsChanged?.Invoke();
    }
}