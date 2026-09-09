using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TextHover : MonoBehaviour
{
    [SerializeField] GameObject hoverParent;
    [SerializeField] TMP_Text tooltipText;

    void OnEnable()
    {
        TextHoverTrigger.onHoverEnter += ShowTooltip;
        TextHoverTrigger.onHoverExit += HideTooltip;
    }

    void OnDisable()
    {
        TextHoverTrigger.onHoverEnter -= ShowTooltip;
        TextHoverTrigger.onHoverExit -= HideTooltip;
    }

    public void ShowTooltip(string text)
    {
        hoverParent.SetActive(true);
        tooltipText.text = text;
    }

    public void HideTooltip()
    {
        hoverParent.SetActive(false);
    }

    void Update() //if the hover parent is active, make sure it follows the mouse position.
    {
        if (hoverParent.activeSelf)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            hoverParent.transform.position = mousePosition;
        }
    }
}