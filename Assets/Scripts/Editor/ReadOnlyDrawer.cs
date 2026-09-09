using UnityEngine;
using UnityEditor;
/// <summary>
/// Adds a custom [ReadOnly] attribute so there are visible but not editable aspects to the inspector
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label.text += " (READ-ONLY)";
        GUI.enabled = false;

        EditorGUI.PropertyField(position, property, label, true);

        GUI.enabled = true;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        //Recalculating the property height to prevent an overlap
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
