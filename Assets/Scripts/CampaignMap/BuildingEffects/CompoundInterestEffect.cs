using UnityEngine;

public class CompoundInterestEffect : GenericTurnEndEffect
{
    [SerializeField] float interestRate;
    [SerializeField] float initialInvestment;
    public override void OnEndTurnEffect(MapCity ownerCity)
    {
        initialInvestment *= (1 + interestRate);
        ownerCity.owner.IncrementMoney((int)initialInvestment);
    }
}