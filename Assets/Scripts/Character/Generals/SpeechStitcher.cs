using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Tasked with stitching together a combination of speech snippets, creating an (ideally) unique speech for each battle.
/// </summary>
public class SpeechStitcher : MonoBehaviour
{
    
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

        
        return snippetTexts;
    }
}