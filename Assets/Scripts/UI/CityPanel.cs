using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// User Interface for viewing a city's details and buildings
/// </summary>
public class CityPanel : MonoBehaviour
{
    [SerializeField] TMP_Text cityName;
    [SerializeField] TMP_Text cityRecruitablePopulation;
    [SerializeField] Transform buildingIconParent;
    [SerializeField] GameObject buildingIconPrefab;
    [SerializeField] GameObject graphicsParent;
    [SerializeField] GameObject optionPrefab;
    [SerializeField] GameObject constructionOptionsParent;

    private MapCity currentCity;

    void OnEnable()
    {
        BuildingQueuePrefab.onBuidlingSelected += NotifyConstructionSelection;
    }

    public void ShowCityPanel(bool show)
    {
        graphicsParent.SetActive(show);
    }

    private void NotifyConstructionSelection(BuildingConstruction construction)
    {
        currentCity.RequestAdditionToBuildingQueue(construction.buildingToConstruct);
    }

    public void RefreshPanelData(MapCity mCity)
    {
        currentCity = mCity;
        cityName.text = mCity.cityName;
        cityRecruitablePopulation.text = $"Recruitable Population: {mCity.recruitablePopulation}";

        ZUtilities.DestroyAllChildren(buildingIconParent.gameObject);
        
        foreach(var building in mCity.buildings)
        {
            GameObject pref = Instantiate(buildingIconPrefab, buildingIconParent);
            BuildingIcon icon = pref.GetComponent<BuildingIcon>();
            icon.iconImage.sprite = building.BuildingData.icon;
            icon.textHoverTrigger.UpdateTooltipText(building.GetPanelDisplayText());
        }

        RefreshConstructionOptions(mCity);
    }

    private void RefreshConstructionOptions(MapCity mCity)
    {
        ZUtilities.DestroyAllChildren(constructionOptionsParent); //destroy all children

        //get a list of buildable buildings
        List<CityBuilding> available = mCity.availableToBuildBuildings;
        //for each one, instantiate a prefab for the building queue
        foreach(var construction in available)
        {
            BuildingQueuePrefab queuePref = Instantiate(optionPrefab, constructionOptionsParent.transform).GetComponent<BuildingQueuePrefab>();
            queuePref.setupOptionItem(new BuildingConstruction(construction));
        }
    }
}
