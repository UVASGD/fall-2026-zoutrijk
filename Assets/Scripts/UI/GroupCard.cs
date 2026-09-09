using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A UI card that represents a group of soldiers. Stores a single integer, attached to a UI prefab. Integer is grabbed onclick to determine which group to select in the GroupUI script.
/// </summary>
public class GroupCard : MonoBehaviour
{
    public int groupNumber;
    [SerializeField] Button attachedButton;
    public event Action<int> onCardClicked;
    void OnEnable()
    {
        attachedButton.onClick.AddListener(() => onCardClicked?.Invoke(groupNumber));
    }

    void OnDisable()
    {
        attachedButton.onClick.RemoveAllListeners(); //to prevent a memory leak
    }
}