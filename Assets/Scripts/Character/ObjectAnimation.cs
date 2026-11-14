using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum IsoDirection
{
    DownRight = 0, // SE
    Right     = 1, // E
    UpRight   = 2, // NE
    Up        = 3, // N
    UpLeft    = 4, // NW
    Left      = 5, // W
    DownLeft  = 6, // SW
    Down      = 7  // S
}

/// <summary>
/// ObjectAnimation stores directional lists of sprites and its delay between frames (in seconds).
/// </summary>
[System.Serializable]
public class ObjectAnimation
{
    public static readonly Vector2Int UpRight   = new Vector2Int(1, 1);
    public static readonly Vector2Int DownRight = new Vector2Int(1, -1);
    public static readonly Vector2Int UpLeft    = new Vector2Int(-1, 1);
    public static readonly Vector2Int DownLeft  = new Vector2Int(-1, -1);
    public static readonly Vector2Int Up        = new Vector2Int(0, 1);
    public static readonly Vector2Int Down      = new Vector2Int(0, -1);
    public static readonly Vector2Int Left      = new Vector2Int(-1, 0);
    public static readonly Vector2Int Right     = new Vector2Int(1, 0);

    // Ordered counter-clockwise starting from DownRight (SE) -> Right (E) -> UpRight (NE) -> Up (N) -> UpLeft (NW) -> Left (W) -> DownLeft (SW) -> Down (S)
    [FormerlySerializedAs("_rightAnimationSprites")]
    [SerializeField] private List<Sprite> _downRightAnimationSprites = new List<Sprite>();
    [SerializeField] private List<Sprite> _rightAnimationSprites = new List<Sprite>();
    [FormerlySerializedAs("_upAnimationSprites")]
    [SerializeField] private List<Sprite> _upRightAnimationSprites = new List<Sprite>();
    [SerializeField] private List<Sprite> _upAnimationSprites = new List<Sprite>();
    [FormerlySerializedAs("_leftAnimationSprites")]
    [SerializeField] private List<Sprite> _upLeftAnimationSprites = new List<Sprite>();
    [SerializeField] private List<Sprite> _leftAnimationSprites = new List<Sprite>();
    [FormerlySerializedAs("_downAnimationSprites")]
    [SerializeField] private List<Sprite> _downLeftAnimationSprites = new List<Sprite>();
    [SerializeField] private List<Sprite> _downAnimationSprites = new List<Sprite>();

    [SerializeField, HideInInspector] private List<Sprite> _animationSprites = new List<Sprite>();
    [SerializeField] private float _animationDelay = 0.08f;
    [SerializeField] private Vector2Int _facingDirection = DownRight;

    public float animationDelay { get { return _animationDelay; } set { _animationDelay = value; } }
    public Vector2Int facingDirection { get { return NormalizeDirection(_facingDirection); } }

    public bool HasSprites =>
        _downRightAnimationSprites.Count > 0 ||
        _rightAnimationSprites.Count > 0 ||
        _upRightAnimationSprites.Count > 0 ||
        _upAnimationSprites.Count > 0 ||
        _upLeftAnimationSprites.Count > 0 ||
        _leftAnimationSprites.Count > 0 ||
        _downLeftAnimationSprites.Count > 0 ||
        _downAnimationSprites.Count > 0 ||
        _animationSprites.Count > 0;

    public void Clear()
    {
        _downRightAnimationSprites.Clear();
        _rightAnimationSprites.Clear();
        _upRightAnimationSprites.Clear();
        _upAnimationSprites.Clear();
        _upLeftAnimationSprites.Clear();
        _leftAnimationSprites.Clear();
        _downLeftAnimationSprites.Clear();
        _downAnimationSprites.Clear();
        _animationSprites.Clear();
    }

    public void SetSprites(IsoDirection dir, List<Sprite> sprites)
    {
        List<Sprite> target = GetListReference(dir);
        target.Clear();
        if (sprites != null)
        {
            target.AddRange(sprites);
        }
    }

    private List<Sprite> GetListReference(IsoDirection dir)
    {
        switch (dir)
        {
            case IsoDirection.DownRight: return _downRightAnimationSprites;
            case IsoDirection.Right:     return _rightAnimationSprites;
            case IsoDirection.UpRight:   return _upRightAnimationSprites;
            case IsoDirection.Up:        return _upAnimationSprites;
            case IsoDirection.UpLeft:    return _upLeftAnimationSprites;
            case IsoDirection.Left:      return _leftAnimationSprites;
            case IsoDirection.DownLeft:  return _downLeftAnimationSprites;
            case IsoDirection.Down:      return _downAnimationSprites;
            default:                     return _downRightAnimationSprites;
        }
    }

    public List<Sprite> GetSprites(Vector2Int direction)
    {
        direction = NormalizeDirection(direction);
        List<Sprite> directionalSprites = _downRightAnimationSprites;

        if (direction == DownRight) directionalSprites = _downRightAnimationSprites;
        else if (direction == Right)
            directionalSprites = _rightAnimationSprites.Count > 0 ? _rightAnimationSprites : _downRightAnimationSprites;
        else if (direction == UpRight) directionalSprites = _upRightAnimationSprites;
        else if (direction == Up)
            directionalSprites = _upAnimationSprites.Count > 0 ? _upAnimationSprites : _upRightAnimationSprites;
        else if (direction == UpLeft) directionalSprites = _upLeftAnimationSprites;
        else if (direction == Left)
            directionalSprites = _leftAnimationSprites.Count > 0 ? _leftAnimationSprites : _upLeftAnimationSprites;
        else if (direction == DownLeft) directionalSprites = _downLeftAnimationSprites;
        else if (direction == Down)
            directionalSprites = _downAnimationSprites.Count > 0 ? _downAnimationSprites : _downLeftAnimationSprites;

        return directionalSprites.Count > 0 ? directionalSprites : _animationSprites;
    }

    public static Vector2Int NormalizeDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
            return DownRight;

        int x = direction.x == 0 ? 0 : (direction.x > 0 ? 1 : -1);
        int y = direction.y == 0 ? 0 : (direction.y > 0 ? 1 : -1);
        return new Vector2Int(x, y);
    }

    public List<Sprite> animationSprites => GetSprites(facingDirection);
    public float GetAnimationTime(Vector2Int direction) => animationDelay * GetSprites(direction).Count;
    public float animationTime => GetAnimationTime(facingDirection);
}