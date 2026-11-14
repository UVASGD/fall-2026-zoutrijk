using UnityEngine;

public class EnemyTraitCondition : SnippetCondition
{
    [SerializeField] string traitID;
    [SerializeField] int pointThreshold;
    public override bool OnCauseCheck(BattleContext context)
    {
        return context.enemyGeneral.HasTraitLevel(new TraitBuilder { traitName = traitID, traitPoints = pointThreshold });
    }
}
