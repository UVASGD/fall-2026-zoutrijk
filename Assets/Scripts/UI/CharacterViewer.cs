using System;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class CharacterViewer : MonoBehaviour
{
    [SerializeField] Image portrait;
    [SerializeField] TMP_Text nameText;
    [SerializeField] GameObject traitTextPrefab; //the prefab that is instantiated (then .getComponent<TextHoverTrigger>()) to represent traits
    [SerializeField] Transform traitTextParent;
    [SerializeField] GameObject graphicsParent;

    [Header("Stat Icon Spawning")]
    [SerializeField] List<Sprite> statIcons;
    [SerializeField] List<Transform> statIconParents;
    [SerializeField] GameObject statImagePrefab;

    /// <summary>
    /// Updates the character viewer UI to display the general's portrait and traits
    /// </summary>
    /// <param name="general"></param>
    public void ViewCharacter(General general)
    {
        //View a character's traits in the UI.
        Noble noble = general.noble;
        nameText.text = general.FullName;
        portrait.sprite = noble?.Portrait; //small null check incase there isn't an associated noble

        //Refresh the traits list and refill with general's current trait levels. Hover text should be configured for each trait flavor text.
        ZUtilities.DestroyAllChildren(traitTextParent.gameObject);
        foreach (var parent in statIconParents)
        {
            ZUtilities.DestroyAllChildren(parent.gameObject);
        }

        //instantiate each trait text
        foreach (var trait in general.GetAllTraits())
        {
            GameObject traitTextObj = Instantiate(traitTextPrefab, traitTextParent);
            TextHoverTrigger hoverTrigger = traitTextObj.GetComponent<TextHoverTrigger>();
            hoverTrigger.tooltipText = $"{trait.CurrentLevel.flavorText} ({trait.CurrentLevel.GetResultantString()})";
            hoverTrigger.gameObject.GetComponent<TMP_Text>().text = $"{trait.CurrentLevel.levelName}";
        }

        //now assign each stat by instantiating its icon to correspond with its value
        foreach (StatType stat in Enum.GetValues(typeof(StatType)))
        {
            int statValue = general.GetStatType(stat);
            if (statValue <= 0) continue; //don't instantiate if there is none
            statValue = math.min(10, statValue); //cap at 10 just in case it didn't elsewhere
            for(int i = 0; i < statValue; i++)
            {
                Image instantiated = Instantiate(statImagePrefab, statIconParents[(int)stat]).GetComponent<Image>();
                instantiated.sprite = statIcons[(int)stat]; //instantiate the image, parent it to the vertical layout group, and set its icon
            }
        }
    }

    public void OpenMenu(bool open)
    {
        graphicsParent.SetActive(open);
    }
}
