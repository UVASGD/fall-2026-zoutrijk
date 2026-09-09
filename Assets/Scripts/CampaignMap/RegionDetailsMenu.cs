using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegionDetailsMenu : MonoBehaviour
{
    [SerializeField] TMP_Text RegionNameText;
    [SerializeField] TMP_Text localNobleText;
    [SerializeField] TMP_Text populationText;
    [SerializeField] Image bannerImage;

    [SerializeField] GameObject graphicsParent;
    public void EnableDetailsGraphics(bool enable)
    {
        graphicsParent.SetActive(enable);
    }
    public void ShowRegionDetails(string name, string noble, int population, Sprite regionBanner)
    {
        RegionNameText.text = name;
        localNobleText.text = $"Local Noble: {noble}";
        populationText.text = $"Population: {population}";
        bannerImage.sprite = regionBanner;
    }
}