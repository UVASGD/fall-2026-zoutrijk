using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterViewer : MonoBehaviour
{
    [SerializeField] Image portrait;
    [SerializeField] TMP_Text nameText;
    [SerializeField] GameObject traitTextPrefab; //the prefab that is instantiated (then .getComponent<TextHoverTrigger>()) to represent traits
    [SerializeField] Transform traitTextParent;
    [SerializeField] GameObject graphicsParent;

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
        foreach(var trait in general.GetAllTraits())
        {
            GameObject traitTextObj = Instantiate(traitTextPrefab, traitTextParent);
            TextHoverTrigger hoverTrigger = traitTextObj.GetComponent<TextHoverTrigger>();
            hoverTrigger.tooltipText = $"{trait.CurrentLevel.flavorText} ({trait.CurrentLevel.GetResultantString()})";
            hoverTrigger.gameObject.GetComponent<TMP_Text>().text = $"{trait.CurrentLevel.levelName}";
        }
    }

    public void OpenMenu(bool open)
    {
        graphicsParent.SetActive(open);
    }
}
