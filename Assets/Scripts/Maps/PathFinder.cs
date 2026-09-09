using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinder
{
    //return a list of tiles a path must go through to reach a set target.
    public List<OverlayTile> FindPath(OverlayTile source, OverlayTile target)
    {
        return FindPath(source, target, null);
    }

    public List<OverlayTile> FindPath(OverlayTile source, OverlayTile target, HashSet<OverlayTile> ignoredTiles)
    {
        if (source == null || target == null)
        {
            return new List<OverlayTile>();
        }

        ResetSearchState();

        List<OverlayTile> openList = new List<OverlayTile>(); //tiles we want to check in the next loop iteration
        List<OverlayTile> closedList = new List<OverlayTile>(); //tiles we do not need to recheck
        
        openList.Add(source);

        while(openList.Count > 0)
        {
            OverlayTile currentOverlayTile = openList.OrderBy(x => x.F).First();

            openList.Remove(currentOverlayTile);
            closedList.Add(currentOverlayTile);

            if(currentOverlayTile == target)
            {
                //found target tile, finalize path
                return GetFinishedList(source, target);
            }

            var neighborTiles = GetNeighborTiles(currentOverlayTile);

            foreach(var neighbor in neighborTiles)
            {
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

                if (neighbor.previous == null || newG < neighbor.G)
                {
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

    private void ResetSearchState()
    {
        if (MapManager.i == null || MapManager.i.map == null)
        {
            return;
        }

        foreach (OverlayTile tile in MapManager.i.map.Values)
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

    private List<OverlayTile> GetNeighborTiles(OverlayTile sourceTile)
    {
        var map = MapManager.i != null ? MapManager.i.map : null;
        if (map == null)
        {
            return new List<OverlayTile>();
        }

        List<OverlayTile> neighbors = new List<OverlayTile>();

        Vector2Int[] directions = new[]
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

        foreach (Vector2Int direction in directions)
        {
            Vector2Int locationToCheck = sourceTile.gridLocation + direction;

            if (map.ContainsKey(locationToCheck))
            {
                neighbors.Add(map[locationToCheck]);
            }
        }

        return neighbors;
    }
}
