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
}

[System.Serializable]
public class EpithetDefinition
{
    public string title; //the surname in question
    public int priority; //lower number means it is more likely. Epithets related to titles are lower priority than trait-based ones.
}