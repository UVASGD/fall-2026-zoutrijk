using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GroupUI : MonoBehaviour
{
    public static GroupUI i;
    [SerializeField] private int groupCap = 9; //cap for the number of unit groups allowed at a time (1-9)
    [SerializeField] private GameObject graphicsParent;
    [SerializeField] private GameObject groupCardPrefab;
    [SerializeField] private TMP_Text unitsSelectedText;
    private List<SoldierGroup> soldierGroups = new List<SoldierGroup>();
    private List<GroupCard> groupCards = new List<GroupCard>();
    
    private void Awake()
    {
        if (i == null)
        {
            i = this;
        }
    }

    private void OnEnable()
    {
        FieldCharacter.onFieldCharacterDied += RemoveUnitFromGroups;
    }

    private void OnDisable()
    {
        FieldCharacter.onFieldCharacterDied -= RemoveUnitFromGroups;
    }

    public void AddGroup(SoldierGroup newGroup)
    {
        if (newGroup == null)
        {
            return;
        }

        RemoveDeadUnitsFromGroups();
        RemoveEmptyGroups();

        List<FieldCharacter> unitsToAssign = newGroup.AssignedSoldiers
            .Where(unit => unit != null && !unit.IsDead)
            .Distinct()
            .ToList();

        if (unitsToAssign.Count == 0)
        {
            return;
        }

        int groupsEmptiedByAssignment = soldierGroups.Count(group =>
            group.AssignedSoldiers.All(unit => unitsToAssign.Contains(unit)));
        if (soldierGroups.Count - groupsEmptiedByAssignment >= groupCap)
        {
            return;
        }

        foreach (var group in soldierGroups)
        {
            foreach (var unit in unitsToAssign)
            {
                group.RemoveSoldier(unit);
            }
        }

        RemoveEmptyGroups();
        newGroup = new SoldierGroup(unitsToAssign);
        newGroup.UpdateGroupNumber(soldierGroups.Count);
        soldierGroups.Add(newGroup);

        var groupCard = Instantiate(groupCardPrefab, graphicsParent.transform).GetComponent<GroupCard>();
        groupCard.groupNumber = newGroup.groupNumber;
        groupCard.onCardClicked += SelectGroup;
        groupCards.Add(groupCard);
    }

    /// <summary>
    /// Updates the interface based on if the player has click-dragged units.
    /// </summary>
    /// <param name="n"></param>
    /// <param name="cancel"></param>
    public void UpdateNumberOfSelectedUnits(int n, bool cancel = false)
    {
        unitsSelectedText.gameObject.SetActive(!cancel);
        unitsSelectedText.text = $"{n} units selected";

        if(n == 1) unitsSelectedText.text = $"{n} unit selected"; //case for singular unit
    }

    /// <summary>
    /// Selects a group of units when a group card is clicked.
    /// </summary>
    private void SelectGroup(int groupNumber)
    {
        RemoveDeadUnitsFromGroups();
        RemoveEmptyGroups();

        if (groupNumber < 0 || groupNumber >= soldierGroups.Count)
        {
            return;
        }

        if(GlobalEditorSettings.i.RichDebugLogs) Debug.Log($"Group {groupNumber} selected");
        MouseController.i.SetSelectedUnits(soldierGroups[groupNumber].AssignedSoldiers);
    }

    private void RemoveUnitFromGroups(FieldCharacter unit)
    {
        foreach (var group in soldierGroups)
        {
            group.RemoveSoldier(unit);
        }

        RemoveEmptyGroups();
    }

    private void RemoveDeadUnitsFromGroups()
    {
        foreach (var group in soldierGroups)
        {
            group.RemoveDeadSoldiers();
        }
    }

    private void RemoveEmptyGroups()
    {
        for (int i = soldierGroups.Count - 1; i >= 0; i--)
        {
            if (soldierGroups[i].AssignedSoldiers.Count > 0)
            {
                continue;
            }

            soldierGroups.RemoveAt(i);
            GroupCard groupCard = groupCards[i];
            groupCards.RemoveAt(i);
            if (groupCard != null)
            {
                Destroy(groupCard.gameObject);
            }
        }

        for (int i = 0; i < soldierGroups.Count; i++)
        {
            soldierGroups[i].UpdateGroupNumber(i);
            groupCards[i].groupNumber = i;
        }
    }
}