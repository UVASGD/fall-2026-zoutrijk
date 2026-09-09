using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class governing general stats and names, associated with a local noble. Gameplay-side of characters.
/// </summary>
[CreateAssetMenu(menuName = "General/Create New General")]
public class General : ScriptableObject
{
    [SerializeField] string givenName;
    [ReadOnly] [SerializeField] string epithet; //accounts for epithets if we add them ("The Great", "The Cowardly" etc.)
    public string GivenName => givenName;
    public string FullName => givenName + " " + epithet;
    [ReadOnly][SerializeField] Dictionary<StatType, int> stats; //stat ints mapped to each type of stat.
    [SerializeField] Noble associatedNoble; //the map noble this general is associated with
    [SerializeField] TraitController traitController; //the general's traits and their effects on stats
    public Noble noble => associatedNoble; //public getter
    void Awake() //for SOs this is called on instantiation
    {
        stats = new Dictionary<StatType, int>();
        int[] statTypes = (int[])Enum.GetValues(typeof(StatType)); //some of the worst code I've ever written
        for (int i = 0; i < statTypes.Length; i++)
        {
            stats.Add((StatType)statTypes[i], 0); //insert a blank zero whenever a character is created for the first time.
        }
    }

    /// <summary>
    /// Checks which epithet is the most fitting for the general based on their traits
    /// </summary>
    public void UpdateEpithet()
    {
        epithet = string.Empty;
        int lowestPriority = int.MaxValue;
        foreach (var trait in traitController.activeTraits.Values)
        {
            if (trait.CurrentLevel != null && trait.CurrentLevel.epithet != null && !string.IsNullOrEmpty(trait.CurrentLevel.epithet.title) && trait.CurrentLevel.epithet.priority < lowestPriority)
            {
                epithet = trait.CurrentLevel.epithet.title;
                lowestPriority = trait.CurrentLevel.epithet.priority;
            }
        }
    }

    /// <summary>
    /// Just an accessor for the general stat values from its trait controller.
    /// </summary>
    /// <param name="stat"></param>
    /// <returns></returns>
    public int GetStatType(StatType stat)
    {
        return traitController.GetTotalStatVal(stat);
    }

    [ContextMenu("Recalculate Stats")]
    private void RecalculateStats()
    {
        int[] statTypes = (int[])Enum.GetValues(typeof(StatType));
        for (int i = 0; i < statTypes.Length; i++)
        {
            stats[(StatType)statTypes[i]] = traitController.GetTotalStatVal((StatType)statTypes[i]);
        }

        UpdateEpithet();
    }

    /// <summary>
    /// If this general has a trait at a specified level or higher. Used for the speech generator.
    /// </summary>
    /// <param name="traitBuilder"></param>
    /// <returns></returns>
    public bool hasTraitLevel(TraitBuilder traitBuilder)
    {
        if(!traitController.activeTraits.ContainsKey(traitBuilder.traitName)) return false;
        if(Math.Abs(traitController.activeTraits[traitBuilder.traitName].CurrentLevel.requiredPoints) < Math.Abs(traitBuilder.traitPoints)) return false;
        else return true;
    }
}

/// <summary>
/// Types of stats that a general influences.
/// </summary>
public enum StatType
{
    Respect, //how much soldiers respect this general's orders (improves morale)
    Dread, //how feared this general's presence is on the battlefield (reduces enemy morale)
    Discipline, //how well trained troops are against their humane instincts (impacts morale, resistance to sacking, movement speed, etc.)
    Administration //this general's abilities as a governor

}

[System.Serializable]
public class StatModifier
{
    public StatType Stat;
    public int Value;
}