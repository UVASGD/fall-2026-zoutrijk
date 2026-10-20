using TMPro;
using UnityEngine;

public class UnitDeploymentInfo : MonoBehaviour
{
    [SerializeField] TMP_Text unitName;
    [SerializeField] TMP_Text countText;
    [SerializeField] UnitDeploymentCount unitDeploymentCount;

    public void Init()
    {
        unitName.text = unitDeploymentCount.unitBase.UnitName;
        countText.text = $"x{unitDeploymentCount.count}";
    }
}