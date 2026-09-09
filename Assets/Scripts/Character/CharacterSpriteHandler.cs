using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpriteHandler : MonoBehaviour
{
    private Sprite currentSprite;
    private SpriteRenderer characterRenderer;
    
    public CharacterAnimationState animationState;
    
    ObjectAnimation idle;
    ObjectAnimation talkingAnimation;
    ObjectAnimation walkCycleAnimation;
    ObjectAnimation meleeAnimation;
    ObjectAnimation rangedAnimation;

    //init function to be used by the FieldCharacter
    public void Setup(CombatSpriteDepot depot)
    {
        characterRenderer = GetComponent<SpriteRenderer>();

        idle = depot.Idle;
        walkCycleAnimation = depot.Walking;
        meleeAnimation = depot.MeleeBase;
        rangedAnimation = depot.RangedBase;

        animationState = CharacterAnimationState.Idle;
    }

    public IEnumerator PlayOneshotAnimation(ObjectAnimation animation)
    {
        animationState = CharacterAnimationState.OneshotAnimation;

        float elapsedTime = 0f;
        float spriteTime = 0f;
        int spriteIndex = 0;

        characterRenderer.sprite = animation.animationSprites[spriteIndex];

        while(elapsedTime < animation.animationTime)
        {
            elapsedTime += Time.deltaTime;
            spriteTime += Time.deltaTime;
            if(spriteTime > animation.animationDelay && spriteIndex < animation.animationSprites.Count)
            {
                //increment the index and update the sprite
                spriteIndex++;
                characterRenderer.sprite = animation.animationSprites[spriteIndex];
                spriteTime = 0f;
            }

            yield return null;
        }
    }
    
    //TODO: make this logic functional
    public IEnumerator LoopAnimation(ObjectAnimation animation)
    {
        yield return null;
    }
    
    private const float hitFlashDelay = 0.1f;
    private const int hitFlashCount = 2;
    
    /// <summary>
    /// Plays a red hitflash for taking damage. Usually called in FieldCharacter
    /// </summary>
    /// <returns></returns>
    public IEnumerator HitFlash()
    {
        Color originalColor = characterRenderer.color;
        
        for(int i = 0; i < hitFlashCount; i++)
        {
            characterRenderer.color = Color.red;
            yield return new WaitForSeconds(hitFlashDelay);
            characterRenderer.color = originalColor;
            yield return new WaitForSeconds(hitFlashDelay);
        }
    }
}
public enum CharacterAnimationState
{
    Idle,
    LoopingAnimation,
    OneshotAnimation
}