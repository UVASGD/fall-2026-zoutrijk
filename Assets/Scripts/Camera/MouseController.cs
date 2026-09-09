using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseController : MonoBehaviour
{
    #region fields
    public static MouseController i;
    private PathFinder pathFinder;
    public Vector2 CurrentMousePos => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

    private OverlayTile hoveredTile; //the tile that the mouse is currently hovering over
    [SerializeField] private OverlayTile clickedTile; //the last tile that was left clicked
    public event Action<BattleState> updateBattleState;
    private readonly List<OverlayTile> formationPreviewTiles = new List<OverlayTile>();
    private bool isRightDraggingFormation;
    private bool suppressRegularRightClickThisFrame;
    private OverlayTile formationDragOrigin;
    private Vector2 formationDragStartScreen;
    [SerializeField] private float formationDragDeadZonePixels = 16f;

    //unit movement fields
    public List<FieldCharacter> selectedCharacters; //the list of selected fieldCharacters. Right clicking a tile will move the formation.
    private OverlayTile characterToMoveSource; //the tile which the characterToMove resides.
    private bool isSelecting;
    private readonly Queue<IEnumerator> movementQueue = new Queue<IEnumerator>();
    private bool isProcessingMovementQueue;
    public bool MovementPaused => BattleManager.i != null && BattleManager.i.battleSpeed == 0;
    #endregion

    void Awake()
    {
        i = this;
        pathFinder = new PathFinder();

        selectedCharacters = new List<FieldCharacter>();
    }

    private void Update()
    {
        if (isSelecting)
        {
            BattleUI.i.UpdateSelectorCorner();
        }

        if (Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame && selectedCharacters.Count > 1 && hoveredTile != null)
        {
            formationDragOrigin = hoveredTile;
            formationDragStartScreen = Mouse.current.position.ReadValue();
            isRightDraggingFormation = false;
            ClearFormationPreview();
        }
        else if (Mouse.current.rightButton.isPressed && formationDragOrigin != null && selectedCharacters.Count > 1)
        {
            float dragDistance = Vector2.Distance(Mouse.current.position.ReadValue(), formationDragStartScreen);
            isRightDraggingFormation = dragDistance > formationDragDeadZonePixels;

            if (isRightDraggingFormation)
            {
                UpdateFormationPreview(formationDragOrigin, hoveredTile);
            }
            else
            {
                ClearFormationPreview();
            }
        }
        else if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            suppressRegularRightClickThisFrame = false;

            if (formationDragOrigin != null && selectedCharacters.Count > 1)
            {
                float dragDistance = Vector2.Distance(Mouse.current.position.ReadValue(), formationDragStartScreen);
                if (dragDistance > formationDragDeadZonePixels)
                {
                    suppressRegularRightClickThisFrame = true;
                    QueueMovement(MoveFormation(selectedCharacters, formationDragOrigin, hoveredTile));
                }
                else
                {
                    HandleRegularRightClick();
                }
            }
            else if (hoveredTile != null)
            {
                HandleRegularRightClick();
            }

            formationDragOrigin = null;
            formationDragStartScreen = Vector2.zero;
            isRightDraggingFormation = false;
            ClearFormationPreview();
        }
    }

    public void BeginSelection()
    {
        isSelecting = true;
        BattleUI.i.SetSelecting(true);
    }

    public void EndSelection()
    {
        if (!isSelecting)
        {
            return;
        }

        isSelecting = false;
        BattleUI.i.SetSelecting(false);

        List<FieldCharacter> highlightedUnits = HighlightAllUnitsInRegion(HandleDragRange(BattleUI.i.SelectorOrigin, CurrentMousePos));

        if (highlightedUnits.Count == 0 && clickedTile != null && clickedTile.RestingObject is FieldCharacter clickedCharacter && clickedCharacter.PlayerControlled)
        {
            highlightedUnits = new List<FieldCharacter> { clickedCharacter };
        }
        else if (highlightedUnits.Count == 0 && hoveredTile != null && hoveredTile.RestingObject is FieldCharacter hoveredCharacter && hoveredCharacter.PlayerControlled)
        {
            highlightedUnits = new List<FieldCharacter> { hoveredCharacter };
        }

        bool addToSelection = BattleKeyboardManager.i != null
            ? BattleKeyboardManager.i.IsCtrlModifierHeld()
            : Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;

        if (addToSelection)
        {
            AddToSelection(highlightedUnits);
        }
        else
        {
            ApplySelection(highlightedUnits);
        }
    }

    public void HandleUpdate(BattleState battleState)
    {
        if (Mouse.current == null || Camera.main == null)
        {
            return;
        }

        OverlayTile focusedTile = GetOverlayTileFromMousePos();

        if (focusedTile != null)
        {
            transform.position = focusedTile.transform.position;
            hoveredTile = focusedTile;

            HandleFocusedTile(battleState);
        }
    }
    private void HandleFocusedTile(BattleState battleState)
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            clickedTile = hoveredTile;
            clickedTile?.ShowTile();

            if (clickedTile.RestingObject != null)
            {
                WorldObjectPreviewUI.i.OpenMenu(clickedTile.RestingObject);
                updateBattleState?.Invoke(BattleState.UnitSelected);

                if (clickedTile.RestingObject is FieldCharacter fieldCharacter && fieldCharacter.PlayerControlled)
                {
                    bool addToSelection = BattleKeyboardManager.i != null
                        ? BattleKeyboardManager.i.IsCtrlModifierHeld()
                        : Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;

                    if (addToSelection)
                    {
                        AddToSelection(new List<FieldCharacter> { fieldCharacter });
                    }
                    else
                    {
                        ApplySelection(new List<FieldCharacter> { fieldCharacter });
                    }
                }
                else if (BattleKeyboardManager.i == null || !BattleKeyboardManager.i.IsCtrlModifierHeld())
                {
                    ClearSelectedUnits();
                }
            }
        }
        else if (Mouse.current.rightButton.wasReleasedThisFrame && hoveredTile != null && !suppressRegularRightClickThisFrame)
        {
            HandleRegularRightClick();
        }
        else if (battleState == BattleState.CheckingLOS && clickedTile != null && hoveredTile != null)
        {
            MapManager.i.UpdateLOSIndicator(clickedTile, hoveredTile);
        }
        else if (Keyboard.current.rKey.wasPressedThisFrame && battleState == BattleState.UnitSelected)
        {
            updateBattleState?.Invoke(BattleState.CheckingLOS);
        }
    }
    private void HandleRegularRightClick()
    {
        if (hoveredTile == null)
        {
            return;
        }

        if (hoveredTile.RestingObject == null)
        {
            if (selectedCharacters.Count > 1)
            {
                QueueMovement(MoveFormation(selectedCharacters, hoveredTile));
            }
            if (selectedCharacters.Count == 1)
            {
                QueueMovement(MoveCharacter(selectedCharacters[0], hoveredTile));
            }
        }
        else if (hoveredTile.RestingObject is FieldCharacter restingCharacter && selectedCharacters.Count > 0)
        {
            if (!restingCharacter.PlayerControlled)
            {
                foreach (FieldCharacter attacker in selectedCharacters)
                {
                    if (attacker == null)
                    {
                        continue;
                    }

                    if (GlobalEditorSettings.i.RichDebugLogs) Debug.Log($"Attacking {restingCharacter.name} with {attacker.name}");
                    attacker.SetAttackTarget(restingCharacter);
                }
            }
        }
    }

    private List<FieldCharacter> HighlightAllUnitsInRegion(List<OverlayTile> tiles)
    {
        List<FieldCharacter> highlightedUnits = new List<FieldCharacter>();

        if (tiles == null)
        {
            return highlightedUnits;
        }

        foreach (OverlayTile tile in tiles)
        {
            if (tile.RestingObject != null && tile.RestingObject is FieldCharacter)
            {
                FieldCharacter character = (FieldCharacter)tile.RestingObject;
                if (character.PlayerControlled)
                {
                    tile.ShowTile();
                    highlightedUnits.Add(character);
                }
            }
        }

        return highlightedUnits;
    }

    public OverlayTile GetOverlayTileFromMousePos()
    {
        var focusedTileHit = GetFocusedOnTile();
        if (focusedTileHit.HasValue)
        {
            GameObject overlayTile = focusedTileHit.Value.collider.gameObject;
            hoveredTile = overlayTile.GetComponent<OverlayTile>();

            return hoveredTile;
        }

        return null;
    }

    public OverlayTile GetOverlayTileFromPosition(Vector2 position)
    {
        var focusedTileHit = GetTileFromPos(position);

        if (focusedTileHit.HasValue)
        {
            GameObject overlayTile = focusedTileHit.Value.collider.gameObject;
            hoveredTile = overlayTile.GetComponent<OverlayTile>();

            return hoveredTile;
        }

        return null;
    }
    public RaycastHit2D? GetFocusedOnTile()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        return GetTileFromPos(mousePosition);
    }

    /// <summary>
    /// Helper function for GetFocusedOnTile.
    /// </summary>
    /// <param name="mousePos"></param>
    /// <returns></returns>
    private RaycastHit2D? GetTileFromPos(Vector2 mousePos)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);

        if (hits.Length > 0)
        {
            return hits.OrderByDescending(i => i.collider.transform.position.z).First();
        }

        return null;
    }

    /// <summary>
    /// Moves a field character on the grid. Uses a coroutine to display the A* pathfinding arrows correctly. Adding parameters allows for multiple movements simultaneously.
    /// </summary>
    /// <returns></returns>
    public void QueueMovement(IEnumerator movementRoutine)
    {
        if (movementRoutine == null)
        {
            return;
        }

        StartCoroutine(movementRoutine);
    }

    private IEnumerator MoveCharacter(FieldCharacter toMove, OverlayTile destination)
    {
        List<OverlayTile> path = pathFinder.FindPath(toMove.TilePosition, destination);

        while (MovementPaused)
        {
            yield return null;
        }

        yield return MoveCharacterAlongPath(toMove, destination, path, true, null);
    }

    private IEnumerator MoveCharacterAlongPath(FieldCharacter toMove, OverlayTile destination, List<OverlayTile> path, bool drawPathArrows, Action onComplete)
    {
        if (path == null)
        {
            yield break;
        }

        bool isStationaryMove = path.Count == 0 && toMove.TilePosition == destination;
        if (path.Count == 0 && !isStationaryMove)
        {
            yield break;
        }

        if (drawPathArrows && path.Count > 0)
        {
            MapManager.i.DestroyPathfindingArrows();
        }

        try
        {
            if (toMove.TilePosition != null)
            {
                toMove.TilePosition.HideTile();
                toMove.TilePosition.ClearRestingObject();
            }

            if (path.Count > 0)
            {
                yield return toMove.setMoveOrders(path);
            }

            if (destination != null)
            {
                destination.SetRestingObject(toMove);
                destination.ShowTile();
            }
        }
        finally
        {
            if (drawPathArrows && path.Count > 0)
            {
                MapManager.i.DestroyPathfindingArrows();
            }

            onComplete?.Invoke();
        }
    }

    private IEnumerator MoveFormation(List<FieldCharacter> units, OverlayTile destination)
    {
        if (!TryBuildFormationMovePlan(units, destination, out List<FormationMovePlan> movePlan))
        {
            yield break;
        }

        yield return MoveFormationInternal(units, movePlan);
    }

    private IEnumerator MoveFormation(List<FieldCharacter> units, OverlayTile origin, OverlayTile dragTarget)
    {
        if (!TryBuildFormationMovePlan(units, origin, dragTarget, out List<FormationMovePlan> movePlan))
        {
            yield break;
        }

        yield return MoveFormationInternal(units, movePlan);
    }

    private IEnumerator MoveFormationInternal(List<FieldCharacter> units, List<FormationMovePlan> movePlan)
    {
        if (movePlan == null || movePlan.Count == 0)
        {
            yield break;
        }

        float formationSpeed = units
            .Where(unit => unit != null)
            .Select(unit => unit.MoveSpeedForFormation(units))
            .DefaultIfEmpty(0f)
            .Min();

        foreach (var unit in units)
        {
            if (unit != null)
            {
                unit.SetFormationMoveSpeed(formationSpeed);
            }

            if (unit != null && unit.TilePosition != null)
            {
                unit.TilePosition.ClearRestingObject();
            }
        }

        List<Coroutine> moveCoroutines = new List<Coroutine>();

        foreach (var move in movePlan)
        {
            if (move.Unit == null || move.TargetTile == null)
            {
                continue;
            }

            while (MovementPaused)
            {
                yield return null;
            }

            moveCoroutines.Add(StartCoroutine(MoveCharacterAlongPath(move.Unit, move.TargetTile, move.Path, false, null)));
        }

        foreach (var moveCoroutine in moveCoroutines)
        {
            if (moveCoroutine != null)
            {
                yield return moveCoroutine;
            }
        }

        foreach (var unit in units)
        {
            if (unit != null)
            {
                unit.ClearFormationMoveSpeed();
            }
        }

        if (selectedCharacters != null)
        {
            bool clearedAnySelectedUnits = false;
            for (int i = selectedCharacters.Count - 1; i >= 0; i--)
            {
                if (units.Contains(selectedCharacters[i]))
                {
                    selectedCharacters.RemoveAt(i);
                    clearedAnySelectedUnits = true;
                }
            }

            if (clearedAnySelectedUnits)
            {
                if (selectedCharacters.Count > 0)
                {
                    BattleUI.i.SetHighlightedUnits(selectedCharacters);
                    GroupUI.i.UpdateNumberOfSelectedUnits(selectedCharacters.Count);
                }
                else
                {
                    BattleUI.i.ClearHighlightedUnits();
                    GroupUI.i.UpdateNumberOfSelectedUnits(0, true);
                }
            }
        }
    }

    private bool TryBuildFormationMovePlan(List<FieldCharacter> units, OverlayTile destination, out List<FormationMovePlan> movePlan)
    {
        movePlan = new List<FormationMovePlan>();

        if (units == null || units.Count == 0 || destination == null || MapManager.i == null)
        {
            return false;
        }

        List<FieldCharacter> activeUnits = units.Where(unit => unit != null && unit.TilePosition != null).ToList();
        if (activeUnits.Count == 0)
        {
            return false;
        }

        return TryBuildFormationMovePlan(activeUnits, destination, destination, out movePlan);
    }

    private bool TryBuildFormationMovePlan(List<FieldCharacter> units, OverlayTile origin, OverlayTile dragTarget, out List<FormationMovePlan> movePlan)
    {
        movePlan = new List<FormationMovePlan>();

        if (units == null || units.Count == 0 || origin == null || dragTarget == null || MapManager.i == null)
        {
            return false;
        }

        List<FieldCharacter> activeUnits = units.Where(unit => unit != null && unit.TilePosition != null).ToList();
        if (activeUnits.Count == 0)
        {
            return false;
        }

        List<Vector2Int> targetLocations = GetFormationTargetLocations(activeUnits, origin, dragTarget);
        if (targetLocations.Count < activeUnits.Count)
        {
            return false;
        }

        HashSet<OverlayTile> selectedTiles = new HashSet<OverlayTile>(activeUnits.Select(unit => unit.TilePosition));
        HashSet<OverlayTile> reservedTargetTiles = new HashSet<OverlayTile>();

        for (int i = 0; i < activeUnits.Count; i++)
        {
            if (i >= targetLocations.Count)
            {
                return false;
            }

            FieldCharacter unit = activeUnits[i];
            Vector2Int targetLocation = targetLocations[i];

            if (!MapManager.i.TryGetOverlayTile(targetLocation, out OverlayTile targetTile))
            {
                return false;
            }

            if (!IsFormationTargetAvailable(targetTile, selectedTiles) || reservedTargetTiles.Contains(targetTile))
            {
                targetTile = GetNearestFreeAdjacentTile(origin, selectedTiles, reservedTargetTiles);
            }

            if (targetTile == null || reservedTargetTiles.Contains(targetTile))
            {
                return false;
            }

            List<OverlayTile> path = pathFinder.FindPath(unit.TilePosition, targetTile, selectedTiles);
            if (path.Count == 0 && unit.TilePosition != targetTile)
            {
                return false;
            }

            reservedTargetTiles.Add(targetTile);
            movePlan.Add(new FormationMovePlan
            {
                Unit = unit,
                TargetTile = targetTile,
                Path = path
            });
        }

        return true;
    }

    private List<Vector2Int> GetFormationTargetLocations(List<FieldCharacter> units, OverlayTile origin, OverlayTile dragTarget)
    {
        List<Vector2Int> targetLocations = new List<Vector2Int>();

        if (units == null || units.Count == 0 || origin == null || dragTarget == null)
        {
            return targetLocations;
        }

        Vector2Int dragDelta = dragTarget.gridLocation - origin.gridLocation;
        if (dragDelta == Vector2Int.zero)
        {
            Vector2Int formationAnchor = Vector2Int.zero;
            foreach (FieldCharacter unit in units)
            {
                if (unit != null && unit.TilePosition != null)
                {
                    formationAnchor += unit.TilePosition.gridLocation;
                }
            }

            if (units.Count > 0)
            {
                formationAnchor /= units.Count;
            }

            foreach (FieldCharacter unit in units)
            {
                if (unit == null || unit.TilePosition == null)
                {
                    continue;
                }

                Vector2Int relativeOffset = unit.TilePosition.gridLocation - formationAnchor;
                targetLocations.Add(dragTarget.gridLocation + relativeOffset);
            }

            return targetLocations;
        }

        Vector2Int rotatedDelta = new Vector2Int(-dragDelta.x - dragDelta.y, -dragDelta.x + dragDelta.y);
        Vector2Int axis = new Vector2Int(
            Mathf.Abs(rotatedDelta.x) >= Mathf.Abs(rotatedDelta.y) ? (int)Mathf.Sign(rotatedDelta.x) : 0,
            Mathf.Abs(rotatedDelta.y) > Mathf.Abs(rotatedDelta.x) ? (int)Mathf.Sign(rotatedDelta.y) : 0);

        if (axis == Vector2Int.zero)
        {
            axis = new Vector2Int(0, 1);
        }

        int dragMagnitude = Mathf.Abs(dragDelta.x) + Mathf.Abs(dragDelta.y);
        float spread = Mathf.Clamp01((float)dragMagnitude / Mathf.Max(1f, units.Count));

        int deepCount = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(units.Count, 1f, spread)));
        int wideCount = Mathf.Max(1, Mathf.CeilToInt((float)units.Count / deepCount));

        Vector2Int side = new Vector2Int(axis.y, -axis.x);

        int index = 0;
        for (int rank = 0; rank < deepCount && index < units.Count; rank++)
        {
            int rowSize = Mathf.Max(1, units.Count - (rank * wideCount));
            rowSize = Mathf.Min(rowSize, wideCount);

            for (int file = 0; file < rowSize && index < units.Count; file++)
            {
                Vector2Int offset = (axis * rank) + (side * file);
                targetLocations.Add(origin.gridLocation + offset);
                index++;
            }
        }

        return targetLocations;
    }

    private void UpdateFormationPreview(OverlayTile origin, OverlayTile destination)
    {
        ClearFormationPreview();

        if (origin == null || selectedCharacters == null || selectedCharacters.Count <= 1)
        {
            return;
        }

        List<FieldCharacter> activeUnits = selectedCharacters.Where(unit => unit != null && unit.TilePosition != null).ToList();
        if (activeUnits.Count <= 1)
        {
            return;
        }

        List<Vector2Int> targetLocations = GetFormationTargetLocations(activeUnits, origin, destination ?? origin);

        foreach (Vector2Int targetLocation in targetLocations)
        {
            if (MapManager.i != null && MapManager.i.TryGetOverlayTile(targetLocation, out OverlayTile tile) && tile != null)
            {
                tile.ShowTile();
                formationPreviewTiles.Add(tile);
            }
        }
    }

    private void ClearFormationPreview()
    {
        foreach (OverlayTile tile in formationPreviewTiles)
        {
            if (tile != null)
            {
                tile.HideTile();
            }
        }

        formationPreviewTiles.Clear();
    }

    private OverlayTile GetNearestFreeAdjacentTile(OverlayTile destination, HashSet<OverlayTile> selectedTiles, HashSet<OverlayTile> reservedTargetTiles)
    {
        if (destination == null || MapManager.i == null || MapManager.i.map == null)
        {
            return null;
        }

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

        List<OverlayTile> candidates = new List<OverlayTile>();

        foreach (Vector2Int direction in directions)
        {
            Vector2Int adjacentLocation = destination.gridLocation + direction;
            if (!MapManager.i.TryGetOverlayTile(adjacentLocation, out OverlayTile tile))
            {
                continue;
            }

            if (tile.isBlocked || reservedTargetTiles.Contains(tile))
            {
                continue;
            }

            if (tile.RestingObject == null)
            {
                candidates.Add(tile);
                continue;
            }

            if (tile.RestingObject is FieldCharacter restingCharacter && selectedTiles.Contains(tile) && restingCharacter == selectedCharacters.FirstOrDefault(unit => unit != null && unit.TilePosition == tile))
            {
                candidates.Add(tile);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates
            .OrderBy(tile => Mathf.Abs(tile.gridLocation.x - destination.gridLocation.x) + Mathf.Abs(tile.gridLocation.y - destination.gridLocation.y))
            .First();
    }

    private bool IsFormationTargetAvailable(OverlayTile tile, HashSet<OverlayTile> selectedTiles)
    {
        if (tile == null || tile.isBlocked)
        {
            return false;
        }

        if (tile.RestingObject == null)
        {
            return true;
        }

        if (tile.RestingObject is FieldCharacter restingCharacter)
        {
            return selectedCharacters.Contains(restingCharacter) && selectedTiles.Contains(tile);
        }

        return false;
    }

    /// <summary>
    /// Unused utility at the moment, might delete later.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    private int GetTileDistance(OverlayTile source, OverlayTile destination)
    {
        Vector2Int delta = source.gridLocation - destination.gridLocation;
        return Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
    }


    /// <summary>
    /// Checks a square from clickdrag selection and returns the list of all overlay tiles.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    public List<OverlayTile> HandleDragRange(Vector2 origin, Vector2 destination)
    {
        if (MapManager.i == null || MapManager.i.map == null)
        {
            return null;
        }

        Vector2 min = Vector2.Min(origin, destination);
        Vector2 max = Vector2.Max(origin, destination);

        List<OverlayTile> rectangleTiles = new List<OverlayTile>();

        foreach (OverlayTile tile in MapManager.i.map.Values)
        {
            Vector3 tileScreenPosition = Camera.main.WorldToScreenPoint(tile.transform.position);

            if (tileScreenPosition.x < min.x || tileScreenPosition.x > max.x)
            {
                continue;
            }

            if (tileScreenPosition.y < min.y || tileScreenPosition.y > max.y)
            {
                continue;
            }

            rectangleTiles.Add(tile);
        }

        return rectangleTiles; //returns the tiles that were highlighted that can be then filtered quickly using LINQ or something
    }

    public void AddToSelection(List<FieldCharacter> units)
    {
        if (units == null || units.Count == 0)
        {
            return;
        }

        List<FieldCharacter> mergedUnits = selectedCharacters
            .Concat(units)
            .Where(unit => unit != null)
            .Distinct()
            .ToList();

        if (mergedUnits.Count == 0)
        {
            ClearSelectedUnits();
            return;
        }

        SetSelectedUnits(mergedUnits);
    }

    public void ApplySelection(List<FieldCharacter> units)
    {
        if (units == null || units.Count == 0)
        {
            ClearSelectedUnits();
            return;
        }

        if (units.Count == 1)
        {
            SetSelectedUnits(new List<FieldCharacter> { units[0] });
            return;
        }

        SetSelectedUnits(units);
    }

    public void SetSelectedUnits(List<FieldCharacter> units)
    {
        selectedCharacters.Clear();
        selectedCharacters.AddRange(units.Where(unit => unit != null));
        selectedCharacters.ForEach(unit => unit.TilePosition?.ShowTile());
        characterToMoveSource = selectedCharacters.Count > 0 ? selectedCharacters[0].TilePosition : null;
        BattleUI.i.SetHighlightedUnits(selectedCharacters);

        if (selectedCharacters.Count > 0)
        {
            GroupUI.i.UpdateNumberOfSelectedUnits(selectedCharacters.Count);
        }
        else
        {
            GroupUI.i.UpdateNumberOfSelectedUnits(0, true);
        }
    }

    public void ClearSelectedUnits()
    {
        selectedCharacters.Clear();
        characterToMoveSource = null;
        BattleUI.i.ClearHighlightedUnits();
        GroupUI.i.UpdateNumberOfSelectedUnits(0, true);
    }

    private struct FormationMovePlan
    {
        public FieldCharacter Unit;
        public OverlayTile TargetTile;
        public List<OverlayTile> Path;
    }
}