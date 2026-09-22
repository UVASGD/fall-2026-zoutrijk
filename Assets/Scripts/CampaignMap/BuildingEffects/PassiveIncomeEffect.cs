using UnityEngine;

[System.Serializable]
public class PassiveIncomeEffect : GenericTurnEndEffect
{
    [SerializeField] int passiveIncome;
    public override void OnEndTurnEffect(MapCity ownerCity)
    {
        ownerCity.owner.IncrementMoney(passiveIncome);
    }
}