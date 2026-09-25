using TMPro;
using UnityEngine;

/// <summary>
/// User Interface for viewing a city's details and buildings
/// </summary>
public class CityPanel : MonoBehaviour
{
    [SerializeField] TMP_Text cityName;
    [SerializeField] Transform buildingIconParent;
    [SerializeField] GameObject buildingIconPrefab;
    [SerializeField] GameObject graphicsParent;

    public void ShowCityPanel(bool show)
    {
        graphicsParent.SetActive(show);
    }
    public void RefreshPanelData(MapCity mCity)
    {
        cityName.text = mCity.cityName;

        ZUtilities.DestroyAllChildren(buildingIconParent.gameObject);
        
        foreach(var building in mCity.buildings)
        {
            GameObject pref = Instantiate(buildingIconPrefab, buildingIconParent);
            BuildingIcon icon = pref.GetComponent<BuildingIcon>();
            icon.iconImage.sprite = building.BuildingData.icon;
            icon.textHoverTrigger.UpdateTooltipText(building.GetPanelDisplayText());
        }
    }
}
