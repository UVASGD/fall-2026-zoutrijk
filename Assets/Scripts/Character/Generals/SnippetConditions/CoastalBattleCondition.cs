using UnityEngine;

public class CoastalBattleCondition : SnippetCondition
{
    [SerializeField] bool shouldBeCoastal;
    public override bool OnCauseCheck(BattleContext context)
    {
        return context.coastalBattle == shouldBeCoastal;
    }
}