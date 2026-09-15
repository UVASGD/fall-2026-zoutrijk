using UnityEngine;
public class PlayerTraitReqSnippet : SnippetCondition
{
    [SerializeField] string traitID;
    [SerializeField] int pointThreshold;
    public override bool OnCauseCheck(BattleContext context)
    {
        return context.playerGeneral.HasTraitLevel(new TraitBuilder{traitName = traitID, traitPoints = pointThreshold});
    }
}