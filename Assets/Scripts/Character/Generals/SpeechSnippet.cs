using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// A chunk of a speech that only applies if the correct context is met.
/// </summary>
[CreateAssetMenu(fileName = "NewSnippet", menuName = "Speech/Create New Speech Snippet")]
public class SpeechSnippet : ScriptableObject
{
    [SerializeField] SnippetType snippetType; //whether the trait is intro, body, or outro text
    public SnippetType SnippetType => snippetType;
    [TextArea(1,3)] [SerializeField] List<string> snippetText; //the content of the snippet (formatted as a text area in the inspector)
    public List<string> SnippetText => snippetText;
    [SerializeField] float snippetChance; //if all conditions are met, what is the chance that this snippet is chosen for a speech
    [SerializeReference] List<SnippetCondition> conditions; //the conditions for causing the snippet to trigger

    public bool RollSnippetChance()
    {
        return UnityEngine.Random.value < snippetChance;
    }

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

    [HideInInspector] public List<string> exportString;
    /// <summary>
    /// Replaces dynamic variable headers with appropriate 
    /// </summary>
    /// <param name="battleContext"></param>
    public void SetExportString(BattleContext battleContext)
    {
        exportString = new List<string>();
        foreach(string line in SnippetText)
        {
            //replace the variables via string.format like so
            string formattedLine = String.Format(
            line,
            battleContext.enemyGeneral.noble.House.HouseName, battleContext.playerGeneral.noble.House.HouseName,
            battleContext.enemyGeneral.FullName, battleContext.playerGeneral.FullName,
            battleContext.currentLocation
            );

            exportString.Add(formattedLine);
        }
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