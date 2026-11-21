using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class FieldCharacter
{
    private const int moveSpeedConstant = 10;

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
        CurrentBehavior = BehaviorState.Limbo;
    }

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
}
