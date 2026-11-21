using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The graphical frontend of a combat unit.
/// </summary>
public partial class FieldCharacter : MonoBehaviour, ObjectHP
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