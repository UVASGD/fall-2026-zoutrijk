using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FieldArmy : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] public List<CampaignUnit> units;
    public Region currentRegion {get; private set;}
    public MapFaction owner {get; private set;}
    [SerializeField] private Noble general;
    public Noble General => general; //for read-only UI purposes
    
    [SerializeField] string startingRegion;
    [SerializeField] string startingOwner;
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

        foreach(var unit in units)
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
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("FieldArmy mouse entered");
        CampaignMapManager.i.UpdateArmy(this);
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