using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Changes the current battle speed (tick rate multiplier) by invoking a static event on buttonClick
/// </summary>
public class SpeedButton : MonoBehaviour
{
    [SerializeField] private int buttonSpeed = 1;
    private Button button;
    public static event Action<int> onSpeedButtonClicked;
    void OnEnable()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() => onSpeedButtonClicked?.Invoke(buttonSpeed));
    }

    void OnDisable()
    {
        button.onClick.RemoveAllListeners();
    }
}
