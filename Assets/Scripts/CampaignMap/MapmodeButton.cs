using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MapmodeButton : MonoBehaviour
{
    [SerializeField] Mapmode mapmode;
    public event Action<Mapmode> onMapmodeSelected;
    private Button attachedButton;
    void Awake()
    {
        attachedButton = GetComponent<Button>();
    }
    void OnEnable()
    {
        attachedButton = GetComponent<Button>();

        if (attachedButton != null)
        {
            attachedButton.onClick.AddListener(InvokeMapmode);
        }
    }

    void OnDisable()
    {
        if (attachedButton != null)
        {
            attachedButton.onClick.RemoveListener(InvokeMapmode);
        }
    }

    private void InvokeMapmode()
    {
        onMapmodeSelected?.Invoke(mapmode);
    }
}