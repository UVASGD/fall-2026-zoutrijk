using System;
using UnityEngine;

[Serializable]
public class Trait
{
    [SerializeField] public TraitDefinition TraitDef;
    [SerializeField] public int CurrentPoints;
    public TraitLevel CurrentLevel => TraitDef?.GetTraitLevelForPoints(CurrentPoints);

    public Trait(TraitDefinition def)
    {
        this.TraitDef = def;
        CurrentPoints = 0;
    }

    public void AddPoints(int points)
    {
        CurrentPoints += points;
    }
}
