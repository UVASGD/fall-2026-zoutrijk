using System.Collections;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// A prefab monobehavior for a destructible object. May inhibit movement. Has a late script execution order to prevent race condition with start on mapmanager.
/// </summary>
public class DestructibleObject : MonoBehaviour, ObjectHP
{
    private SpriteRenderer spriteRenderer => gameObject.GetComponent<SpriteRenderer>();
    [SerializeField] string objectName;
    [SerializeField] string objectDescription;
    [SerializeField] int maxHealth;
    private int currentHealth;
    [SerializeField] SpriteEffect destructionEffect;
    [SerializeField] Vector2 startingPosition; //TODO: remove this field when proc gen is added
    [SerializeField] Vector2 positionalOffset;
    [SerializeField] bool canWalkThrough; //if an object can be walked through in pathfinding
    private OverlayTile tilePosition;
    void Start()
    {
        currentHealth = maxHealth;
        PlaceObjectOnGrid(startingPosition);
    }
    public void onLeftClick()
    {
        StartCoroutine(TakeDamage(20, DamageType.Blunt));
    }
    public IEnumerator TakeDamage(int taken, DamageType damageType)
    {
        currentHealth = math.max(0, currentHealth - taken);

        if (currentHealth == 0)
        {
            Debug.Log("Destructible object destroyed");
            //cause the destruction effect to take place on this sprite only
            yield return destructionEffect.RunSpriteEffect(spriteRenderer);

            //destroy the object
            Destroy(this.gameObject);
        }
    }
    private void PlaceObjectOnGrid(Vector2 tilePosition)
    {
        if (!MapManager.i.TryGetOverlayTile(tilePosition, out OverlayTile currentTile))
        {
            Debug.LogError($"No overlay tile found at tile position {tilePosition}.");
            return;
        }

        this.tilePosition = currentTile;

        if (currentTile != null)
            currentTile.SetRestingObject(this); //set the current tile as its resting object

        transform.position = (Vector2)currentTile.transform.position + positionalOffset;
    }

    public bool AllowPassthrough(FieldCharacter passing)
    {
        return canWalkThrough;
    }

    public WorldObjectPreviewData ExposeObjectInfo()
    {
        return new WorldObjectPreviewData(objectName, spriteRenderer.sprite, objectDescription, (float)currentHealth / (float)maxHealth, maxHealth);
    }
}