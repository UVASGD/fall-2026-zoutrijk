using UnityEngine;
using UnityEngine.InputSystem;

public class OverlayTile : MonoBehaviour
{
    //A* implementation
    public int G;
    public int H;
    public int F => G+H;

    public Vector2Int gridLocation;

    public bool isBlocked;

    public OverlayTile previous;
    private SpriteRenderer overlayRenderer;

    private MapObject restingObject; //an object resting on top of the tile
    public MapObject RestingObject => restingObject;

    void Awake()
    {
        overlayRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if(Mouse.current.leftButton.wasPressedThisFrame){
            HideTile();
        }    
    }
    public void ShowTile()
    {
        overlayRenderer.color = new Color(1,1,1,1); //make it fully visible
    }
    public void HideTile()
    {
        overlayRenderer.color = new Color(1,1,1,0); //disable its alpha component.
    }
    public void SetRestingObject(MapObject mObject)
    {
        restingObject = mObject;
    }
    public void ClearRestingObject()
    {
        restingObject = null;
    }
}