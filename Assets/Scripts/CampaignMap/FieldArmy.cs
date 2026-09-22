using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FieldArmy : MonoBehaviour
{
    [SerializeField] public List<CampaignUnit> units;
    public Region currentRegion { get; private set; }
    public Region targetingRegion { get; private set; } //the region that will be attacked at the end of the turn
    public MapFaction owner { get; private set; }
    [SerializeField] private General general;
    public General General => general; //for read-only UI purposes
    private float remainingMovement;
    private float movementRange => ZUtilities.BASE_MOVEMENT_RANGE + (ZUtilities.DISCIPLINE_MOVEMENT_RANGE_MULTIPLIER * ZUtilities.BASE_MOVEMENT_RANGE * general.GetStatType(StatType.Discipline));  //movement range is derived from base range, plus 5% for each point of discipline the general has.

    [SerializeField] string startingRegion;
    [SerializeField] string startingOwner;
    [SerializeField] GameObject movementRangeIndicator; //the aura that indicates remaining movement
    /// <summary>
    /// updates the current region the unit is in.
    /// </summary>
    /// <param name="newRegion"></param>
    public void UpdateCurrentRegion(Region newRegion)
    {
        currentRegion = newRegion;
    }

    /// <summary>
    /// sums up the individual maintenance costs of every unit in the army.
    /// </summary>
    /// <returns></returns>
    public float GetUnitMaintenance()
    {
        float totalMaintenance = 0f;

        foreach (var unit in units)
        {
            totalMaintenance += unit.GetUnitMaintenance();
        }

        return totalMaintenance;
    }

    /// <summary>
    /// Sets up a fieldarmy for use after its creation in a city.
    /// </summary>
    /// <param name="owner"></param>
    public void InitializeArmy()
    {
        this.owner = CampaignMapManager.i.getFactionByCode(startingOwner);
        this.currentRegion = CampaignMapManager.i.getRegionByCode(startingRegion);
        OnTurnStart();
    }

    /// <summary>
    /// Function called on every army when the turn is started.
    /// </summary>
    public void OnTurnStart()
    {
        remainingMovement = movementRange; //reset movement range
        RefreshMovementIndicator();
    }

    /// <summary>
    /// Called when a unit is selected in CampaignMapManager.cs
    /// </summary>
    public void OnUnitSelected(bool selected)
    {
        movementRangeIndicator.SetActive(selected);
        RefreshMovementIndicator();
    }

    /// <summary>
    /// Move an army to a position on the map. Returns false if the movement could not be completed.
    /// </summary>
    /// <param name="targetPos"></param>
    public bool MoveArmy(Region highlightedRegion, Vector2 targetPos)
    {
        //first check if the position is too far away
        float distance = Vector2.Distance(targetPos, transform.position);
        if (distance > remainingMovement || highlightedRegion == null)
        {
            if(GlobalEditorSettings.i.RichDebugLogs) Debug.Log($"{name} does not have {distance} movement range, it only has {remainingMovement}.");
            return false; //null check on highlightedRegion prevents movement onto the sea. 
        }

        ZUtilities.Generic2DLerp(this, gameObject, targetPos, 1f); //lerptime of 1 second
        remainingMovement -= distance;
        movementRangeIndicator.SetActive(false);

        return true;
    }

    /// <summary>
    /// Refreshes the movement indicator aura according to the remaining Unit movement.
    /// </summary>
    public void RefreshMovementIndicator()
    {
        float indicatorScale = remainingMovement * 2; //because of a circle's radius
        movementRangeIndicator.transform.localScale = new Vector3(indicatorScale, indicatorScale, 1f);
    }
}


/// <summary>
/// stores the details of a single unit in an army for use on the campaign map
/// </summary>
[Serializable]
public class CampaignUnit
{
    [SerializeField] private UnitArmor armor;
    [SerializeField] private float unitHealth; //health of the unit after various battles
    [SerializeField] private UnitBase attachedUnit; //the associated unitBase for this CampaignUnit

    /// <summary>
    /// setter for the unit's armor.
    /// </summary>
    /// <param name="armor"></param>
    public void updateArmor(UnitArmor armor)
    {
        this.armor = armor;
    }

    /// <summary>
    /// setter for the new unit's health (percentage from 0 -> 1)
    /// </summary>
    /// <param name="newHealth"></param>
    public void updateUnitHealth(float newHealth)
    {
        this.unitHealth = newHealth;
    }

    public CampaignUnit(UnitArmor startingArmor, UnitBase attachedUnit)
    {
        this.unitHealth = 1f; //constructing a unit 
    }

    public float GetUnitMaintenance()
    {
        return (float)attachedUnit.UnitMaintenance * unitHealth;
    }
}