using TMPro;
using UnityEngine;

/// <summary>
/// Stores behaviors pertaining to campaign UI. Exists as a means to prevent the CampaignMapManager from becoming ridiculously large in the future (encapsulation)
/// </summary>
public class CampaignUI : MonoBehaviour
{
    #region UI elements
    [SerializeField] TMP_Text contextualHighlightText;
    [SerializeField] RegionDetailsMenu regionDetails;
    [SerializeField] TMP_Text yearText;
    [SerializeField] int startingYear;    
    [SerializeField] GameObject highlightCursor; //a cursor gameobject that points at the current city/fieldarmy
    #endregion

    /// <summary>
    /// updates the highlighted region UI. Content changes based on the current mapmode.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="currentMode"></param>
    public void UpdateHighlightedRegionUI(Region region, Mapmode currentMode)
    {
        if(currentMode == Mapmode.political)
            contextualHighlightText.text = region != null ? $"{region.RegionName} ({region.owner.fBase.FactionAdjective})" : string.Empty;
        else if(currentMode == Mapmode.terrain)
            contextualHighlightText.text = region != null ? $"{region.RegionName} : {region.RegionalTerrain.TerrainName}" : string.Empty;
    }

    /// <summary>
    /// Updates the context text prompt for the currently selected army.
    /// </summary>
    /// <param name="army"></param>
    public void UpdateHighlightedArmyUI(FieldArmy army)
    {
        contextualHighlightText.text = army != null ? $"{army.General.NobleName}'s Army in {army.currentRegion.RegionName} ({army.owner.fBase.FactionAdjective})" : string.Empty;
    }

    /// <summary>
    /// Update the contextual text prompt for the selected city.
    /// </summary>
    /// <param name="city"></param>
    public void UpdateHighlightedCityUI(MapCity city)
    {
        contextualHighlightText.text = city != null ? $"{city.cityName} ({city.owner.fBase.FactionAdjective})" : string.Empty;
    }

    /// <summary>
    /// Exposes regional details using the generic object details menu.
    /// </summary>
    /// <param name="region"></param>
    public void ShowRegionDetails(Region region)
    {
        regionDetails.EnableDetailsGraphics(true);

        regionDetails.ShowRegionDetails(region.RegionName, region.LocalNoble.NobleName, region.currentPopulation, region.RegionalBanner);
    }

    /// <summary>
    /// Disables regional details in the UI
    /// </summary>
    public void HideRegionDetails()
    {
        regionDetails.EnableDetailsGraphics(false);
    }
    
    /// <summary>
    /// Takes an internal turncount from the campaignmanager and formats it as a year on the world's calendar.
    /// </summary>
    /// <param name="turnCount"></param>
    public void UpdateYearText(int turnCount)
    {
        yearText.text = $"{turnCount + startingYear} P.E.";
    }

    /// <summary>
    /// Enables and places the worldspace highlight cursor on a highlighted object.
    /// </summary>
    /// <param name="target"></param>
    public void PlaceHighlightCursor(Transform target)
    {
        highlightCursor.SetActive(true);
        highlightCursor.transform.position = new Vector3(target.localPosition.x, target.localPosition.y, 0);
    }

    /// <summary>
    /// When an object is unselected, disable the highlight cursor.
    /// </summary>
    public void DisableHighlightCursor()
    {
        highlightCursor.SetActive(false);
    }
}