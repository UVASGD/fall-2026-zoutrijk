using System.Collections;

/// <summary>
/// an ObjectHP interface is applied to all MapObjects that also are impacted by taking damage.
/// </summary>
public interface ObjectHP : MapObject
{
    /// <summary>
    /// May cause a reduction in health, destruction with an animation, etc.
    /// </summary>
    public IEnumerator TakeDamage(int taken, DamageType damageType = DamageType.Blunt);
}