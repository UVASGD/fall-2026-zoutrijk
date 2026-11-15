using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The graphical frontend of a combat unit.
/// </summary>
public class FieldCharacter : MonoBehaviour, ObjectHP
{
    public static event Action<FieldCharacter> onFieldCharacterDied;

    #region Fields
    [SerializeField] bool isGeneral; //whether or not the fieldCharacter is a general
    [SerializeField] General generalName; //used if the general dies in battle
    [SerializeField] GameObject healthbarPrefab;
    private OverlayTile tilePosition;
    public OverlayTile TilePosition => tilePosition;
    private OverlayTile targetTile;
    public OverlayTile TargetTile => targetTile;
    [SerializeField] List<OverlayTile> movementOrders;
    private int movementRequestVersion;
    public string internalCharacterName { get; private set; }
    public string unitDisplayName { get; private set; } //determined from province of recruitment probably? like 2nd Dolebin Archers
    [SerializeField] CharacterSpriteHandler spriteHandler;
    public CharacterSpriteHandler SpriteHandler => spriteHandler;
    [SerializeField] UnitBase defaultBase;
    [SerializeField] UnitArmor unitArmor;
    [SerializeField] Weapon unitWeapon;
    private WorldspaceHealthbar healthBar;
    public UnitArmor UnitArmor => unitArmor;
    public Weapon UnitWeapon => unitWeapon;
    public int MaximumRangedRange => unit != null ? unit._base.MaximumRangedRange : 0;
    public Projectile RangedProjectile { get; private set; }
    public AudioClip MeleeAttackSfx { get; private set; }
    public AudioClip RangedAttackSfx { get; private set; }
    private Unit unit;
    public Unit Unit => unit;
    public int MaxUnitHP => unit.vitality;
    public int UnitHP { get; private set; }
    public bool IsDead => isDead || UnitHP <= 0;
    public bool PlayerControlled; //make private set later
    public BehaviorState CurrentBehavior { get; private set; } = BehaviorState.Idle;
    private FieldCharacter attackTarget;
    private Coroutine attackRoutine;
    private Coroutine idleAnimationRoutine;
    private bool wasWalking;
    private float reloadTimer;
    private bool isDead;
    private static bool victoryAnnounced;
    private static int enemiesKilled;
    private static int friendlyUnitsKilled;
    private const float rangedAimDelay = 0.5f;
    [SerializeField] private Vector2Int facingDirection = ObjectAnimation.DownRight;
    public Vector2Int FacingDirection => facingDirection;
    public Bounds CollisionBounds => new Bounds(transform.position, Vector3.one * collisionSquareSize);

    public static void ResetBattleAnnouncements()
    {
        victoryAnnounced = false;
        enemiesKilled = 0;
        friendlyUnitsKilled = 0;
    }
    private static Vector2 positionalOffset = new Vector2(0, 0.25f); //change to make it so character positions reflect properly on the isometric grid
    private const float collisionSquareSize = 0.5f;
    private static readonly HashSet<FieldCharacter> activeCharacters = new HashSet<FieldCharacter>();
    [SerializeField] private float idleTargetCheckInterval = 0.2f;
    [SerializeField] private int playerPathThreatRange = 10;
    [SerializeField] private int playerAutoAttackRange = 5;
    private const int deathRetargetRange = 10;
    private float idleTargetCheckTimer;
    #endregion
    #region Setup

    //TODO: change in place of selecting deployment area later in development
    [SerializeField] private Vector2Int defaultTilePosition = new Vector2Int(4, 4);

    private void OnEnable()
    {
        activeCharacters.Add(this);
    }

    private void OnDisable()
    {
        activeCharacters.Remove(this);
    }

    void Start()
    {
        if (unit == null)
        {
            SetupUnit(defaultBase, defaultTilePosition);
        }
    }
    public void SetupUnit(UnitBase uBase, Vector2 tilePosition)
    {
        if (idleAnimationRoutine != null)
        {
            StopCoroutine(idleAnimationRoutine);
            idleAnimationRoutine = null;
        }

        unit = new Unit(uBase);
        internalCharacterName = uBase.UnitName; //bubbled up 
        spriteHandler.Setup(uBase.CombatSprites);
        SetFacingDirection(uBase.CombatSprites.Idle.facingDirection);
        RangedProjectile = uBase.RangedProjectile;
        MeleeAttackSfx = uBase.CombatSprites.MeleeAttackSfx;
        RangedAttackSfx = uBase.CombatSprites.RangedAttackSfx;
        StartIdleAnimation();

        UnitHP = MaxUnitHP; //TODO: change to be carryover from campaign map
        isDead = false;

        SetTilePosition(tilePosition);

        healthBar = Instantiate(healthbarPrefab, BattleUI.i.gameObject.transform).GetComponent<WorldspaceHealthbar>(); //spawn healthbar as a child of the battleUI
    }

    private IEnumerator PlayIdleAnimationWhenIdle()
    {
        while (!isDead && spriteHandler != null && spriteHandler.HasIdleSprites)
        {
            if (CurrentBehavior == BehaviorState.Idle)
            {
                yield return spriteHandler.PlayIdleAnimationCycle();
            }
            else
            {
                yield return null;
            }
        }

        idleAnimationRoutine = null;
    }

    private void StartIdleAnimation()
    {
        if (idleAnimationRoutine == null && spriteHandler != null && spriteHandler.HasIdleSprites)
        {
            idleAnimationRoutine = StartCoroutine(PlayIdleAnimationWhenIdle());
        }
    }

    private void StopIdleAnimation()
    {
        if (idleAnimationRoutine != null)
        {
            StopCoroutine(idleAnimationRoutine);
            idleAnimationRoutine = null;
        }
    }
    #endregion
    #region Tile Movement
    public void SetTilePosition(Vector2 tilePosition)
    {
        // null check for map manager
        if (MapManager.i == null)
        {
            Debug.LogError("MapManager instance not found when setting up field character.");
            return;
        }

        //check to see if a tile exists at this position
        if (!MapManager.i.TryGetOverlayTile(tilePosition, out OverlayTile currentTile))
        {
            Debug.LogError($"No overlay tile found at tile position {tilePosition}.");
            return;
        }

        this.tilePosition = currentTile;
        targetTile = currentTile;

        if (currentTile != null)
            currentTile.SetRestingObject(this); //set the current tile as its resting object

        transform.position = (Vector2)currentTile.transform.position + positionalOffset;
    }
    /// <summary>
    /// Receives a list of overlay tiles for movement, and then calls FollowPath to lerp to each of those tiles.
    /// </summary>
    /// <param name="tiles"></param>
    public IEnumerator setMoveOrders(List<OverlayTile> tiles)
    {
        int requestVersion = SetMoveOrders(tiles);
        yield return FollowPath(requestVersion);
    }

    public int SetMoveOrders(List<OverlayTile> tiles)
    {
        movementOrders = tiles;
        return ++movementRequestVersion;
    }

    private void SetFacingDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        Vector2Int normalizedDirection = ObjectAnimation.NormalizeDirection(direction);
        if (CurrentBehavior == BehaviorState.Moving
            && normalizedDirection != facingDirection
            && GlobalEditorSettings.i.RichDebugLogs)
        {
            Debug.Log($"{internalCharacterName} is turning from {facingDirection} to {normalizedDirection} while moving at {transform.position}.");
        }

        facingDirection = normalizedDirection;
        spriteHandler?.SetFacingDirection(facingDirection);
    }

    private void SetFacingDirectionFromGrid(Vector2Int gridDirection)
    {
        if (gridDirection == Vector2Int.zero)
        {
            return;
        }

        // Grid +X/+Y project to screen UpRight/UpLeft respectively.
        Vector2Int animationDirection = new Vector2Int(
            gridDirection.x - gridDirection.y,
            gridDirection.x + gridDirection.y
        );
        SetFacingDirection(animationDirection);
    }

    private void FaceTile(OverlayTile tile)
    {
        if (tile != null && tilePosition != null)
        {
            SetFacingDirectionFromGrid(tile.gridLocation - tilePosition.gridLocation);
        }
    }

    public bool IsMovementRequestCurrent(int requestVersion)
    {
        return requestVersion == movementRequestVersion;
    }

    [SerializeField] private float baseMoveSpeed = 6f;
    [SerializeField] private float enemyAggroRange = 6f;
    [SerializeField] private AnimationCurve movementEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    private float formationMoveSpeedOverride;
    private float BaseMoveSpeed => unit != null ? baseMoveSpeed + (unit.agility * 0.5f) : baseMoveSpeed;
    public float CurrentMoveSpeed => formationMoveSpeedOverride > 0f ? formationMoveSpeedOverride : BaseMoveSpeed;

    public void SetFormationMoveSpeed(float speed)
    {
        formationMoveSpeedOverride = Mathf.Max(0f, speed);
    }

    public void ClearFormationMoveSpeed()
    {
        formationMoveSpeedOverride = 0f;
    }

    public float MoveSpeedForFormation(List<FieldCharacter> group)
    {
        float slowest = BaseMoveSpeed;

        if (group == null || group.Count == 0)
        {
            return slowest;
        }

        foreach (FieldCharacter member in group)
        {
            if (member == null)
            {
                continue;
            }

            float memberSpeed = member.BaseMoveSpeed;
            if (memberSpeed < slowest)
            {
                slowest = memberSpeed;
            }
        }

        return slowest;
    }

    public FieldCharacter AttackTarget => attackTarget;

    private void Update()
    {
        if (unit == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (healthBar.barPrompted)
        {
            healthBar.UpdateBarPosition(transform.position);
        }

        if (BattleManager.i != null && BattleManager.i.battleSpeed <= 0)
        {
            return;
        }

        if (CurrentBehavior == BehaviorState.Moving)
        {
            if (!wasWalking)
            {
                spriteHandler?.ResetWalkingAnimation();
                wasWalking = true;
            }

            spriteHandler?.UpdateWalkingAnimation(facingDirection, Time.deltaTime);
        }
        else if (wasWalking)
        {
            wasWalking = false;
            spriteHandler?.ResetWalkingAnimation();
        }

        if (PlayerControlled)
        {
            if (attackTarget == null && ShouldCheckForIdleTarget())
            {
                FieldCharacter attacker = CurrentBehavior == BehaviorState.Moving
                    ? FindNearestEnemyNearMovementPath()
                    : FindNearestAttackingEnemy();
                if (attacker != null)
                {
                    SetAttackTarget(attacker);
                }
            }

            return;
        }

        if (attackTarget != null)
        {
            if (attackTarget.UnitHP <= 0)
            {
                attackTarget = null;
                CurrentBehavior = BehaviorState.Idle;
                return;
            }

            int targetDistance = GetTileDistanceTo(attackTarget);
            if (targetDistance <= enemyAggroRange)
            {
                if (CurrentBehavior == BehaviorState.Idle)
                {
                    SetAttackTarget(attackTarget);
                }
                return;
            }

            if (targetDistance > enemyAggroRange * 2)
            {
                attackTarget = null;
                CurrentBehavior = BehaviorState.Idle;
            }

            return;
        }

        if (ShouldCheckForIdleTarget())
        {
            FieldCharacter nearestPlayerTarget = FindNearestPlayerTarget();
            if (nearestPlayerTarget != null && GetTileDistanceTo(nearestPlayerTarget) <= enemyAggroRange)
            {
                SetAttackTarget(nearestPlayerTarget);
            }
        }

    }

    private bool ShouldCheckForIdleTarget()
    {
        idleTargetCheckTimer -= Time.deltaTime;
        if (idleTargetCheckTimer > 0f)
        {
            return false;
        }

        idleTargetCheckTimer = Mathf.Max(0.01f, idleTargetCheckInterval);
        return true;
    }

    public int GetTileDistanceTo(FieldCharacter other)
    {
        if (other == null || TilePosition == null || other.TilePosition == null)
        {
            return int.MaxValue;
        }

        Vector2Int a = TilePosition.gridLocation;
        Vector2Int b = other.TilePosition.gridLocation;
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }

    private FieldCharacter FindNearestPlayerTarget()
    {
        FieldCharacter nearestTarget = null;
        int nearestDistance = int.MaxValue;

        foreach (FieldCharacter potentialTarget in activeCharacters)
        {
            if (potentialTarget == null || potentialTarget == this || !potentialTarget.PlayerControlled || potentialTarget.UnitHP <= 0)
            {
                continue;
            }

            int distance = GetTileDistanceTo(potentialTarget);
            if (distance <= enemyAggroRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = potentialTarget;
            }
        }

        return nearestTarget;
    }

    private FieldCharacter FindNearestAttackingEnemy()
    {
        FieldCharacter nearestAttacker = null;
        int nearestDistance = int.MaxValue;

        foreach (FieldCharacter potentialAttacker in activeCharacters)
        {
            if (potentialAttacker == null || potentialAttacker == this || potentialAttacker.PlayerControlled || potentialAttacker.UnitHP <= 0)
            {
                continue;
            }

            int distance = GetTileDistanceTo(potentialAttacker);
            bool isThreatening = potentialAttacker.AttackTarget == this
                || distance <= playerAutoAttackRange;
            if (!isThreatening)
            {
                continue;
            }

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestAttacker = potentialAttacker;
            }
        }

        return nearestAttacker;
    }

    private FieldCharacter FindNearestEnemyWithinRange(int range)
    {
        FieldCharacter nearestEnemy = null;
        int nearestDistance = int.MaxValue;

        foreach (FieldCharacter potentialEnemy in activeCharacters)
        {
            if (potentialEnemy == null
                || potentialEnemy == this
                || potentialEnemy.PlayerControlled == PlayerControlled
                || potentialEnemy.UnitHP <= 0
                || potentialEnemy.TilePosition == null)
            {
                continue;
            }

            int distance = GetTileDistanceTo(potentialEnemy);
            if (distance <= range && distance < nearestDistance)
            {
                nearestEnemy = potentialEnemy;
                nearestDistance = distance;
            }
        }

        return nearestEnemy;
    }

    private bool IsRangedUnit => unit != null && unit._base.UnitType == UnitType.Ranged;
    public bool IsRanged => IsRangedUnit;

    private int GetAttackRange()
    {
        return IsRangedUnit ? MaximumRangedRange : UnitWeapon.Range;
    }

    private FieldCharacter FindNearestEnemyNearMovementPath()
    {
        if (movementOrders == null || movementOrders.Count == 0)
        {
            return null;
        }

        FieldCharacter nearestEnemy = null;
        int nearestPathDistance = int.MaxValue;
        int nearestEnemyDistance = int.MaxValue;

        foreach (FieldCharacter potentialEnemy in activeCharacters)
        {
            if (potentialEnemy == null
                || potentialEnemy == this
                || potentialEnemy.PlayerControlled
                || potentialEnemy.UnitHP <= 0
                || potentialEnemy.TilePosition == null)
            {
                continue;
            }

            int enemyDistance = GetTileDistanceTo(potentialEnemy);
            for (int i = 0; i < movementOrders.Count; i++)
            {
                OverlayTile pathTile = movementOrders[i];
                if (pathTile == null)
                {
                    continue;
                }

                int pathDistance = Mathf.Max(
                    Mathf.Abs(pathTile.gridLocation.x - potentialEnemy.TilePosition.gridLocation.x),
                    Mathf.Abs(pathTile.gridLocation.y - potentialEnemy.TilePosition.gridLocation.y));
                if (pathDistance > playerPathThreatRange)
                {
                    continue;
                }

                if (pathDistance < nearestPathDistance
                    || (pathDistance == nearestPathDistance && enemyDistance < nearestEnemyDistance))
                {
                    nearestEnemy = potentialEnemy;
                    nearestPathDistance = pathDistance;
                    nearestEnemyDistance = enemyDistance;
                }
            }
        }

        return nearestEnemy;
    }

    private static readonly Dictionary<FieldCharacter, int> activeMeleeAttackCountByTarget = new Dictionary<FieldCharacter, int>();
    private static readonly Dictionary<FieldCharacter, HashSet<OverlayTile>> reservedAttackTilesByTarget = new Dictionary<FieldCharacter, HashSet<OverlayTile>>();
    private static readonly Dictionary<FieldCharacter, OverlayTile> reservedAttackTileByAttacker = new Dictionary<FieldCharacter, OverlayTile>();

    private int AcquireAttackSlot(FieldCharacter target)
    {
        if (target == null)
        {
            return 0;
        }

        int currentCount = activeMeleeAttackCountByTarget.TryGetValue(target, out int count) ? count : 0;
        activeMeleeAttackCountByTarget[target] = currentCount + 1;
        return currentCount;
    }

    private void ReleaseAttackSlot(FieldCharacter target)
    {
        if (target == null)
        {
            return;
        }

        if (!activeMeleeAttackCountByTarget.TryGetValue(target, out int count))
        {
            return;
        }

        if (count <= 1)
        {
            activeMeleeAttackCountByTarget.Remove(target);
            return;
        }

        activeMeleeAttackCountByTarget[target] = count - 1;
    }

    private void ReserveAttackTile(FieldCharacter target, OverlayTile tile)
    {
        if (target == null || tile == null)
        {
            return;
        }

        if (reservedAttackTileByAttacker.TryGetValue(this, out OverlayTile currentTile) && currentTile != null && currentTile != tile)
        {
            ReleaseAttackTile(attackTarget);
        }

        if (!reservedAttackTilesByTarget.TryGetValue(target, out HashSet<OverlayTile> tiles))
        {
            tiles = new HashSet<OverlayTile>();
            reservedAttackTilesByTarget[target] = tiles;
        }

        tiles.Add(tile);
        reservedAttackTileByAttacker[this] = tile;
    }

    private void ReleaseAttackTile(FieldCharacter target)
    {
        if (this == null)
        {
            return;
        }

        if (!reservedAttackTileByAttacker.TryGetValue(this, out OverlayTile tile) || tile == null)
        {
            return;
        }

        if (target != null && reservedAttackTilesByTarget.TryGetValue(target, out HashSet<OverlayTile> tiles))
        {
            tiles.Remove(tile);

            if (tiles.Count == 0)
            {
                reservedAttackTilesByTarget.Remove(target);
            }
        }

        reservedAttackTileByAttacker.Remove(this);
    }

    public bool IsTargetInWeaponRange(FieldCharacter target)
    {
        if (target == null || target.TilePosition == null || TilePosition == null || UnitWeapon == null)
        {
            return false;
        }

        Vector2Int attackerTile = TilePosition.gridLocation;
        Vector2Int targetTile = target.TilePosition.gridLocation;
        int distance = Mathf.Max(Mathf.Abs(attackerTile.x - targetTile.x), Mathf.Abs(attackerTile.y - targetTile.y));

        return distance <= GetAttackRange();
    }

    public bool TryFindNearestAttackTile(FieldCharacter target, out OverlayTile nearestTile)
    {
        nearestTile = null;

        if (target == null || target.TilePosition == null || TilePosition == null || UnitWeapon == null || MapManager.i == null || MapManager.i.map == null)
        {
            return false;
        }

        PathFinder pathFinder = new PathFinder();
        OverlayTile bestTile = null;
        int bestTargetDistance = int.MaxValue;
        int bestPathLength = int.MaxValue;

        Vector2Int targetLocation = target.TilePosition.gridLocation;
        int attackRange = GetAttackRange();
        int range = Mathf.Max(1, attackRange);
        for (int x = targetLocation.x - range; x <= targetLocation.x + range; x++)
        {
            for (int y = targetLocation.y - range; y <= targetLocation.y + range; y++)
            {
                if (!MapManager.i.TryGetOverlayTile(new Vector2Int(x, y), out OverlayTile tile)
                    || tile == null
                    || tile.isBlocked)
                {
                    continue;
                }

                if (tile == target.TilePosition)
                {
                    continue;
                }

                if (tile.RestingObject != null && !ReferenceEquals(tile.RestingObject, this) && !ReferenceEquals(tile.RestingObject, target))
                {
                    continue;
                }

                if (reservedAttackTilesByTarget.TryGetValue(target, out HashSet<OverlayTile> reservedTiles) && reservedTiles.Contains(tile) && (!reservedAttackTileByAttacker.TryGetValue(this, out OverlayTile currentTile) || currentTile != tile))
                {
                    continue;
                }

                int tileDistanceToTarget = Mathf.Max(Mathf.Abs(tile.gridLocation.x - targetLocation.x), Mathf.Abs(tile.gridLocation.y - targetLocation.y));
                if (tileDistanceToTarget > attackRange)
                {
                    continue;
                }

                if (attackRange <= 1 && tileDistanceToTarget != 1)
                {
                    continue;
                }

                List<OverlayTile> path = pathFinder.FindPath(TilePosition, tile);
                if (path == null || (path.Count == 0 && tile != TilePosition))
                {
                    continue;
                }

                if (tileDistanceToTarget < bestTargetDistance || (tileDistanceToTarget == bestTargetDistance && path.Count < bestPathLength))
                {
                    bestTile = tile;
                    bestTargetDistance = tileDistanceToTarget;
                    bestPathLength = path.Count;
                }
            }
        }

        nearestTile = bestTile;
        if (nearestTile != null)
        {
            ReserveAttackTile(target, nearestTile);
        }

        return nearestTile != null;
    }

    public void SetAttackTarget(FieldCharacter target)
    {
        if (target == attackTarget && attackRoutine != null)
        {
            return;
        }

        if (attackTarget != null && attackTarget != target)
        {
            ReleaseAttackTile(attackTarget);
        }

        if (target == null)
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            attackTarget = null;
            CurrentBehavior = BehaviorState.Idle;
            return;
        }

        if (CurrentBehavior == BehaviorState.Moving)
        {
            InterruptUnit();
        }

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }

        attackTarget = target;
        FaceTile(target.TilePosition);
        CurrentBehavior = BehaviorState.Moving;
        attackRoutine = StartCoroutine(MoveToNearestAttackTileAndAttack());
    }

    private IEnumerator MoveToNearestAttackTileAndAttack()
    {
        if (attackTarget == null || TilePosition == null)
        {
            attackRoutine = null;
            yield break;
        }

        while (attackTarget != null && attackTarget.gameObject.activeInHierarchy && attackTarget.UnitHP > 0)
        {
            if (GetTileDistanceTo(attackTarget) <= GetAttackRange())
            {
                CurrentBehavior = BehaviorState.MeleeAttacking;
                attackRoutine = StartCoroutine(AttackAfterDelay());
                yield break;
            }

            if (!TryFindNearestAttackTile(attackTarget, out OverlayTile liveAttackTile))
            {
                attackTarget = null;
                CurrentBehavior = BehaviorState.Idle;
                attackRoutine = null;
                yield break;
            }

            PathFinder pathFinder = new PathFinder();
            List<OverlayTile> path = pathFinder.FindPath(TilePosition, liveAttackTile);
            if (path != null && path.Count > 0)
            {
                CurrentBehavior = BehaviorState.Moving;
                int requestVersion = SetMoveOrders(path);
                yield return FollowPath(requestVersion);
            }

            if (attackTarget == null || !attackTarget.gameObject.activeInHierarchy || attackTarget.UnitHP <= 0)
            {
                attackRoutine = null;
                yield break;
            }

            if (GetTileDistanceTo(attackTarget) <= GetAttackRange())
            {
                CurrentBehavior = BehaviorState.MeleeAttacking;
                attackRoutine = StartCoroutine(AttackAfterDelay());
                yield break;
            }

            yield return null;
        }

        if (attackTarget == null || attackTarget.UnitHP <= 0)
        {
            CurrentBehavior = BehaviorState.Idle;
            attackRoutine = null;
            yield break;
        }

        CurrentBehavior = BehaviorState.Idle;
        attackRoutine = null;
    }

    private IEnumerator AttackAfterDelay()
    {
        while (unit != null && unit._base != null && attackTarget != null)
        {
            FieldCharacter target = attackTarget;
            if (GetTileDistanceTo(target) > GetAttackRange())
            {
                attackTarget = null;
                CurrentBehavior = BehaviorState.Idle;
                attackRoutine = null;
                yield break;
            }

            AttackType attackType = IsRangedUnit ? AttackType.Ranged : AttackType.Melee;
            int attackSlot = attackType == AttackType.Melee ? AcquireAttackSlot(target) : 0;
            float delay = attackType == AttackType.Ranged
                ? 0f
                : Mathf.Max(0f, unit._base.AttackDelay) + attackSlot * 0.08f;

            while (delay > 0f)
            {
                while (MouseController.i != null && MouseController.i.MovementPaused)
                {
                    yield return null;
                }

                float timeScale = BattleManager.i != null ? BattleManager.i.TimeScaleMultiplier : 1f;
                delay -= Time.deltaTime * Mathf.Max(1f, timeScale);
                yield return null;
            }

            if (attackTarget != target || target == null || target.UnitHP <= 0)
            {
                if (attackType == AttackType.Melee)
                {
                    ReleaseAttackSlot(target);
                }

                attackRoutine = null;
                yield break;
            }

            if (attackType == AttackType.Melee)
            {
                CurrentBehavior = BehaviorState.MeleeAttacking;
                FaceTile(target.TilePosition);
                if (spriteHandler != null && spriteHandler.HasMeleeAnimation)
                {
                    yield return StartCoroutine(spriteHandler.PlayMeleeAnimation());
                }
                else
                {
                    yield return StartCoroutine(BopTowardsTarget(target));
                }
            }
            else
            {
                CurrentBehavior = BehaviorState.RangeAttacking;
                FaceTile(target.TilePosition);

                float aimDelay = rangedAimDelay;
                while (aimDelay > 0f)
                {
                    while (MouseController.i != null && MouseController.i.MovementPaused)
                    {
                        yield return null;
                    }

                    float timeScale = BattleManager.i != null ? BattleManager.i.TimeScaleMultiplier : 1f;
                    aimDelay -= Time.deltaTime * Mathf.Max(1f, timeScale);
                    yield return null;
                }

                if (attackTarget != target || target == null || target.UnitHP <= 0)
                {
                    attackRoutine = null;
                    yield break;
                }

                if (spriteHandler != null && spriteHandler.HasRangedAnimation)
                {
                    yield return StartCoroutine(spriteHandler.PlayRangedAnimation());
                }
            }

            if (BattleManager.i != null)
            {
                BattleManager.i.PerformAttack(this, target, attackType);
            }

            if (attackType == AttackType.Ranged
                && (unit._base.ReloadTime > 0f || (spriteHandler != null && spriteHandler.HasReloadAnimation)))
            {
                yield return StartCoroutine(ReloadRangedWeapon());
            }

            if (attackType == AttackType.Melee)
            {
                ReleaseAttackSlot(target);
            }

            if (target.UnitHP <= 0 || !target.gameObject.activeInHierarchy)
            {
                ReleaseAttackTile(target);
                attackTarget = null;
                CurrentBehavior = BehaviorState.Idle;
                attackRoutine = null;
                yield break;
            }

            if (attackType != AttackType.Melee)
            {
                attackRoutine = null;
                CurrentBehavior = BehaviorState.Idle;
                yield break;
            }

            yield return null;
        }

        attackRoutine = null;
    }

    private IEnumerator ReloadRangedWeapon()
    {
        float reloadAnimationDuration = spriteHandler != null && spriteHandler.HasReloadAnimation
            ? spriteHandler.ReloadAnimationDuration
            : 0f;
        reloadTimer = Mathf.Max(0.01f, unit._base.ReloadTime, reloadAnimationDuration);
        Coroutine reloadAnimationRoutine = spriteHandler != null && spriteHandler.HasReloadAnimation
            ? StartCoroutine(spriteHandler.PlayReloadAnimation())
            : null;

        while (reloadTimer > 0f)
        {
            while (MouseController.i != null && MouseController.i.MovementPaused)
            {
                yield return null;
            }

            float timeScale = BattleManager.i != null ? BattleManager.i.TimeScaleMultiplier : 1f;
            reloadTimer -= Time.deltaTime * Mathf.Max(1f, timeScale);
            yield return null;
        }

        if (reloadAnimationRoutine != null)
        {
            StopCoroutine(reloadAnimationRoutine);
        }
    }

    /// <summary>
    /// Sample animation where a unit bops towards another to signify an attack
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private IEnumerator BopTowardsTarget(FieldCharacter target)
    {
        if (target == null)
        {
            yield break;
        }

        FaceTile(target.TilePosition);
        Vector3 startPosition = transform.position;
        Vector3 targetDirection = (target.transform.position - transform.position);
        if (targetDirection.sqrMagnitude < 0.001f)
        {
            yield break;
        }

        Vector3 strikePosition = startPosition + targetDirection.normalized * 0.18f;
        float duration = 0.12f;
        float elapsed = 0f;

        float timeScale = BattleManager.i != null ? BattleManager.i.TimeScaleMultiplier : 1f;
        while (elapsed < duration / 2f)
        {
            float t = Mathf.Clamp01(elapsed / (duration / 2f));
            transform.position = Vector3.Lerp(startPosition, strikePosition, t);
            elapsed += Time.deltaTime * Mathf.Max(1f, timeScale);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            float t = Mathf.Clamp01(elapsed / (duration / 2f));
            transform.position = Vector3.Lerp(strikePosition, startPosition, t);
            elapsed += Time.deltaTime * Mathf.Max(1f, timeScale);
            yield return null;
        }

        transform.position = startPosition;
    }

    private const int moveSpeedConstant = 10; // used to slow down movement to a more reasonable realtime speed.

    private Vector3 GetPositionOnPath(List<Vector3> points, float normalizedTime)
    {
        if (points == null || points.Count == 0)
        {
            return transform.position;
        }

        if (points.Count == 1)
        {
            return points[0];
        }

        float totalDistance = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            totalDistance += Vector3.Distance(points[i - 1], points[i]);
        }

        if (totalDistance <= 0f)
        {
            return points[points.Count - 1];
        }

        float targetDistance = Mathf.Clamp01(normalizedTime) * totalDistance;
        float travelled = 0f;

        for (int i = 1; i < points.Count; i++)
        {
            Vector3 start = points[i - 1];
            Vector3 end = points[i];
            float segmentDistance = Vector3.Distance(start, end);

            if (targetDistance <= travelled + segmentDistance)
            {
                float segmentTime = segmentDistance <= 0f ? 0f : (targetDistance - travelled) / segmentDistance;
                return Vector3.Lerp(start, end, segmentTime);
            }

            travelled += segmentDistance;
        }

        return points[points.Count - 1];
    }

    private void SetCurrentTile(OverlayTile tile)
    {
        if (tile == null)
        {
            return;
        }

        if (tilePosition != null && tilePosition != tile)
        {
            tilePosition.ClearRestingObject();
        }

        if (tile.RestingObject != null && !ReferenceEquals(tile.RestingObject, this))
        {
            return;
        }

        if (tilePosition != null)
        {
            Vector2Int movementDirection = tile.gridLocation - tilePosition.gridLocation;
            if (movementDirection != Vector2Int.zero)
            {
                SetFacingDirectionFromGrid(movementDirection);
            }
        }

        tilePosition = tile;
        tile.SetRestingObject(this);
    }

    private void UpdateMovementFacing(List<OverlayTile> path, int segmentIndex, OverlayTile sourceTile)
    {
        if (segmentIndex < 0 || segmentIndex >= path.Count || path[segmentIndex] == null)
        {
            return;
        }

        Vector2Int segmentStart = segmentIndex == 0 && sourceTile != null
            ? sourceTile.gridLocation
            : segmentIndex > 0 && path[segmentIndex - 1] != null
                ? path[segmentIndex - 1].gridLocation
                : path[segmentIndex].gridLocation;
        Vector2Int segmentDirection = path[segmentIndex].gridLocation - segmentStart;

        if (segmentDirection != Vector2Int.zero)
        {
            SetFacingDirectionFromGrid(segmentDirection);
        }
    }

    
    /// <summary>
    /// When attacked, a unit's current movement objective should be halted so they don't run through the lines
    /// </summary>
    public void InterruptUnit()
    {
        movementRequestVersion++;
        movementOrders?.Clear();
        wasWalking = false;
        spriteHandler?.ResetWalkingAnimation();
        StartIdleAnimation();

        if (tilePosition != null)
        {
            tilePosition.SetRestingObject(this);
            targetTile = tilePosition;
        }

        CurrentBehavior = BehaviorState.Idle;
    }

    public void LimboState()
    {
        this.CurrentBehavior = BehaviorState.Limbo;
    }

    
    /// <summary>
    /// Moves the character through a valid path without pausing at each tile. Occupancy is updated only when the next tile is reached.
    /// </summary>
    public IEnumerator FollowPath(int requestVersion)
    {
        if (!IsMovementRequestCurrent(requestVersion) || movementOrders == null || movementOrders.Count == 0)
        {
            yield break;
        }

        List<OverlayTile> activeMovementOrders = new List<OverlayTile>(movementOrders);
        if (!IsMovementRequestCurrent(requestVersion) || activeMovementOrders.Count == 0)
        {
            yield break;
        }

        CurrentBehavior = BehaviorState.Moving;
        OverlayTile sourceTile = tilePosition;
        UpdateMovementFacing(activeMovementOrders, 0, sourceTile);
        StopIdleAnimation();

        spriteHandler?.ResetWalkingAnimation();
        targetTile = activeMovementOrders[activeMovementOrders.Count - 1];

        if (sourceTile != null && sourceTile != targetTile)
        {
            sourceTile.ClearRestingObject();
        }

        List<Vector3> pathPoints = new List<Vector3> { transform.position };
        foreach (OverlayTile tile in activeMovementOrders)
        {
            if (tile != null)
            {
                pathPoints.Add(tile.transform.position + (Vector3)positionalOffset);
            }
        }

        if (pathPoints.Count <= 1)
        {
            if (sourceTile != null && sourceTile != targetTile)
            {
                sourceTile.SetRestingObject(this);
            }

            movementOrders.Clear();
            CurrentBehavior = BehaviorState.Idle;
            StartIdleAnimation();
            yield break;
        }

        float totalDistance = 0f;
        for (int i = 1; i < pathPoints.Count; i++)
        {
            totalDistance += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
        }

        float totalDuration = totalDistance <= 0f ? 0.01f : (totalDistance / CurrentMoveSpeed) * moveSpeedConstant;
        float elapsedTime = 0f;
        int pathIndex = -1;

        while (elapsedTime < totalDuration)
        {
            if (!IsMovementRequestCurrent(requestVersion))
            {
                StartIdleAnimation();
                yield break;
            }

            while (MouseController.i != null && MouseController.i.MovementPaused)
            {
                yield return null;
            }

            if (!IsMovementRequestCurrent(requestVersion))
            {
                StartIdleAnimation();
                yield break;
            }

            float normalizedTime = Mathf.Clamp01(elapsedTime / totalDuration);
            transform.position = GetPositionOnPath(pathPoints, normalizedTime);

            int nextPathIndex = Mathf.Clamp((int)(normalizedTime * activeMovementOrders.Count), 0, Mathf.Max(0, activeMovementOrders.Count - 1));
            UpdateMovementFacing(activeMovementOrders, nextPathIndex, sourceTile);
            if (nextPathIndex != pathIndex && nextPathIndex >= 0 && nextPathIndex < activeMovementOrders.Count)
            {
                SetCurrentTile(activeMovementOrders[nextPathIndex]);
                pathIndex = nextPathIndex;
            }

            float timeScale = BattleManager.i != null ? BattleManager.i.TimeScaleMultiplier : 1f;
            elapsedTime += Time.deltaTime * Mathf.Max(1f, timeScale);
            yield return null;
        }

        if (!IsMovementRequestCurrent(requestVersion))
        {
            StartIdleAnimation();
            yield break;
        }

        transform.position = pathPoints[pathPoints.Count - 1];

        if (sourceTile != null && sourceTile != targetTile)
        {
            sourceTile.ClearRestingObject();
        }

        SetCurrentTile(targetTile);

        movementOrders.Clear();
        CurrentBehavior = BehaviorState.Idle;
        StartIdleAnimation();
    }
    #endregion
    #region MapObject and HP implementation

    /// <summary>
    /// Function for taking damage for a FieldCharacter.
    /// </summary>
    /// <param name="baseDamage"></param>
    /// <param name="damageType"></param>
    /// <returns></returns>
    public IEnumerator TakeDamage(int baseDamage, DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Blunt:
                baseDamage -= unitArmor.padding;
                break;
            case DamageType.Slash:
                baseDamage -= unitArmor.mail;
                break;
            case DamageType.Pierce:
                baseDamage -= unitArmor.plating;
                break;
            default:
                break;
        }

        if (baseDamage < 0) return null; //no damage taken if below armor threshold

        //if UnitHP is currently maximum, prompt the healthbar. Otherwise just update the healthbar.
        if(UnitHP == MaxUnitHP) healthBar.PromptHealthbar();
        healthBar.UpdateBarFill((float)UnitHP / MaxUnitHP); //cast to prevent rounding

        UnitHP -= baseDamage;

        if (GlobalEditorSettings.i.RichDebugLogs) Debug.Log($"{internalCharacterName} took {baseDamage} damage, remaining HP: {UnitHP}");

        if (UnitHP <= 0)
        {
            UnitHP = 0;
            FieldUnitDeath();
        }
        else
        {
            spriteHandler?.StartHitFlash();
        }
        return null;
    }

    public bool AllowPassthrough(FieldCharacter passing)
    {
        return passing.PlayerControlled == this.PlayerControlled; //if both are playercontrolled of both are not.
    }

    public enum BehaviorState
    {
        Idle,
        Moving,
        MeleeAttacking,
        RangeAttacking,
        Limbo //when a unit is temporarily stuck, wait for a surrounding tile to be cleared to allow for safe movement
    }

    private void FieldUnitDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        if (PlayerControlled)
        {
            friendlyUnitsKilled++;
        }
        else
        {
            enemiesKilled++;
        }

        List<FieldCharacter> unitsTargetingThis = new List<FieldCharacter>();
        foreach (FieldCharacter character in activeCharacters)
        {
            if (character != null && character.attackTarget == this)
            {
                unitsTargetingThis.Add(character);
            }
        }
        onFieldCharacterDied?.Invoke(this);

        if (tilePosition != null)
        {
            tilePosition.ClearRestingObject();
            tilePosition.HideTile();
            tilePosition = null;
        }

        if (movementOrders != null)
        {
            movementOrders.Clear();
        }

        if (spriteHandler != null)
        {
            spriteHandler.StopHitFlash();
            StopIdleAnimation();
            if (spriteHandler.HasDyingAnimation)
            {
                StartCoroutine(PlayDyingAnimationAndDisable());
            }
            else
            {
                spriteHandler.gameObject.SetActive(false);
            }
        }

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        CurrentBehavior = BehaviorState.Idle;
        attackTarget = null;

        if(isGeneral) BattlefieldAnnouncer.i.BattlefieldAnnouncement($"General {generalName} has died");

        if (GlobalEditorSettings.i.RichDebugLogs) Debug.Log("Unit should die here");
        
        //deactivate health bar so it no longer shows onscreen
        healthBar.CancelHealthbar();

        if (!PlayerControlled && !victoryAnnounced && EnemyDeathCheck())
        {
            victoryAnnounced = true;
            BattlefieldAnnouncer.i?.BattlefieldAnnouncement(
                $"Victory! Enemies killed: {enemiesKilled}. Friendly units killed: {friendlyUnitsKilled}.");
        }

        foreach (FieldCharacter unitTargetingThis in unitsTargetingThis)
        {
            if (unitTargetingThis != null && unitTargetingThis.gameObject.activeInHierarchy)
            {
                unitTargetingThis.SetAttackTarget(
                    unitTargetingThis.FindNearestEnemyWithinRange(deathRetargetRange));
            }
        }

    }

    private IEnumerator PlayDyingAnimationAndDisable()
    {
        yield return spriteHandler.PlayDyingAnimation();
        if (spriteHandler != null)
        {
            spriteHandler.gameObject.SetActive(false);
        }
    }

    private bool EnemyDeathCheck()
    {
        foreach (FieldCharacter character in activeCharacters)
        {
            if (character != null
                && character != this
                && character.unit != null
                && !character.PlayerControlled
                && !character.IsDead)
            {
                return false;
            }
        }

        return true;
    }

    public WorldObjectPreviewData ExposeObjectInfo()
    {
        return new WorldObjectPreviewData(unit._base.UnitName, unit._base.PortraitSprite, unit._base.UnitDescription, (float)UnitHP / (float)MaxUnitHP, (int)MaxUnitHP);
    }
    #endregion
}