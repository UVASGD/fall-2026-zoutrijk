using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitDeploymentInfo : MonoBehaviour
{
    [SerializeField] TMP_Text unitName;
    [SerializeField] TMP_Text countText;
    [SerializeField] UnitDeploymentCount unitDeploymentCount;
    [SerializeField] Button iconButton;

    public static event Action<UnitBase> onDeploymentPressed; //static event for when the unitbase is clicked
    public UnitBase UnitBase => unitDeploymentCount != null ? unitDeploymentCount.unitBase : null;

    /// <summary>
    /// For when this UI element is initialized after instantiation
    /// </summary>
    /// <param name="unitDeploymentCount"></param>
    public void Init(UnitDeploymentCount unitDeploymentCount)
    {
        unitName.text = unitDeploymentCount.unitBase.UnitName;
        countText.text = $"x{unitDeploymentCount.count}";

        iconButton.image.sprite = unitDeploymentCount.unitBase.PortraitSprite;

        this.unitDeploymentCount = unitDeploymentCount;

        iconButton.onClick.AddListener(OnDeploymentClicked);
    }

    public void OnDeploymentClicked()
    {
        if (unitDeploymentCount != null && unitDeploymentCount.count > 0)
        {
            onDeploymentPressed?.Invoke(unitDeploymentCount.unitBase);
        }
    }

    public void Refresh()
    {
        if (unitDeploymentCount != null)
        {
            countText.text = $"x{unitDeploymentCount.count}";
            gameObject.SetActive(unitDeploymentCount.count > 0);
        }
    }
}