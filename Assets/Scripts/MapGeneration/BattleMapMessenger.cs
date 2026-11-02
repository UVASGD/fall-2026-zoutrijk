using System.Collections.Generic;
using UnityEngine;

public class BattleMapMessenger : MonoBehaviour
{
    //object that serves as the messenger between the campaign map and battle map.
    public BattleMapSeedData seedData;
    public BattleArmyData playerArmy;
    public BattleArmyData enemyArmy;
    [HideInInspector] public BattleContext battleContext;
    public List<UnitDeploymentCount> unitDeploymentCounts = new List<UnitDeploymentCount>();

    public void SetUnitDeploymentCounts(BattleArmyData army)
    {
        if (unitDeploymentCounts.Count > 0)
        {
            return;
        }

        if (army == null)
        {
            return;
        }

        foreach (CampaignUnit campaignUnit in army.Units)
        {
            if (campaignUnit == null || campaignUnit.UnitBase == null)
            {
                continue;
            }

            UnitDeploymentCount deploymentCount = unitDeploymentCounts.Find(
                item => item.unitBase == campaignUnit.UnitBase);

            if (deploymentCount == null)
            {
                unitDeploymentCounts.Add(new UnitDeploymentCount(campaignUnit.UnitBase, 1));
            }
            else
            {
                deploymentCount.count++;
            }
        }
    }
}

[System.Serializable]
public class BattleArmyData
{
    [SerializeField] private List<CampaignUnit> units = new List<CampaignUnit>();
    public IReadOnlyList<CampaignUnit> Units => units;
    public int UnitCount => units.Count;

    public BattleArmyData(FieldArmy source)
    {
        if (source == null || source.units == null)
        {
            return;
        }

        foreach (CampaignUnit unit in source.units)
        {
            if (unit != null)
            {
                units.Add(unit.DeepClone());
            }
        }
    }

    public float GetUnitMaintenance()
    {
        float totalMaintenance = 0f;
        foreach (CampaignUnit unit in units)
        {
            totalMaintenance += unit.GetUnitMaintenance();
        }

        return totalMaintenance;
    }
}

/// <summary>
/// contains a UnitBase and a count integer, used in the deployment phase
/// </summary>
[System.Serializable]
public class UnitDeploymentCount
{
    public UnitBase unitBase;
    public int count;

    public UnitDeploymentCount(UnitBase unitBase, int count)
    {
        this.unitBase = unitBase;
        this.count = count;
    }

    public bool TryDecrement()
    {
        if (count <= 0)
        {
            return false;
        }

        count--;
        return true;
    }
}