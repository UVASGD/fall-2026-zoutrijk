using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    public static MapManager i;

    [SerializeField] public Dictionary<Vector2Int, OverlayTile> map;
    public OverlayTile overlayTilePrefab;
    public GameObject overlayParent;

    [SerializeField] Tilemap walkableMap;

    [SerializeField] GameObject pathArrowHead;
    [SerializeField] Transform arrowParent;
    [SerializeField] Color pathLineColor = new Color(0.2f, 0.85f, 1f, 1f);
    [SerializeField] float pathLineWidth = 0.2f;
    [SerializeField] string pathLineSortingLayer = "Default";

    private readonly List<OverlayTile> losIndicatorTiles = new List<OverlayTile>();

    void Awake()
    {
        if (i == null) i = this;
    }

    void Start()
    {
        BoundsInt bounds = walkableMap.cellBounds;
        Debug.Log("Looping through all tiles");

        map = new Dictionary<Vector2Int, OverlayTile>();

        //loop through all tiles
        for (int z = bounds.min.z; z < bounds.max.z; z++)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                for (int x = bounds.min.x; x < bounds.max.x; x++)
                {
                    var tileLocation = new Vector3Int(x, y, z); //store the location of that tile
                    var tileKey = new Vector2Int(x,y);
                    if (walkableMap.HasTile(tileLocation) && !map.ContainsKey(tileKey))
                    {
                        var overlayTile = Instantiate(overlayTilePrefab, overlayParent.transform);
                        var cellWorldPosition = walkableMap.layoutGrid.GetCellCenterWorld(tileLocation);

                        overlayTile.transform.position = new Vector3(cellWorldPosition.x, cellWorldPosition.y - 0.125f, cellWorldPosition.z);
                        overlayTile.name = $"{x},{y}"; //for ease of viewing in the inspector
                        overlayTile.GetComponent<SpriteRenderer>().sortingLayerName = "Floor Highlight";
                        overlayTile.gridLocation = new Vector2Int(x,y); //insert the X and Y location

                        map.Add(tileKey, overlayTile);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Destroys the rendered line segments for a pathfinding line renderer
    /// </summary>
    public void DestroyPathfindingArrows()
    {
        if (arrowParent == null)
        {
            return;
        }

        DestroyAllChildrenOfGameobject(arrowParent.gameObject);
    }

    /// <summary>
    /// Given a list of overlay tiles, computes corner points and draws a dynamic path line.
    /// </summary>
    /// <param name="tiles"></param>
    public void DrawPathfindingArrows(List<OverlayTile> tiles)
    {
        if (arrowParent == null)
        {
            return;
        }

        DestroyPathfindingArrows();
    }
    public bool TryGetOverlayTile(Vector2 tilePosition, out OverlayTile overlayTile)
    {
        overlayTile = null;

        if (map == null)
        {
            return false;
        }

        Vector2Int tileKey = new Vector2Int(Mathf.RoundToInt(tilePosition.x), Mathf.RoundToInt(tilePosition.y));
        return map.TryGetValue(tileKey, out overlayTile);
    }
    static void DestroyAllChildrenOfGameobject(GameObject go)
    {
        for(int i = go.transform.childCount - 1; i >=0; i--)
        {
            Destroy(go.transform.GetChild(i).gameObject);
        }
    }
    public bool CheckLineOfSight(OverlayTile t1, OverlayTile t2)
    {
        //checks the line of sight between two tiles, drawing a straight line and seeing if any restingObjects are in the way
        List<OverlayTile> lineTiles = BuildLineOfSightTiles(t1, t2);

        if (lineTiles.Count == 0)
        {
            return false;
        }

        return lineTiles.Count > 0 && lineTiles[lineTiles.Count - 1] == t2;
    }

    public void UpdateLOSIndicator(OverlayTile sourceTile, OverlayTile targetTile)
    {
        ClearLOSIndicator();

        List<OverlayTile> lineTiles = BuildLineOfSightTiles(sourceTile, targetTile);
        foreach (OverlayTile tile in lineTiles)
        {
            if (tile == null)
            {
                continue;
            }

            tile.ShowTile();
            losIndicatorTiles.Add(tile);
        }
    }

    void ClearLOSIndicator()
    {
        for (int i = 0; i < losIndicatorTiles.Count; i++)
        {
            if (losIndicatorTiles[i] != null)
            {
                losIndicatorTiles[i].HideTile();
            }
        }

        losIndicatorTiles.Clear();
    }

    List<OverlayTile> BuildLineOfSightTiles(OverlayTile sourceTile, OverlayTile targetTile)
    {
        List<OverlayTile> lineTiles = new List<OverlayTile>();

        if (sourceTile == null || targetTile == null || map == null)
        {
            return lineTiles;
        }

        Vector2Int start = sourceTile.gridLocation;
        Vector2Int end = targetTile.gridLocation;

        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;

        bool isFirstTile = true;

        while (true)
        {
            Vector2Int currentKey = new Vector2Int(x0, y0);

            if (map.TryGetValue(currentKey, out OverlayTile currentTile))
            {
                lineTiles.Add(currentTile);

                if (!isFirstTile && currentKey != end && currentTile.RestingObject != null)
                {
                    CursorHandler.i.UpdateCursor(CursorState.LOSX);
                    break;
                }
            }

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int doubledError = error * 2;

            if (doubledError >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (doubledError <= dx)
            {
                error += dx;
                y0 += sy;
            }

            isFirstTile = false;
        }

        CursorHandler.i.UpdateCursor(CursorState.LOSCheck);
        return lineTiles;
    }
}