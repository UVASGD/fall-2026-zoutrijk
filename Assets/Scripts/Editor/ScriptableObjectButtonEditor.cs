using System;
using UnityEditor;
using UnityEngine;
/// <summary>
/// The final boss of abstraction. Simply inherit this script and change a couple of values to make an entire editor that adds to the list target of choice.
/// </summary>
public abstract class ScriptableObjectButtonEditor<T> : Editor where T : UnityEngine.Object
{
    protected abstract string HeaderText { get; }
    protected abstract string[] ButtonLabels { get; }
    protected abstract Action<T>[] ButtonActions { get; }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); //draw the default inspector

        //cast to specific type
        var typedTarget = target as T;
        if(typedTarget == null) return; //null check

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(HeaderText, EditorStyles.boldLabel);

        DrawTripleButtons(typedTarget, ButtonLabels, ButtonActions);
    }

    private void DrawTripleButtons(T targetItem, string[] labels, Action<T>[] actions)
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
                    {
                        // Record the undo state BEFORE making the change
                        Undo.RecordObject(targetItem, $"Added {labels[idx]}");
                        
                        // Execute the specific action, passing in the target
                        actions[idx]?.Invoke(targetItem);
                        
                        // Tell Unity the object has changed so it saves the new list items
                        EditorUtility.SetDirty(targetItem); 
                    }
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