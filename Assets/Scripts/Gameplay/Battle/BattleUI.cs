using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The central handler for on-screen battle UI elements. Some are encapsulated into their own scripts (such as the GroupUI).
/// Currently some mechanical functionality is handled here, which calls for refactor later.
/// </summary>
public class BattleUI : MonoBehaviour
{
    public static BattleUI i;
    public List<FieldCharacter> highlightedUnits = new List<FieldCharacter>();
    private Vector2 selectorOrigin;
    private bool isSelecting;

    [SerializeField] private Image selectorBox;
    [SerializeField] private GroupUI groupUI;
    [SerializeField] private Material movementPathMaterial;
    [SerializeField] private Color movementPathColor = new Color(0.2f, 0.85f, 1f, 1f);
    [SerializeField] private float movementPathWidth = 0.12f;
    [SerializeField] private int movementPathSortingOrder = 10;
    private readonly List<LineRenderer> activeMovementLines = new List<LineRenderer>();

    public List<FieldCharacter> HighlightedUnits => highlightedUnits;
    public Vector2 SelectorOrigin => selectorOrigin;

    void Awake()
    {
        if (i == null)
        {
            i = this;
        }
    }

    public void SetSelecting(bool selecting)
    {
        isSelecting = selecting;

        if (selectorBox == null)
        {
            return;
        }

        selectorBox.gameObject.SetActive(selecting);

        if (selecting)
        {
            PlaceSelectorOrigin(MouseController.i.CurrentMousePos);
        }
    }

    public void SetHighlightedUnits(List<FieldCharacter> units)
    {
        highlightedUnits.Clear();

        if (units == null)
        {
            return;
        }

        highlightedUnits.AddRange(units.Where(unit => unit != null));
    }

    public void ClearHighlightedUnits()
    {
        highlightedUnits.Clear();
    }

    public void PlaceSelectorOrigin(Vector2 origin)
    {
        selectorOrigin = origin;
        selectorBox.rectTransform.position = origin;
    }

    public void UpdateSelectorCorner()
    {
        Vector2 currentPosition = MouseController.i.CurrentMousePos;
        Vector2 bottomLeft = new Vector2(Mathf.Min(selectorOrigin.x, currentPosition.x), Mathf.Min(selectorOrigin.y, currentPosition.y));
        Vector2 topRight = new Vector2(Mathf.Max(selectorOrigin.x, currentPosition.x), Mathf.Max(selectorOrigin.y, currentPosition.y));

        selectorBox.rectTransform.position = bottomLeft;
        selectorBox.rectTransform.sizeDelta = topRight - bottomLeft;
    }

    void Update()
    {
        if (isSelecting)
        {
            UpdateSelectorCorner();
        }
    }

    public LineRenderer CreateMovementPath(List<OverlayTile> tiles)
    {
        return null;
    }

    public void DestroyMovementPath(LineRenderer lineRenderer)
    {
        if (lineRenderer == null)
        {
            return;
        }
    }

    public void AttemptGrouping()
    {
        if (highlightedUnits.Count > 0)
        {
            groupUI.AddGroup(new SoldierGroup(highlightedUnits));
            highlightedUnits.Clear();
            groupUI.UpdateNumberOfSelectedUnits(0, true);

            Debug.Log("Attempting to group selected units.");
        }
    }
}