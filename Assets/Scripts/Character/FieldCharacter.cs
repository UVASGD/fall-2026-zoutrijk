using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The graphical frontend of a combat unit.
/// </summary>
public class FieldCharacter : MonoBehaviour, ObjectHP
{
    #region Fields
    private OverlayTile tilePosition;
    public OverlayTile TilePosition => tilePosition;
    private OverlayTile targetTile;
    public OverlayTile TargetTile => targetTile;
    [SerializeField] List<OverlayTile> movementOrders;
    public string internalCharacterName { get; private set; }
    public string unitDisplayName { get; private set; } //determined from province of recruitment probably? like 2nd Dolebin Archers
    [SerializeField] CharacterSpriteHandler spriteHandler;
    public CharacterSpriteHandler SpriteHandler => spriteHandler;
    [SerializeField] UnitBase defaultBase;
    [SerializeField] UnitArmor unitArmor;
    [SerializeField] Weapon unitWeapon;
    public UnitArmor UnitArmor => unitArmor;
    public Weapon UnitWeapon => unitWeapon;
    private Unit unit;
    public Unit Unit => unit;
    public int MaxUnitHP => unit.vitality;
    public int UnitHP { get; private set; }
    public bool PlayerControlled; //make private set later
    public BehaviorState CurrentBehavior { get; private set; } = BehaviorState.Idle;
    private FieldCharacter attackTarget;
    private Coroutine attackRoutine;
    private static Vector2 positionalOffset = new Vector2(0, 0.25f); //change to make it so character positions reflect properly on the isometric grid
    #endregion
    #region Setup

    //TODO: change in place of selecting deployment area later in development
    [SerializeField] private Vector2Int defaultTilePosition = new Vector2Int(4, 4);
    void Start()
    {
        SetupUnit(defaultBase, defaultTilePosition);
    }
    public void SetupUnit(UnitBase uBase, Vector2 tilePosition)
    {
        unit = new Unit(uBase);
        internalCharacterName = uBase.UnitName; //bubbled up 
        spriteHandler.Setup(uBase.CombatSprites);

        UnitHP = MaxUnitHP; //TODO: change to be carryover from campaign map

        SetTilePosition(tilePosition);
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
        movementOrders = tiles;
        yield return FollowPath();
    }

    [SerializeField] private float baseMoveSpeed = 6f;
    [SerializeField] private float enemyAggroRange = 6f;
    [SerializeField] private AnimationCurve movementEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    private float formationMoveSpeedOverride;
    private float BaseMoveSpeed => unit != null && unit._base != null ? baseMoveSpeed + (unit._base.Agility * 0.5f) : baseMoveSpeed;
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

        if (PlayerControlled)
        {
            if (CurrentBehavior == BehaviorState.Idle && attackTarget == null)
            {
                FieldCharacter attacker = FindNearestAttackingEnemy();
                if (attacker != null && GetTileDistanceTo(attacker) <= UnitWeapon.Range)
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

        FieldCharacter nearestPlayerTarget = FindNearestPlayerTarget();
        if (nearestPlayerTarget != null && GetTileDistanceTo(nearestPlayerTarget) <= enemyAggroRange)
        {
            SetAttackTarget(nearestPlayerTarget);
        }
    }

    private int GetTileDistanceTo(FieldCharacter other)
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

        FieldCharacter[] allUnits = FindObjectsByType<FieldCharacter>();
        foreach (FieldCharacter potentialTarget in allUnits)
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

        FieldCharacter[] allUnits = FindObjectsByType<FieldCharacter>();
        foreach (FieldCharacter potentialAttacker in allUnits)
        {
            if (potentialAttacker == null || potentialAttacker == this || potentialAttacker.PlayerControlled || potentialAttacker.UnitHP <= 0)
            {
                continue;
            }

            int distance = GetTileDistanceTo(potentialAttacker);
            bool isThreatening = potentialAttacker.AttackTarget == this || distance <= 1;
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

        return distance <= UnitWeapon.Range;
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

        foreach (OverlayTile tile in MapManager.i.map.Values)
        {
            if (tile == null || tile.isBlocked)
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

            int tileDistanceToTarget = Mathf.Max(Mathf.Abs(tile.gridLocation.x - target.TilePosition.gridLocation.x), Mathf.Abs(tile.gridLocation.y - target.TilePosition.gridLocation.y));
            if (tileDistanceToTarget > UnitWeapon.Range)
            {
                continue;
            }

            if (UnitWeapon.Range <= 1 && tileDistanceToTarget != 1)
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

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }

        attackTarget = target;
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
            if (GetTileDistanceTo(attackTarget) <= UnitWeapon.Range)
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
                yield return setMoveOrders(path);
            }

            if (attackTarget == null || !attackTarget.gameObject.activeInHierarchy || attackTarget.UnitHP <= 0)
            {
                attackRoutine = null;
                yield break;
            }

            if (GetTileDistanceTo(attackTarget) <= UnitWeapon.Range)
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
        if (unit == null || unit._base == null || attackTarget == null)
        {
            attackRoutine = null;
            yield break;
        }

        if (GetTileDistanceTo(attackTarget) > UnitWeapon.Range)
        {
            attackTarget = null;
            CurrentBehavior = BehaviorState.Idle;
            attackRoutine = null;
            yield break;
        }

        AttackType attackType = UnitWeapon != null && (UnitWeapon.Type == WeaponType.Bow || UnitWeapon.Type == WeaponType.Crossbow) ? AttackType.Ranged : AttackType.Melee;
        int attackSlot = 0;

        if (attackType == AttackType.Melee)
        {
            attackSlot = AcquireAttackSlot(attackTarget);
        }

        float delay = Mathf.Max(0f, unit._base.AttackDelay) + (attackType == AttackType.Melee ? attackSlot * 0.08f : 0f);

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

        if (attackTarget == null)
        {
            if (attackType == AttackType.Melee)
            {
                ReleaseAttackSlot(attackTarget);
            }

            attackRoutine = null;
            yield break;
        }

        if (attackType == AttackType.Melee)
        {
            CurrentBehavior = BehaviorState.MeleeAttacking;
            yield return StartCoroutine(BopTowardsTarget(attackTarget));
        }
        else
        {
            CurrentBehavior = BehaviorState.RangeAttacking;
        }

        if (BattleManager.i != null)
        {
            BattleManager.i.PerformAttack(this, attackTarget, attackType);
        }

        if (attackType == AttackType.Melee)
        {
            ReleaseAttackSlot(attackTarget);
        }

        if (attackTarget == null || attackTarget.UnitHP <= 0)
        {
            ReleaseAttackTile(attackTarget);
            CurrentBehavior = BehaviorState.Idle;
            attackRoutine = null;
            yield break;
        }

        if (attackType == AttackType.Melee)
        {
            attackRoutine = StartCoroutine(AttackAfterDelay());
        }
        else
        {
            attackRoutine = null;
            CurrentBehavior = BehaviorState.Idle;
        }
    }

    private IEnumerator BopTowardsTarget(FieldCharacter target)
    {
        if (target == null)
        {
            yield break;
        }

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

        tilePosition = tile;
        tile.SetRestingObject(this);
    }

    /// <summary>
    /// Moves the character through a valid path without pausing at each tile. Occupancy is updated only when the next tile is reached.
    /// </summary>
    private IEnumerator FollowPath()
    {
        if (movementOrders == null || movementOrders.Count == 0)
        {
            yield break;
        }

        List<OverlayTile> activeMovementOrders = new List<OverlayTile>(movementOrders);
        if (activeMovementOrders.Count == 0)
        {
            yield break;
        }

        CurrentBehavior = BehaviorState.Moving;
        targetTile = activeMovementOrders[activeMovementOrders.Count - 1];

        OverlayTile sourceTile = tilePosition;
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
            while (MouseController.i != null && MouseController.i.MovementPaused)
            {
                yield return null;
            }

            float normalizedTime = Mathf.Clamp01(elapsedTime / totalDuration);
            transform.position = GetPositionOnPath(pathPoints, normalizedTime);

            int nextPathIndex = Mathf.Clamp((int)(normalizedTime * activeMovementOrders.Count), 0, Mathf.Max(0, activeMovementOrders.Count - 1));
            if (nextPathIndex != pathIndex && nextPathIndex >= 0 && nextPathIndex < activeMovementOrders.Count)
            {
                SetCurrentTile(activeMovementOrders[nextPathIndex]);
                pathIndex = nextPathIndex;
            }

            float timeScale = BattleManager.i != null ? BattleManager.i.TimeScaleMultiplier : 1f;
            elapsedTime += Time.deltaTime * Mathf.Max(1f, timeScale);
            yield return null;
        }

        transform.position = pathPoints[pathPoints.Count - 1];

        if (sourceTile != null && sourceTile != targetTile)
        {
            sourceTile.ClearRestingObject();
        }

        SetCurrentTile(targetTile);

        movementOrders.Clear();
        CurrentBehavior = BehaviorState.Idle;
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

        UnitHP -= baseDamage;

        if (GlobalEditorSettings.i.RichDebugLogs) Debug.Log($"{internalCharacterName} took {baseDamage} damage, remaining HP: {UnitHP}");

        if (UnitHP <= 0)
        {
            UnitHP = 0;
            FieldUnitDeath();
        }
        else
        {
            StartCoroutine(spriteHandler?.HitFlash()); //play hitflash animation
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
        RangeAttacking
    }

    private void FieldUnitDeath()
    {
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
            spriteHandler.gameObject.SetActive(false);
        }

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        CurrentBehavior = BehaviorState.Idle;
        attackTarget = null;

        if (GlobalEditorSettings.i.RichDebugLogs) Debug.Log("Unit should die here");
    }

    public WorldObjectPreviewData ExposeObjectInfo()
    {
        return new WorldObjectPreviewData(unit._base.UnitName, unit._base.PortraitSprite, unit._base.UnitDescription, (float)UnitHP / (float)MaxUnitHP, (int)MaxUnitHP);
    }
    #endregion
}