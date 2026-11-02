using System;
using System.Collections.Generic;
using UnityEngine;

public class PathFinder
{
    private static readonly Vector2Int[] directions =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(-1, 1)
    };

    //return a list of tiles a path must go through to reach a set target.
    public List<OverlayTile> FindPath(OverlayTile source, OverlayTile target)
    {
        return FindPath(source, target, null);
    }

    public List<OverlayTile> FindPath(OverlayTile source, OverlayTile target, HashSet<OverlayTile> ignoredTiles)
    {
        if (source == null || target == null || MapManager.i == null || MapManager.i.map == null)
        {
            return new List<OverlayTile>();
        }

        List<OverlayTile> openList = new List<OverlayTile>(); //tiles we want to check in the next loop iteration
        HashSet<OverlayTile> closedList = new HashSet<OverlayTile>(); //tiles we do not need to recheck
        HashSet<OverlayTile> discoveredTiles = new HashSet<OverlayTile>();
        List<OverlayTile> searchedTiles = new List<OverlayTile>();
        
        openList.Add(source);
        discoveredTiles.Add(source);
        searchedTiles.Add(source);

        while(openList.Count > 0)
        {
            OverlayTile currentOverlayTile = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].F < currentOverlayTile.F)
                {
                    currentOverlayTile = openList[i];
                }
            }

            openList.Remove(currentOverlayTile);
            closedList.Add(currentOverlayTile);

            if(currentOverlayTile == target)
            {
                //found target tile, finalize path
                List<OverlayTile> finishedPath = GetFinishedList(source, target);
                ResetSearchState(searchedTiles);
                return finishedPath;
            }

            foreach (Vector2Int direction in directions)
            {
                if (!MapManager.i.map.TryGetValue(currentOverlayTile.gridLocation + direction, out OverlayTile neighbor))
                {
                    continue;
                }

                if (closedList.Contains(neighbor))
                {
                    continue;
                }

                bool isIgnoredTile = ignoredTiles != null && ignoredTiles.Contains(neighbor);
                if (!isIgnoredTile && IsTileBlocked(neighbor, target))
                {
                    continue;
                }

                int movementCost = GetMovementCost(currentOverlayTile, neighbor);
                int newG = currentOverlayTile.G + movementCost;

                if (!discoveredTiles.Contains(neighbor) || newG < neighbor.G)
                {
                    if (discoveredTiles.Add(neighbor))
                    {
                        searchedTiles.Add(neighbor);
                    }

                    neighbor.G = newG;
                    neighbor.H = GetDistance(target, neighbor);
                    neighbor.previous = currentOverlayTile;

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }

        ResetSearchState(searchedTiles);
        return new List<OverlayTile>();
    }

    private bool IsTileBlocked(OverlayTile tile, OverlayTile target)
    {
        if (tile == null)
        {
            return true;
        }

        if (tile.isBlocked)
        {
            return true;
        }

        if (tile.RestingObject == null)
        {
            return false;
        }

        if (tile == target)
        {
            return true;
        }

        if (tile.RestingObject is FieldCharacter movingCharacter)
        {
            return movingCharacter.CurrentBehavior != FieldCharacter.BehaviorState.Moving;
        }

        return true;
    }

    private List<OverlayTile> GetFinishedList(OverlayTile source, OverlayTile target)
    {
        List<OverlayTile> finishedList = new List<OverlayTile>();

        OverlayTile current = target;

        while(current != source)
        {
            finishedList.Add(current);
            current = current.previous; //traverse the nodes
        }

        finishedList.Reverse();
        return finishedList;
    }

    private void ResetSearchState(List<OverlayTile> searchedTiles)
    {
        if (searchedTiles == null)
        {
            return;
        }

        foreach (OverlayTile tile in searchedTiles)
        {
            if (tile == null)
            {
                continue;
            }

            tile.G = 0;
            tile.H = 0;
            tile.previous = null;
        }
    }

    private int GetDistance(OverlayTile source, OverlayTile neighbor)
    {
        int dx = Mathf.Abs(source.gridLocation.x - neighbor.gridLocation.x);
        int dy = Mathf.Abs(source.gridLocation.y - neighbor.gridLocation.y);

        return (14 * Mathf.Min(dx, dy) + 10 * Mathf.Abs(dx - dy));
    }

    private int GetMovementCost(OverlayTile source, OverlayTile neighbor)
    {
        int dx = Mathf.Abs(source.gridLocation.x - neighbor.gridLocation.x);
        int dy = Mathf.Abs(source.gridLocation.y - neighbor.gridLocation.y);

        return dx == 1 && dy == 1 ? 14 : 10;
    }

}
