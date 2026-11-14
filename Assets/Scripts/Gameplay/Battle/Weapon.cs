using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/New Weapon")]
public class Weapon : ScriptableObject
{
    [SerializeField] private string weaponName;
    [SerializeField] private Sprite weaponIcon;
    [SerializeField] private WeaponType type;
    [SerializeField] private DamageType damageType;
    [SerializeField] private int baseDamage;
    [SerializeField] private int range = 1; //melee weapons default to a range of 1. Spears may go up to 2.

    public string WeaponName {get {return weaponName;}}
    public Sprite WeaponIcon {get {return weaponIcon;}}
    public WeaponType Type {get {return type;}}
    public DamageType DamageType {get {return damageType;}}
    public int BaseDamage {get {return baseDamage;}}
    public int Range {get {return range;}}
}

public enum WeaponType
{
    Axe,
    Sword,
    Spear,
    Bow,
    Crossbow,
}

public enum DamageType
{
    Slash,
    Pierce,
    Blunt
}