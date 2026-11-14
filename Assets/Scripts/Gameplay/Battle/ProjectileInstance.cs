using System.Collections;
using System.Linq;
using UnityEngine;

public class ProjectileInstance : MonoBehaviour
{
    private FieldCharacter target;
    private DamageType damageType;
    private int damage;
    private int splashRadius;
    private bool attackerPlayerControlled;
    private float travelDuration;

    public void Initialize(
        Projectile projectile,
        FieldCharacter target,
        DamageType damageType,
        bool attackerPlayerControlled,
        float travelDuration)
    {
        this.target = target;
        this.damageType = damageType;
        damage = projectile.Damage;
        splashRadius = projectile.SplashRadius;
        this.attackerPlayerControlled = attackerPlayerControlled;
        this.travelDuration = Mathf.Max(0.01f, travelDuration);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = projectile.Sprite;
        StartCoroutine(TravelToTarget());
    }

    private IEnumerator TravelToTarget()
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < travelDuration)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                yield break;
            }

            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(
                startPosition,
                target.transform.position,
                Mathf.Clamp01(elapsed / travelDuration));
            yield return null;
        }

        if (target != null && target.gameObject.activeInHierarchy)
        {
            foreach (FieldCharacter character in FindObjectsByType<FieldCharacter>()
                .Where(character => character != null
                    && character.gameObject.activeInHierarchy
                    && character.TilePosition != null
                    && (character == target || character.PlayerControlled != attackerPlayerControlled)
                    && target.GetTileDistanceTo(character) <= splashRadius))
            {
                character.TakeDamage(damage, damageType);
            }
        }

        Destroy(gameObject);
    }
}
