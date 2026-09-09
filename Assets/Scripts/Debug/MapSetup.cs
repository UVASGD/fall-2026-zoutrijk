using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSetup : MonoBehaviour
{
    [SerializeField] List<FieldCharacter> placementCharacters;

    void Start()
    {
        
    }
    void Update()
    {
        
    }
    private IEnumerator PlaceTestCharacters()
    {
        foreach(var character in placementCharacters)
        {
            DebugView.i.UpdateDebugViewText($"Place character: {character.internalCharacterName}");
            bool characterPlaced = false;

            yield return new WaitUntil(() => characterPlaced); // ()=> is a function notation that recurringly checks until characterplaced is true
        }
    }
}
