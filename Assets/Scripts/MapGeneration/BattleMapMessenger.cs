using UnityEngine;

public class BattleMapMessenger : MonoBehaviour
{
    //object that serves as the messenger between the campaign map and battle map.
    public BattleMapSeedData seedData;
    public FieldArmy playerArmy;
    public FieldArmy enemyArmy;
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
}