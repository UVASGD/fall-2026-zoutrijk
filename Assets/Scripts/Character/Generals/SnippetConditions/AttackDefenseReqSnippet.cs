using UnityEngine;

/// <summary>
/// Condition met if the battle is an attack or defense
/// </summary>
public class AttackDefenseReqSnippet : SnippetCondition
{
    [SerializeField] bool attacking; //whether or not the player is attacking in this battle
    public override bool OnCauseCheck(BattleContext context)
    {
        if(context.playerAttacking == attacking) return true;
        else return false;
    }
}