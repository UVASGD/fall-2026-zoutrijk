using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sets up a battle from detached campaign snapshots. Automatically places each type of unit in formation with itself on its half of the map.
/// </summary>
public class BattleSetup : MonoBehaviour
{
    public static BattleSetup i;
    [SerializeField] private FieldCharacter fieldCharacterPrefab;
    [SerializeField] private DeploymentPhaseUI deploymentPhaseUI;
    [SerializeField] SpeechStitcher speechStitcher;
    private UnitBase selectedUnitBase;
    private readonly List<UnitDeploymentCount> deploymentCounts = new List<UnitDeploymentCount>();
    private List<string> deploymentSpeech;

    void Awake()
    {
        if (i == null)
        {
            i = this;
        }

    }

    private void OnDestroy()
    {
        if (MouseController.i != null)
        {
            MouseController.i.deploymentTileClicked -= DeploySelectedUnit;
        }
    }

    void Start()
    {
        if (MouseController.i == null)
        {
            Debug.LogError("BattleSetup requires a MouseController in the scene.");
            return;
        }

        MouseController.i.deploymentTileClicked += DeploySelectedUnit;
        BattleMapMessenger bMessenger = FindAnyObjectByType<BattleMapMessenger>(); //get the battle map messenger

        if (bMessenger == null || bMessenger.playerArmy == null)
        {
            Debug.LogError("BattleSetup requires a BattleMapMessenger with a player army.");
            return;
        }

        SetupBattle(bMessenger.playerArmy, bMessenger.enemyArmy);
    }

    public void SetupBattle(BattleArmyData playerArmy, BattleArmyData enemyArmy)
    {
        BattleManager.i.SetBattleSpeed(0); //set the battle speed to paused to start
        
        //pick a region for the player to be able to place their units (TODO)

        BattleMapMessenger battleMapMessenger = FindAnyObjectByType<BattleMapMessenger>();
        if (battleMapMessenger == null)
        {
            Debug.LogError("BattleSetup requires a BattleMapMessenger.");
            return;
        }

        deploymentCounts.Clear();
        deploymentCounts.AddRange(battleMapMessenger.unitDeploymentCounts);

        deploymentPhaseUI = deploymentPhaseUI != null
            ? deploymentPhaseUI
            : FindAnyObjectByType<DeploymentPhaseUI>();
        deploymentPhaseUI?.Initialize(deploymentCounts);
        if (deploymentPhaseUI != null)
        {
            deploymentPhaseUI.deploymentEnded += EndDeployment;
            deploymentPhaseUI.UpdatePhaseText("Deployment Phase");
        }

        deploymentSpeech = speechStitcher.StitchSpeeches(battleMapMessenger.battleContext);
    }

    public void SelectUnitBase(UnitBase unitBase)
    {
        if (BattleManager.i.battleState != BattleState.Deployment)
        {
            return;
        }

        selectedUnitBase = deploymentCounts.Exists(item => item.unitBase == unitBase && item.count > 0)
            ? unitBase
            : null;
    }

    private void DeploySelectedUnit(OverlayTile tile)
    {
        if (selectedUnitBase == null || tile == null || tile.RestingObject != null)
        {
            return;
        }

        UnitDeploymentCount deploymentCount = deploymentCounts.Find(item => item.unitBase == selectedUnitBase);
        if (deploymentCount == null || !deploymentCount.TryDecrement())
        {
            selectedUnitBase = null;
            return;
        }

        if (fieldCharacterPrefab == null)
        {
            fieldCharacterPrefab = FindAnyObjectByType<FieldCharacter>();
            if (fieldCharacterPrefab == null)
            {
                Debug.LogError("BattleSetup requires a FieldCharacter prefab for deployment.");
                deploymentCount.count++;
                return;
            }
        }

        FieldCharacter character = Instantiate(fieldCharacterPrefab);
        character.PlayerControlled = true;
        character.SetupUnit(selectedUnitBase, tile.gridLocation);
        ClickDragCam cameraController = Camera.main?.GetComponent<ClickDragCam>();
        if (cameraController != null)
        {
            cameraController.PlaceCamera(character.transform.position);
        }
        deploymentPhaseUI?.TryDecrement(selectedUnitBase);
        selectedUnitBase = deploymentCount.count > 0 ? selectedUnitBase : null;
    }

    private void EndDeployment()
    {
        selectedUnitBase = null;
        BattleManager.i.EndDeployment();
        StartCoroutine(speechStitcher.ReadSpeech(deploymentSpeech));
    }
}