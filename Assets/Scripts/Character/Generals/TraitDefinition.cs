
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Logic for a trait's accruement of points and each of its levels.
/// </summary>
[CreateAssetMenu(menuName = "Traits/Create New Trait Definition")]
public class TraitDefinition : ScriptableObject
{
    [Header("ID: Internal string associated with this type of trait.")]
    [SerializeField] public string TraitID;
    [SerializeField] public List<TraitLevel> levels; //make sure they are sorted in order!
    [SerializeField] public List<string> mutallyExclusiveTraits; //traits that cannot be held by the same character at the same time.
    /// <summary>
    /// Returns the appropriate level for the number of points towards this trait.
    /// </summary>
    public TraitLevel GetTraitLevelForPoints(int points)
    {
        TraitLevel highestLevel = null;
        if (points >= 0)
        {
            //check positive
            foreach (var level in levels)
            {
                if (level.requiredPoints < 0) continue;
                if (points >= level.requiredPoints) highestLevel = level;
                else break;
            }
        }
        else //might be a better way to do this to handle negatives?
        {
            //check negative
            foreach (var level in levels)
            {
                if (level.requiredPoints > 0) break;
                if (points <= level.requiredPoints) return level;
            }
        }

        return highestLevel;
    }
}