using System.Collections.Generic;
using UnityEngine;

public partial class FieldCharacter
{
    private static readonly Dictionary<FieldCharacter, int> activeMeleeAttackCountByTarget = new Dictionary<FieldCharacter, int>();
    private static readonly Dictionary<FieldCharacter, HashSet<OverlayTile>> reservedAttackTilesByTarget = new Dictionary<FieldCharacter, HashSet<OverlayTile>>();
    private static readonly Dictionary<FieldCharacter, OverlayTile> reservedAttackTileByAttacker = new Dictionary<FieldCharacter, OverlayTile>();

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
}
