using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The singleton that has a DB of all trait defs in the game, and can give a general points towards a trait.
/// </summary>
public class TraitDB : MonoBehaviour
{
    public static TraitDB i;
    
    static Dictionary<string, TraitDefinition> traitDB; //a database that automatically grabs all traits on awake

    void Awake()
    {
        Init();

        if(i==null) i = this;
    }

    private void Init()
    {
        traitDB = new Dictionary<string, TraitDefinition>();

        TraitDefinition[] defArray = Resources.LoadAll<TraitDefinition>("Traits"); //loads ALL traitDefs from the resources folder.

        for(int i = 0; i < defArray.Length; i++)
        {
            if (traitDB.ContainsKey(defArray[i].TraitID))
            {
                Debug.LogError("TraitDB contains a duplicate ID");
                continue;
            }

            traitDB.Add(defArray[i].TraitID, defArray[i]);
        }

        if(GlobalEditorSettings.i.RichDebugLogs) Debug.Log($"TraitDB cached {traitDB.Count} TraitDefs");
    }

    public TraitDefinition GetTraitByID(string traitID)
    {
        if(traitDB.ContainsKey(traitID)) return traitDB[traitID];
        else
        {
            Debug.LogWarning($"TraitController attempted to call for trait {traitID} that does not exist!");
            return null;
        }
    }
}