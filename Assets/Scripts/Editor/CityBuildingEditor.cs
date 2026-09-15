using System;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(CityBuilding))]
public class CityBuildingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); //draw everything else first

        var dynamicBuilding = target as CityBuilding;

        EditorGUILayout.LabelField("Building End-Turn Effects", EditorStyles.boldLabel);

        var effectLabels = new string[]
        {
            "Passive Income Effect",
            "Trait Chance Impact"
        };

        var causeActions = new Action[]
        {
            () => dynamicBuilding.AddOnTurnEndCause(new PassiveIncomeEffect()),
            () => dynamicBuilding.AddOnTurnEndCause(new TraitChanceEffect())
        };

        DrawTripleButtons(effectLabels, causeActions);
    }

    
    private void DrawTripleButtons(string[] labels, Action[] actions) //copied function from Concert of Europe
    {
        if (labels == null || actions == null) return;
        int count = Math.Min(labels.Length, actions.Length);
        float padding = 18f;
        float third = Mathf.Max(60f, (EditorGUIUtility.currentViewWidth - padding) / 3f);

        for (int i = 0; i < count; i += 3)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < 3; j++)
            {
                int idx = i + j;
                if (idx < count)
                {
                    if (GUILayout.Button(labels[idx], GUILayout.Width(third)))
                        actions[idx]?.Invoke();
                }
                else
                {
                    GUILayout.FlexibleSpace();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}