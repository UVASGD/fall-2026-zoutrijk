using TMPro;
using UnityEngine;

public class DeploymentPhaseUI : MonoBehaviour
{
    [SerializeField] TMP_Text currentPhase;
    [SerializeField] Transform UnitInfoParent;
    [SerializeField] GameObject UnitInfoPrefab;
    
    public void UpdatePhaseText(string text)
    {
        currentPhase.text = text;
    }
}
