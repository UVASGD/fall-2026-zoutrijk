using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterViewer : MonoBehaviour
{
    [SerializeField] Image portrait;
    [SerializeField] TMP_Text nameText;
    [SerializeField] GameObject traitTextPrefab;
    [SerializeField] Transform traitTextParent;

    public void ViewCharacter(General general)
    {
        //View a character's traits in the UI.
        Noble noble = general.noble;
        nameText.text = general.FullName;
        portrait.sprite = noble.Portrait;
    }
}
