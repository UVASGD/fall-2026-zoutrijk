using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A chunk of a speech that only applies if the correct context is met.
/// </summary>
[CreateAssetMenu(fileName = "NewSnippet", menuName = "Speech/Create New Speech Snippet")]
public class SpeechSnippet : ScriptableObject
{
    [SerializeField] SnippetType snippetType; //whether the trait is intro, body, or outro text
    [TextArea] [SerializeField] List<string> snippetText; //the content of the snippet (formatted as a text area in the inspector)
    [SerializeField] float snippetChance; //if all conditions are met, what is the chance that this snippet is chosen for a speech
    [SerializeReference] List<SnippetCondition> conditions; //the conditions for causing the snippet to trigger

    /// <summary>
    /// Checks if the snippet should fire based on the list of snippet conditions.
    /// </summary>
    /// <param name="orator"></param>
    public bool CheckSnippetCondition(BattleContext context)
    {
        foreach(var condition in conditions)
        {
            bool conditionResult = condition.OnCauseCheck(context);
            if((!conditionResult && !condition.ORCause) || conditionResult && condition.InvertLogic && !condition.ORCause)
            {
                return false; //a condition failed that was not an OR cause
            }
            else if((conditionResult && condition.ORCause) || (!conditionResult && condition.InvertLogic && condition.ORCause))
            {
                return true; //automatic pass due to an OR cause being met
            }
        }

        return true;
    }

    public void AddCondition(SnippetCondition condition)
    {
        conditions.Add(condition);
    }
}

/// <summary>
/// Where in the speech this snippet should be said.
/// </summary>
public enum SnippetType
{
    Intro,
    Body,
    Outro
}