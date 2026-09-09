using System.Collections.Generic;
using UnityEngine;

public class CursorHandler : MonoBehaviour
{
    public static CursorHandler i;
    [SerializeField] List<Texture2D> staticCursors;
    private CursorState cursorState;
    private Vector2 hotSpot;
    private CursorMode cursorMode = CursorMode.Auto;
    void Awake()
    {
        if(i==null) i = this;
    }
    
    public void UpdateCursor(CursorState cursorState)
    {
        this.cursorState = cursorState;
        Cursor.SetCursor(staticCursors[(int)this.cursorState], hotSpot, cursorMode); //cast the cursorstate into an int to find the texture index.
    }
    
    /// <summary>
    /// place logic correlating to animated cursors here.
    /// </summary>
    void Update()
    {
        
    }
}
public enum CursorState
{
    Default,
    Movement,
    LOSCheck, //line of sight succeeding
    LOSX, //line of sight failing
    MeleeAttack,
}