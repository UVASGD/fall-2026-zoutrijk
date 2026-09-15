using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// A chunk of a speech that only applies if the correct context is met.
/// </summary>
[CreateAssetMenu(fileName = "NewSnippet", menuName = "Speech/Create New Speech Snippet")]
public class SpeechSnippet : ScriptableObject
{
    [SerializeField] SnippetType snippetType; //whether the trait is intro, body, or outro text
    [SerializeField] SnippetContext snippetContext; //if the snippet is based on a condition of the battle, or a trait the general has
    
    [TextArea(3,10)] [SerializeField] string snippetText; //the content of the snippet (formatted as a text area in the inspector)

    [SerializeField] float snippetChance; //if the snippet's condition is met, the chance that it will be said.

    [Header("Snippet trait: ONLY applies to trait-context snippets")]
    [SerializeField] List<TraitBuilder> associatedTraits; //only applies if the snippet is trait-related

    /// <summary>
    /// Checks if the snippet should fire based on the current orator and battle context.
    /// </summary>
    /// <param name="orator"></param>
    public bool CheckSnippetCondition(General orator)
    {
        if(snippetContext == SnippetContext.Trait)
        {
            //check if the general has any of the traits at this level or higher
            foreach(var trait in associatedTraits)
            {
                if(orator.HasTraitLevel(trait)) return true;
            }

            return false;
        }

        return false;
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

/// <summary>
/// For what reason this snippet is spoken. Trait = general trait, Combat = some detail about the current battle.
/// </summary>
public enum SnippetContext
{
    Trait,
    Combat
}