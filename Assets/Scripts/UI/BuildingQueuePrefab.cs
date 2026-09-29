using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Component container for BuildingConstructors within the UI. OnClick will invoke the static event for adding to the queue.
/// </summary>
public class BuildingQueuePrefab : MonoBehaviour, IPointerClickHandler
{
    public static event Action<BuildingConstruction> onBuidlingSelected;
    [HideInInspector] public BuildingConstruction associatedConstruction;
    private TextHoverTrigger hoverTrigger;
    public void OnPointerClick(PointerEventData eventData)
    {
        //when clicked, invoke the static event for onBuildingSelected, passing this as a parameter
        onBuidlingSelected?.Invoke(associatedConstruction);

        Destroy(this.gameObject); //destroy this gameobject
    }

    public void setupOptionItem(BuildingConstruction buildingConstruction)
    {
        associatedConstruction = buildingConstruction;
        GetComponent<Image>().sprite = associatedConstruction.buildingToConstruct.BuildingData.icon;
        hoverTrigger = GetComponent<TextHoverTrigger>();
        hoverTrigger.tooltipText = buildingConstruction.buildingToConstruct.GetPanelDisplayText() +
        $"Construction cost: {buildingConstruction.buildingToConstruct.ConstructionCost}"
        + "\n";
    }
}