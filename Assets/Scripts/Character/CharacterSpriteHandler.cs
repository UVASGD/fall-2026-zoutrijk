using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpriteHandler : MonoBehaviour
{
    private Sprite currentSprite;
    private SpriteRenderer characterRenderer;
    private SpriteRenderer rootRenderer;
    private SpriteRenderer offsetRenderer;
    public SpriteRenderer CharacterRenderer => characterRenderer;
    public Vector2Int FacingDirection { get; private set; } = ObjectAnimation.DownRight;
    public bool HasIdleSprites => idle != null && idle.HasSprites;
    public bool HasMeleeAnimation => meleeAnimation != null && meleeAnimation.HasSprites;
    public bool HasRangedAnimation => rangedAnimation != null && rangedAnimation.HasSprites;
    public bool HasWalkingAnimation => walkCycleAnimation != null && walkCycleAnimation.HasSprites;
    public int WalkingFrameCount => HasWalkingAnimation
        ? walkCycleAnimation.GetSprites(FacingDirection).Count
        : 0;
    public bool HasDyingAnimation => dyingAnimation != null && dyingAnimation.HasSprites;
    public bool HasReloadAnimation => reloadAnimation != null && reloadAnimation.HasSprites;
    public float ReloadAnimationDuration => HasReloadAnimation
        ? reloadAnimation.GetAnimationTime(FacingDirection)
        : 0f;
    
    public CharacterAnimationState animationState;
    
    ObjectAnimation idle;
    ObjectAnimation talkingAnimation;
    ObjectAnimation walkCycleAnimation;
    ObjectAnimation meleeAnimation;
    ObjectAnimation rangedAnimation;
    ObjectAnimation dyingAnimation;
    ObjectAnimation reloadAnimation;
    private Vector2Int walkingDirection = ObjectAnimation.DownRight;
    private int walkingFrameIndex;
    private float walkingFrameTime;

    //init function to be used by the FieldCharacter
    public void Setup(CombatSpriteDepot depot)
    {
        rootRenderer = GetComponent<SpriteRenderer>();
        characterRenderer = rootRenderer;

        if (Mathf.Abs(depot.Offset) > Mathf.Epsilon)
        {
            if (offsetRenderer == null)
            {
                GameObject offsetObject = new GameObject("Offset Sprite");
                offsetObject.transform.SetParent(transform, false);
                offsetRenderer = offsetObject.AddComponent<SpriteRenderer>();
                CopyRendererSettings(rootRenderer, offsetRenderer);
            }

            offsetRenderer.transform.localPosition = Vector3.up * depot.Offset;
            offsetRenderer.enabled = true;
            rootRenderer.enabled = false;
            characterRenderer = offsetRenderer;
        }
        else if (offsetRenderer != null)
        {
            offsetRenderer.enabled = false;
            rootRenderer.enabled = true;
        }

        baseColor = characterRenderer.color;

        idle = depot.Idle;
        walkCycleAnimation = depot.Walking;
        meleeAnimation = depot.MeleeBase;
        rangedAnimation = depot.RangedBase;
        dyingAnimation = depot.Dying;
        reloadAnimation = depot.Reload;

        animationState = CharacterAnimationState.Idle;
        walkingDirection = FacingDirection;

        SetFacingDirection(idle.facingDirection);
        List<Sprite> sprites = idle.GetSprites(FacingDirection);
        if (sprites.Count > 0)
        {
            characterRenderer.sprite = sprites[0];
        }

    }

    private static void CopyRendererSettings(SpriteRenderer source, SpriteRenderer destination)
    {
        destination.sharedMaterial = source.sharedMaterial;
        destination.color = source.color;
        destination.flipX = source.flipX;
        destination.flipY = source.flipY;
        destination.sortingLayerID = source.sortingLayerID;
        destination.sortingOrder = source.sortingOrder;
        destination.maskInteraction = source.maskInteraction;
        destination.spriteSortPoint = source.spriteSortPoint;
        destination.renderingLayerMask = source.renderingLayerMask;
    }

    public IEnumerator PlayDyingAnimation()
    {
        if (HasDyingAnimation)
        {
            yield return PlayOneshotAnimation(dyingAnimation);
        }
    }

    public void SetFacingDirection(Vector2Int direction)
    {
        if (direction != Vector2Int.zero)
        {
            Vector2Int normalizedDirection = ObjectAnimation.NormalizeDirection(direction);
            if (normalizedDirection != FacingDirection)
            {
                FacingDirection = normalizedDirection;
                RefreshCurrentAnimationSprite();
            }
        }
    }

    private void RefreshCurrentAnimationSprite()
    {
        ObjectAnimation animation = null;
        if (animationState == CharacterAnimationState.LoopingAnimation)
        {
            animation = walkCycleAnimation;
        }

        if (animation == null)
        {
            animation = idle;
        }

        if (animation == null)
        {
            return;
        }

        List<Sprite> sprites = animation.GetSprites(FacingDirection);
        if (sprites.Count > 0)
        {
            characterRenderer.sprite = sprites[0];
        }
    }

    public IEnumerator PlayOneshotAnimation(ObjectAnimation animation)
    {
        animationState = CharacterAnimationState.OneshotAnimation;

        float elapsedTime = 0f;
        float spriteTime = 0f;
        int spriteIndex = 0;
        List<Sprite> sprites = animation.GetSprites(FacingDirection);

        if (sprites.Count == 0)
        {
            yield break;
        }

        characterRenderer.sprite = sprites[spriteIndex];

        float animationDuration = animation.GetAnimationTime(FacingDirection);
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            spriteTime += Time.deltaTime;
            if (spriteTime >= animation.animationDelay && spriteIndex < sprites.Count - 1)
            {
                //increment the index and update the sprite
                spriteIndex++;
                characterRenderer.sprite = sprites[spriteIndex];
                spriteTime = 0f;
            }

            yield return null;
        }

        animationState = CharacterAnimationState.Idle;
    }

    public IEnumerator PlayMeleeAnimation()
    {
        if (HasMeleeAnimation)
        {
            yield return PlayOneshotAnimation(meleeAnimation);
        }
    }

    public IEnumerator PlayRangedAnimation()
    {
        if (HasRangedAnimation)
        {
            yield return PlayOneshotAnimation(rangedAnimation);
        }
    }

    public IEnumerator PlayReloadAnimation()
    {
        if (HasReloadAnimation)
        {
            yield return PlayOneshotAnimation(reloadAnimation);
        }
    }
    
    public IEnumerator LoopAnimation(ObjectAnimation animation)
    {
        while (true)
        {
            yield return PlayAnimationCycle(animation);
        }
    }

    public IEnumerator PlayIdleAnimationCycle()
    {
        if (idle != null && animationState == CharacterAnimationState.Idle)
        {
            yield return PlayAnimationCycle(idle);
        }
    }

    public void ResetWalkingAnimation()
    {
        walkingFrameIndex = 0;
        walkingFrameTime = 0f;
        walkingDirection = FacingDirection;
    }

    public void UpdateWalkingAnimation(Vector2Int direction, float deltaTime)
    {
        if (!HasWalkingAnimation)
        {
            return;
        }

        Vector2Int normalizedDirection = ObjectAnimation.NormalizeDirection(direction);
        if (normalizedDirection != walkingDirection)
        {
            walkingDirection = normalizedDirection;
            walkingFrameIndex = 0;
            walkingFrameTime = 0f;
        }

        List<Sprite> sprites = walkCycleAnimation.GetSprites(walkingDirection);
        if (sprites.Count == 0)
        {
            return;
        }

        if (sprites.Count > 1 && walkCycleAnimation.animationDelay > 0f)
        {
            walkingFrameTime += deltaTime;
            while (walkingFrameTime >= walkCycleAnimation.animationDelay)
            {
                walkingFrameTime -= walkCycleAnimation.animationDelay;
                walkingFrameIndex = (walkingFrameIndex + 1) % sprites.Count;
            }
        }

        characterRenderer.sprite = sprites[walkingFrameIndex];
    }

    private IEnumerator PlayAnimationCycle(ObjectAnimation animation)
    {
        Vector2Int animationDirection = FacingDirection;
        int spriteIndex = 0;
        while (true)
        {
            List<Sprite> sprites = animation.GetSprites(animationDirection);
            if (sprites.Count == 0)
            {
                yield break;
            }

            characterRenderer.sprite = sprites[spriteIndex];
            if (sprites.Count == 1 || animation.animationDelay <= 0f)
            {
                yield return null;
                yield break;
            }

            yield return new WaitForSeconds(animation.animationDelay);

            if (FacingDirection != animationDirection)
            {
                animationDirection = FacingDirection;
                spriteIndex = 0;
                continue;
            }

            spriteIndex++;
            if (spriteIndex >= sprites.Count)
            {
                yield break;
            }
        }
    }
    
    private const float hitFlashDelay = 0.1f;
    private const int hitFlashCount = 2;
    private Coroutine hitFlashRoutine;
    private Color baseColor = Color.white;
    
    /// <summary>
    /// Plays a red hitflash for taking damage. Usually called in FieldCharacter
    /// </summary>
    /// <returns></returns>
    public void StartHitFlash()
    {
        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
        }

        hitFlashRoutine = StartCoroutine(HitFlash());
    }

    public void StopHitFlash()
    {
        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
            hitFlashRoutine = null;
        }

        if (characterRenderer != null)
        {
            characterRenderer.color = baseColor;
        }
    }

    private IEnumerator HitFlash()
    {
        for(int i = 0; i < hitFlashCount; i++)
        {
            characterRenderer.color = Color.red;
            yield return new WaitForSeconds(hitFlashDelay);
            characterRenderer.color = baseColor;
            yield return new WaitForSeconds(hitFlashDelay);
        }

        hitFlashRoutine = null;
    }
}
public enum CharacterAnimationState
{
    Idle,
    LoopingAnimation,
    OneshotAnimation
}