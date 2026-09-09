using System.Collections;
using UnityEngine;

/// <summary>
/// Scriptable object with an attached material + shader that has a set duration. Note: not animation-based, but shader-based. Ensure all shaders have a parameter named "duration" to function properly.
/// </summary>
[CreateAssetMenu(menuName = "Effect/Create new Sprite Effect")]
public class SpriteEffect : ScriptableObject
{
    private static readonly int DurationPropertyId = Shader.PropertyToID("_duration");

    [SerializeField] Material effectMaterial;
    [SerializeField] float duration;

    public Material EffectMaterial { get { return effectMaterial; } }
    public float Duration { get { return duration; } }

    public IEnumerator RunSpriteEffect(SpriteRenderer renderer)
    {
        Material instanceMaterial = Instantiate(effectMaterial);

        renderer.material = instanceMaterial;

        float elapsed = 0f;
        instanceMaterial.SetFloat(DurationPropertyId, 0f); //set to 0 to start
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            instanceMaterial.SetFloat(DurationPropertyId, elapsed / duration); //sets the duration to the percentage of the effect's completion.
            yield return null;
        }

        yield break;
    }
}