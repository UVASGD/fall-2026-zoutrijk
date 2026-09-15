using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Information associated with each level of a trait. Modifiers, point requirements, and flavor text.
/// </summary>
[System.Serializable]
public class TraitLevel
{
    public string levelName;
    public EpithetDefinition epithet; //an epithet is a surname replacement if the character has a trait at this level. If null, the character's surname is "of [House Name]".
    [TextArea] public string flavorText;
    [SerializeField] public int requiredPoints;
    public List<StatModifier> Modifiers;

    /// <summary>
    /// Returns a string that describes the modifiers for the trait upon the general. Used in the characterViewer UI
    /// </summary>
    /// <returns></returns>
    public string GetResultantString()
    {
        string result = string.Empty;
        //get a list of the modifier int values and their associated stat types, then format them into a string.
        for (int index = 0; index < Modifiers.Count; index++)
        {
            var modifier = Modifiers[index];
            string sign = modifier.Value >= 0 ? "+" : "-"; //whether or not to put a negative or positive in front
            result += $"{sign}{Mathf.Abs(modifier.Value)} {modifier.Stat}"; //add the value to the string
            if (index < Modifiers.Count - 1) result += ", ";
        }

        return result;
    }
}

[System.Serializable]
public class EpithetDefinition
{
    public string title; //the surname in question
    public int priority; //lower number means it is more likely. Epithets related to titles are lower priority than trait-based ones.
}