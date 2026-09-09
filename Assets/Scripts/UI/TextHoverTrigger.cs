using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A UI object that triggers a tooltip which displays text.
/// </summary>
public class TextHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static event Action<string> onHoverEnter;
    public static event Action onHoverExit;
    public string tooltipText; //public so it can be assigned upon instantiation of the prefab by whichever viewer.

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHoverEnter?.Invoke(tooltipText);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        onHoverExit?.Invoke();
    }
}