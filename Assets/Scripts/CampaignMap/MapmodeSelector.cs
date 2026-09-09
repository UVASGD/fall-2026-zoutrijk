using System.Collections.Generic;
using UnityEngine;

public class MapmodeSelector : MonoBehaviour
{
    [SerializeField] List<MapmodeButton> mapmodeButtons;
    void OnEnable()
    {
        foreach (var button in mapmodeButtons)
        {
            if (button != null)
            {
                button.onMapmodeSelected += OnMapmodeButtonPressed;
            }
        }
    }
    void OnDisable()
    {
        foreach (var button in mapmodeButtons)
        {
            if (button != null)
            {
                button.onMapmodeSelected -= OnMapmodeButtonPressed;
            }
        }
    }

    private void OnMapmodeButtonPressed(Mapmode mapmode)
    {
        MapmodeManager.i.UpdateMapmode(mapmode);
    }
}
