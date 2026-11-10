using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpeechSnippet))]
public class SpeechSnippetEditor : ScriptableObjectButtonEditor<SpeechSnippet>
{
    protected override string HeaderText => "Snippet Editor";

    protected override string[] ButtonLabels => new string[]
    {
        //some example conditions are commented out (to be implemented later)

        "Trait Threshold",
        "Relative Manpower",
        "Enemy Trait",
        "Coastal Battle",
        "Player attacking"
    };

    protected override Action<SpeechSnippet>[] ButtonActions => new Action<SpeechSnippet>[]
    {
        (s) => s.AddCondition(new PlayerTraitReqSnippet()),
        (s) => s.AddCondition(new RelativeNumbersSnippet()),
        (s) => s.AddCondition(new EnemyTraitCondition()),
        (s) => s.AddCondition(new CoastalBattleCondition()),
        (s) => s.AddCondition(new AttackingOrDefendingTraitReq())
    };
}