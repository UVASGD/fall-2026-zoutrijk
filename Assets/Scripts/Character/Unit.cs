using UnityEngine;

/// <summary>
/// Unit is the class (not monobehavior) that is associated with a FieldCharacter for combat functions. It has a single UnitBase that it performs operations on. This architecture is borrowed from the other turn based RPG I made.
/// </summary>
public class Unit
{
    public int level; //the level of the unit applies
    public UnitBase _base {get; private set;} //this just means it can be accessed but not reassigned by another script (only internally by the constructor). Useful in all sorts of places.
    public Unit(UnitBase _base)
    {
        this._base = _base;
        CalculateStats();
    }

    public void CalculateStats()
    {
        //vitality has its own calculation for an HP value
        vitality = (level * 3) + (_base.Vitality * 5);
        
        agility = level + _base.Agility;
        strength = level + _base.Strength;
    }

    //compiled stats (not base)
    public int vitality {get; private set;}
    public int agility {get; private set;}
    public int strength {get; private set;}
    public int dexterity {get; private set;}
    public int tactics {get; private set;}
}