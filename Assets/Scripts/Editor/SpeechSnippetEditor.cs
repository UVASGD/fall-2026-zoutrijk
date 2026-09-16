using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpeechSnippet))]
public class SpeechSnippetEditor : ScriptableObjectButtonEditor<SpeechSnippet>
{
    protected override string HeaderText => "Snippet Editor";

    protected override string[] ButtonLabels => new string[]
    {
        "Trait Threshold",
        "Relative Manpower",
        // "Enemy Trait",
        // "Rematch"
    };

    protected override Action<SpeechSnippet>[] ButtonActions => new Action<SpeechSnippet>[]
    {
        (s) => s.AddCondition(new PlayerTraitReqSnippet()),
        (s) => s.AddCondition(new RelativeNumbersSnippet())
    };
}