using UnityEngine;
using System;

[Serializable]
public class SpeechSnippet : MonoBehaviour
{
    [SerializeField] SnippetType snippetType;
    [SerializeField] SnippetContext snippetContext;
    [SerializeField] string snippetText;
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