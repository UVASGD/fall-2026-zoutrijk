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
        if (soldier != null && !soldier.IsDead && !assignedSoldiers.Contains(soldier))
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

    public void RemoveSoldier(FieldCharacter soldier)
    {
        assignedSoldiers.Remove(soldier);
    }

    public void RemoveDeadSoldiers()
    {
        assignedSoldiers.RemoveAll(soldier => soldier == null || soldier.IsDead);
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
