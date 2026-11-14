using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile", menuName = "Weapons/New Projectile")]
public class Projectile : ScriptableObject
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private int damage;
    [SerializeField] private int splashRadius;

    public Sprite Sprite => sprite;
    public int Damage => damage;
    public int SplashRadius => Mathf.Max(0, splashRadius);
}
