using System.Collections.Generic;
using UnityEngine;

public enum AnimationSlot
{
    Idle,
    Walking,
    MeleeBase,
    RangedBase,
    Dying,
    Reload
}

/// <summary>
/// CombatSpriteDepot stores idle, melee, ranged etc. sprite animations.
/// </summary>
[CreateAssetMenu(menuName = "Unit/Create Combat Sprite Depot")]
public class CombatSpriteDepot : ScriptableObject
{
    [Header("Use parser tool instead of filling these manually")]
    [SerializeField] ObjectAnimation idle = new ObjectAnimation();
    [SerializeField] ObjectAnimation walking = new ObjectAnimation();
    [SerializeField] ObjectAnimation meleeBase = new ObjectAnimation();
    [SerializeField] ObjectAnimation rangedBase = new ObjectAnimation();
    [SerializeField] ObjectAnimation dying = new ObjectAnimation();
    [SerializeField] float offset;

    [Header("Attack SFX")]
    [SerializeField] AudioClip meleeAttackSfx;
    [SerializeField] AudioClip rangedAttackSfx;

    [Header("Ranged-specific")]
    [SerializeField] ObjectAnimation reload = new ObjectAnimation();

    public ObjectAnimation Idle => idle;
    public ObjectAnimation Walking => walking;
    public ObjectAnimation MeleeBase => meleeBase;
    public ObjectAnimation RangedBase => rangedBase;
    public ObjectAnimation Dying => dying;
    public float Offset => offset;
    public AudioClip MeleeAttackSfx => meleeAttackSfx;
    public AudioClip RangedAttackSfx => rangedAttackSfx;
    public ObjectAnimation Reload => reload;

    //SPRITESHEET PARSER

    [Header("Spritesheet Batch Parser")]
    [SerializeField] private AnimationSlot targetSlot = AnimationSlot.Idle;
    [SerializeField] private List<Sprite> spritesToParse = new List<Sprite>();
    [SerializeField, Min(1)] private int framesPerDirection = 42;
    [SerializeField] private IsoDirection startingOrientation = IsoDirection.DownRight;
    [SerializeField] private bool counterClockwise = true;
    [SerializeField] private float defaultFrameDelay = 0.05f;

    /// <summary>
    /// Parses a flat list of sprites into the target ObjectAnimation based on frame length,
    /// starting orientation, and rotation direction. Automatically infers 4-way or 8-way
    /// from the total sprite count divided by framesPerDirection.
    /// </summary>
    public void ParseSprites(
        AnimationSlot slot,
        IReadOnlyList<Sprite> sprites,
        int animationLength,
        IsoDirection startOrientation = IsoDirection.DownRight,
        bool isCounterClockwise = true,
        float frameDelay = -1f)
    {
        if (sprites == null || sprites.Count == 0)
        {
            Debug.LogWarning($"[{name}] ParseSprites aborted: Sprite list is empty.");
            return;
        }

        if (animationLength <= 0)
        {
            Debug.LogWarning($"[{name}] ParseSprites aborted: animationLength must be > 0.");
            return;
        }

        ObjectAnimation targetAnim = GetAnimationBySlot(slot);
        if (targetAnim == null)
        {
            Debug.LogError($"[{name}] Target animation slot '{slot}' is null.");
            return;
        }

        int directionCount = sprites.Count / animationLength;
        if (directionCount != 4 && directionCount != 8)
        {
            Debug.LogWarning(
                $"[{name}] Expected 4 or 8 directions, but got {directionCount} " +
                $"({sprites.Count} sprites / {animationLength} frames). Parsing available slices anyway.");
        }

        if (sprites.Count % animationLength != 0)
        {
            Debug.LogWarning(
                $"[{name}] Sprite count ({sprites.Count}) is not evenly divisible by animationLength ({animationLength}). " +
                $"Trailing sprites will be ignored.");
        }

        // Step size in the 8-direction enum: 1 for 8-way, 2 for 4-way (skips cardinals/diagonals appropriately)
        int enumStep = (directionCount == 4) ? 2 : 1;
        int rotationSign = isCounterClockwise ? 1 : -1;

        targetAnim.Clear();
        if (frameDelay > 0f)
        {
            targetAnim.animationDelay = frameDelay;
        }

        for (int dirIndex = 0; dirIndex < directionCount; dirIndex++)
        {
            // Wrap cleanly around 0..7 in either clockwise or counter-clockwise order
            int rawDir = ((int)startOrientation + (dirIndex * enumStep * rotationSign)) % 8;
            if (rawDir < 0) rawDir += 8;

            IsoDirection mappedDir = (IsoDirection)rawDir;

            int startIdx = dirIndex * animationLength;
            List<Sprite> slice = new List<Sprite>(animationLength);
            for (int f = 0; f < animationLength; f++)
            {
                slice.Add(sprites[startIdx + f]);
            }

            targetAnim.SetSprites(mappedDir, slice);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[{name}] Parsed {directionCount} directions x {animationLength} frames into '{slot}'.");
    }

    public ObjectAnimation GetAnimationBySlot(AnimationSlot slot)
    {
        switch (slot)
        {
            case AnimationSlot.Idle:       return idle;
            case AnimationSlot.Walking:    return walking;
            case AnimationSlot.MeleeBase:  return meleeBase;
            case AnimationSlot.RangedBase: return rangedBase;
            case AnimationSlot.Dying:      return dying;
            case AnimationSlot.Reload:     return reload;
            default:                       return null;
        }
    }

    [ContextMenu("Parse Assigned Sprites Into Target Slot")]
    private void ParseFromInspector()
    {
        ParseSprites(
            targetSlot,
            spritesToParse,
            framesPerDirection,
            startingOrientation,
            counterClockwise,
            defaultFrameDelay
        );
    }
}