using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class FieldCharacter
{
    private bool IsRangedUnit => unit != null && unit._base.UnitType == UnitType.Ranged;
    public bool IsRanged => IsRangedUnit;

    private int GetAttackRange()
    {
        return IsRangedUnit ? MaximumRangedRange : UnitWeapon.Range;
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
                    || tile.isBlocked
                    || tile == target.TilePosition)
                {
                    continue;
                }

                if (tile.RestingObject != null && !ReferenceEquals(tile.RestingObject, this) && !ReferenceEquals(tile.RestingObject, target))
                {
                    continue;
                }

                if (reservedAttackTilesByTarget.TryGetValue(target, out HashSet<OverlayTile> reservedTiles)
                    && reservedTiles.Contains(tile)
                    && (!reservedAttackTileByAttacker.TryGetValue(this, out OverlayTile currentTile) || currentTile != tile))
                {
                    continue;
                }

                int tileDistanceToTarget = Mathf.Max(
                    Mathf.Abs(tile.gridLocation.x - targetLocation.x),
                    Mathf.Abs(tile.gridLocation.y - targetLocation.y));
                if (tileDistanceToTarget > attackRange || (attackRange <= 1 && tileDistanceToTarget != 1))
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

            List<OverlayTile> path = new PathFinder().FindPath(TilePosition, liveAttackTile);
            if (path != null && path.Count > 0)
            {
                CurrentBehavior = BehaviorState.Moving;
                yield return FollowPath(SetMoveOrders(path));
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

    private IEnumerator BopTowardsTarget(FieldCharacter target)
    {
        if (target == null)
        {
            yield break;
        }

        FaceTile(target.TilePosition);
        Vector3 startPosition = transform.position;
        Vector3 targetDirection = target.transform.position - transform.position;
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
}
