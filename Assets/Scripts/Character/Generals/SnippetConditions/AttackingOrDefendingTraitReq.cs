using UnityEngine;

public class AttackingOrDefendingTraitReq : SnippetCondition
{
    [SerializeField] bool shouldBeAttacking;
    public override bool OnCauseCheck(BattleContext context)
    {
        return shouldBeAttacking == context.playerAttacking;
    }
}
