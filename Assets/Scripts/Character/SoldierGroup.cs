using System.Collections.Generic;
using UnityEngine;

public class SoldierGroup
{
    private List<FieldCharacter> assignedSoldiers = new List<FieldCharacter>();
    public List<FieldCharacter> AssignedSoldiers => assignedSoldiers;
    private bool inFormation = false;

    public int groupNumber {get; private set;}

    /// <summary>
    /// The grid of how the soldierGroup is assigned.
    /// </summary>
    private Vector2 groupDimension = new Vector2();

    public void AssignSoldier(FieldCharacter soldier)
    {
        if (!assignedSoldiers.Contains(soldier))
        {
            assignedSoldiers.Add(soldier);
        }
    }
    public void AssignSoldiers(List<FieldCharacter> soldiers)
    {
        foreach (var soldier in soldiers)
        {
            AssignSoldier(soldier);
        }
    }

    public void UpdateGroupNumber(int newNumber)
    {
        groupNumber = newNumber;
    }
    
    public SoldierGroup(List<FieldCharacter> soldiers)
    {
        AssignSoldiers(soldiers);
    }
}
