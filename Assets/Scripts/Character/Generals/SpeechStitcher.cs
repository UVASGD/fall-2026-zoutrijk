using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tasked with stitching together a combination of speech snippets, creating an (ideally) unique speech for each battle.
/// </summary>
public class SpeechStitcher : MonoBehaviour
{
    /// <summary>
    /// Uses the general's battlefield announcer to read the speech sequentially, with a small delay between each line.
    /// </summary>
    /// <param name="lines"></param>
    public IEnumerator ReadSpeech(List<string> lines)
    {
        foreach(string line in lines)
        {
            BattlefieldAnnouncer.generalSpeechBubble.BattlefieldAnnouncement(line);

            yield return new WaitForSeconds(4f);
        }

        yield return null;
    }

    /// <summary>
    /// Uses the current BattleContext to create a new dynamic general speech.
    /// </summary>
    /// <param name="battleContext"></param>
    /// <returns></returns>
    public List<string> StitchSpeeches(BattleContext battleContext)
    {
        //Stitch together a full list of stitches, including their odds rolls, based on current battle context.

        //streams are the most powerful thing ever created, Exhibit A:
        List<SpeechSnippet> allIntros = SnippetDB.GetAllObjects().Where(s => s.SnippetType == SnippetType.Intro && s.CheckSnippetCondition(battleContext)).ToList();
        List<SpeechSnippet> allBody = SnippetDB.GetAllObjects().Where(s => s.SnippetType == SnippetType.Body && s.CheckSnippetCondition(battleContext)).ToList();
        List<SpeechSnippet> allOutros = SnippetDB.GetAllObjects().Where(s => s.SnippetType == SnippetType.Outro && s.CheckSnippetCondition(battleContext)).ToList();
        
        List<string> snippetTexts = new List<string>();

        foreach(SpeechSnippet bodySnippet in allBody)
        {
            if(bodySnippet.RollSnippetChance())
            {
                snippetTexts.AddRange(bodySnippet.SnippetText);
            }
        }

        //Pick an intro
        if(GlobalEditorSettings.i.RichDebugLogs) Debug.Log($"{allIntros.Count} intros passed this context");
        if(allIntros.Count > 1) snippetTexts.AddRange(ZUtilities.GetRandomElement(allIntros).SnippetText); //pick one random member



        //Pick an outro
        if(GlobalEditorSettings.i.RichDebugLogs) Debug.Log($"{allOutros.Count} outros passed this context");
        if(allOutros.Count > 1) snippetTexts.AddRange(ZUtilities.GetRandomElement(allOutros).SnippetText); //pick one random member

        
        if(GlobalEditorSettings.i.RichDebugLogs) Debug.Log($"Speech stitched to be {snippetTexts.Count} lines long");

        return snippetTexts;
    }
}