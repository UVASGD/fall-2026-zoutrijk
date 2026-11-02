using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DeploymentPhaseUI : MonoBehaviour
{
    [SerializeField] TMP_Text currentPhase;
    [SerializeField] Transform UnitInfoParent;
    [SerializeField] GameObject UnitInfoPrefab;
    [SerializeField] Button endDeploymentButton;
    [SerializeField] GameObject graphicsParent;
    private readonly List<UnitDeploymentInfo> deploymentInfos = new List<UnitDeploymentInfo>();

    public static DeploymentPhaseUI i;
    public event System.Action deploymentEnded;

    private void Awake()
    {
        i = this;
        if (endDeploymentButton != null)
        {
            endDeploymentButton.onClick.AddListener(EndDeployment);
        }
    }

    private void OnDestroy()
    {
        UnitDeploymentInfo.onDeploymentPressed -= HandleDeploymentPressed;
    }

    public void Initialize(List<UnitDeploymentCount> deploymentCounts)
    {
        UnitDeploymentInfo.onDeploymentPressed -= HandleDeploymentPressed;
        UnitDeploymentInfo.onDeploymentPressed += HandleDeploymentPressed;
        SetGraphicsActive(true);
        ClearDeploymentInfos();

        UnitDeploymentInfo template = UnitInfoPrefab != null
            ? UnitInfoPrefab.GetComponent<UnitDeploymentInfo>()
            : GetComponentInChildren<UnitDeploymentInfo>(true);

        if (template == null || UnitInfoParent == null)
        {
            Debug.LogError("DeploymentPhaseUI requires a UnitDeploymentInfo prefab and parent.");
            return;
        }

        template.gameObject.SetActive(false);
        foreach (UnitDeploymentCount deploymentCount in deploymentCounts)
        {
            if (deploymentCount == null || deploymentCount.unitBase == null || deploymentCount.count <= 0)
            {
                continue;
            }

            UnitDeploymentInfo info = Instantiate(template, UnitInfoParent);
            info.gameObject.SetActive(true);
            info.Init(deploymentCount);
            deploymentInfos.Add(info);
        }
    }

    private void ClearDeploymentInfos()
    {
        foreach (UnitDeploymentInfo info in deploymentInfos)
        {
            if (info != null)
            {
                Destroy(info.gameObject);
            }
        }

        deploymentInfos.Clear();
    }

    private void HandleDeploymentPressed(UnitBase unitBase)
    {
        BattleSetup.i?.SelectUnitBase(unitBase);
    }

    public bool TryDecrement(UnitBase unitBase)
    {
        UnitDeploymentInfo info = deploymentInfos.Find(item => item.UnitBase == unitBase);
        if (info == null)
        {
            return false;
        }

        info.Refresh();
        return true;
    }

    public void EndDeployment()
    {
        deploymentEnded?.Invoke();
        SetGraphicsActive(false);
    }

    public void EnableGraphics()
    {
        SetGraphicsActive(true);
    }

    public bool IsPointerOverDeploymentUI()
    {
        if (graphicsParent == null || EventSystem.current == null || !graphicsParent.activeInHierarchy)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero
        };
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject == graphicsParent || result.gameObject.transform.IsChildOf(graphicsParent.transform))
            {
                return true;
            }
        }

        return false;
    }

    private void SetGraphicsActive(bool isActive)
    {
        if (graphicsParent == null)
        {
            Debug.LogError("DeploymentPhaseUI requires a graphics parent.");
            return;
        }

        graphicsParent.SetActive(isActive);
    }

    public void UpdatePhaseText(string text)
    {
        if (currentPhase != null)
        {
            currentPhase.text = text;
        }
    }
}