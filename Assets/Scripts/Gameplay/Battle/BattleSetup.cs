using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private int enemyDeploymentDistance = 50;
    private UnitBase selectedUnitBase;
    private FieldCharacter lastDeployedCharacter;
    private readonly List<UnitDeploymentCount> deploymentCounts = new List<UnitDeploymentCount>();
    private List<string> deploymentSpeech;
    private BattleArmyData enemyArmy;
    public int SelectedUnitCount => selectedUnitBase == null
        ? 0
        : deploymentCounts.Find(item => item.unitBase == selectedUnitBase)?.count ?? 0;

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
        FieldCharacter.ResetBattleAnnouncements();
        BattleManager.i.SetBattleSpeed(0); //set the battle speed to paused to start
        this.enemyArmy = enemyArmy;
        
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
        CombatMusicManager.i.PlayDeploymentTheme();
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
        lastDeployedCharacter = character;
        deploymentPhaseUI?.TryDecrement(selectedUnitBase);
        selectedUnitBase = deploymentCount.count > 0 ? selectedUnitBase : null;
    }

    private void EndDeployment()
    {
        selectedUnitBase = null;
        DeployEnemyArmy();
        if (lastDeployedCharacter != null)
        {
            ClickDragCam cameraController = Camera.main?.GetComponent<ClickDragCam>();
            cameraController?.PlaceCamera(lastDeployedCharacter.transform.position);
        }

        lastDeployedCharacter = null;
        BattleManager.i.EndDeployment();
        StartCoroutine(speechStitcher.ReadSpeech(deploymentSpeech));
    }

    private void DeployEnemyArmy()
    {
        if (enemyArmy == null || enemyArmy.Units == null || enemyArmy.Units.Count == 0 || MapManager.i == null)
        {
            return;
        }

        List<FieldCharacter> playerCharacters = FindObjectsByType<FieldCharacter>(FindObjectsSortMode.None)
            .Where(character => character != null && character.PlayerControlled && character.TilePosition != null)
            .ToList();
        if (playerCharacters.Count == 0)
        {
            Debug.LogError("BattleSetup could not deploy the enemy army because no player units were deployed.");
            return;
        }

        Vector2Int playerCenter = Vector2Int.zero;
        foreach (FieldCharacter character in playerCharacters)
        {
            playerCenter += character.TilePosition.gridLocation;
        }

        playerCenter /= playerCharacters.Count;
        Vector2Int mapCenter = Vector2Int.zero;
        if (MapManager.i.map != null && MapManager.i.map.Count > 0)
        {
            foreach (Vector2Int location in MapManager.i.map.Keys)
            {
                mapCenter += location;
            }

            mapCenter /= MapManager.i.map.Count;
        }

        Vector2Int enemyDirection = mapCenter - playerCenter;
        if (enemyDirection == Vector2Int.zero)
        {
            enemyDirection = Vector2Int.up;
        }

        Vector2Int cardinalEnemyDirection = GetCardinalDirection(enemyDirection);
        Vector2Int enemyAnchor = playerCenter + cardinalEnemyDirection * Mathf.Max(1, enemyDeploymentDistance);
        HashSet<OverlayTile> occupiedTiles = new HashSet<OverlayTile>(
            playerCharacters.Select(character => character.TilePosition));
        Dictionary<UnitBase, int> enemyUnitCounts = new Dictionary<UnitBase, int>();
        foreach (CampaignUnit campaignUnit in enemyArmy.Units)
        {
            if (campaignUnit == null || campaignUnit.UnitBase == null)
            {
                continue;
            }

            int unitHealth = Mathf.Max(0, Mathf.RoundToInt(campaignUnit.UnitHealth));
            if (unitHealth == 0)
            {
                continue;
            }

            if (enemyUnitCounts.ContainsKey(campaignUnit.UnitBase))
            {
                enemyUnitCounts[campaignUnit.UnitBase] += unitHealth;
            }
            else
            {
                enemyUnitCounts.Add(campaignUnit.UnitBase, unitHealth);
            }
        }

        int totalEnemyUnits = enemyUnitCounts.Values.Sum();
        int width = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(totalEnemyUnits)));
        int index = 0;

        foreach (KeyValuePair<UnitBase, int> unitGroup in enemyUnitCounts)
        {
            for (int unitIndex = 0; unitIndex < unitGroup.Value; unitIndex++)
            {
                Vector2Int location = enemyAnchor
                    + cardinalEnemyDirection * (index / width)
                    + GetPerpendicularDirection(cardinalEnemyDirection) * (index % width);
                if (!TryFindAvailableEnemyTile(location, occupiedTiles, out OverlayTile tile))
                {
                    Debug.LogError($"BattleSetup could not find a tile for enemy unit {unitGroup.Key.UnitName}.");
                    continue;
                }

                FieldCharacter character = Instantiate(fieldCharacterPrefab);
                character.PlayerControlled = false;
                character.SetupUnit(unitGroup.Key, tile.gridLocation);
                occupiedTiles.Add(tile);
                index++;
            }
        }
    }

    private bool TryFindAvailableEnemyTile(
        Vector2Int location,
        HashSet<OverlayTile> occupiedTiles,
        out OverlayTile tile)
    {
        tile = null;
        for (int radius = 0; radius <= enemyDeploymentDistance; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius
                        || !MapManager.i.TryGetOverlayTile(location + new Vector2Int(x, y), out OverlayTile candidate)
                        || candidate == null
                        || candidate.isBlocked
                        || candidate.RestingObject != null
                        || occupiedTiles.Contains(candidate))
                    {
                        continue;
                    }

                    tile = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private Vector2Int GetCardinalDirection(Vector2Int direction)
    {
        return Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
            ? new Vector2Int((int)Mathf.Sign(direction.x), 0)
            : new Vector2Int(0, (int)Mathf.Sign(direction.y));
    }

    private Vector2Int GetPerpendicularDirection(Vector2Int direction)
    {
        return new Vector2Int(-direction.y, direction.x);
    }
}