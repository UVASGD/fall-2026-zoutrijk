using System.Collections.Generic;
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
    
    private void Awake()
    {
        if (i == null)
        {
            i = this;
        }
    }

    public void AddGroup(SoldierGroup newGroup)
    {
        if (soldierGroups.Count < groupCap)
        {
            soldierGroups.Add(newGroup);
            var groupCard = Instantiate(groupCardPrefab, graphicsParent.transform).GetComponent<GroupCard>();
            groupCard.groupNumber = newGroup.groupNumber;

            groupCard.onCardClicked += SelectGroup;
        }
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
        if(GlobalEditorSettings.i.RichDebugLogs) Debug.Log($"Group {groupNumber} selected");
        MouseController.i.SetSelectedUnits(soldierGroups[groupNumber].AssignedSoldiers);
    }
}