using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UnitBase is the data container for combat units. The Unit class itself allows for functions to be called (stat calculations) that use this data.
/// </summary>
[CreateAssetMenu(menuName = "Unit/Create New Unit")]
public class UnitBase : ScriptableObject
{
    [SerializeField] string unitName;
    [SerializeField] UnitType unitType;
    [SerializeField] CombatSpriteDepot combatSprites;
    [SerializeField] Projectile rangedProjectile;

    [Header("Weapons")]
    [SerializeField] List<WeaponType> allowedPrimaryWeapons;
    [SerializeField] Weapon basePrimaryWeapon;
    [SerializeField] Weapon baseSecondaryWeapon;

    [Header("Panel View Data")]
    [TextArea(3, 5)][SerializeField] string unitDescription;
    [SerializeField] Sprite portraitSprite;

    [SerializeField] int vitality;
    [SerializeField] int morale;
    [SerializeField] int agility;
    [SerializeField] int strength;
    [SerializeField] float attackDelay = 0.5f;
    [SerializeField] float reloadTime = 0.5f;
    [Header("Combat")]
    [SerializeField] int maximumRangedRange = 5;

    [SerializeField] int unitMaintenance;
    [SerializeField] int recruitmentCost; //how much the upstart cost is to muster
    [SerializeField] int recruitmentTime; //how long a batch of this unit takes to conscript

    [Header("Base Equipment")]
    [SerializeField] UnitArmor startingArmor;
    
    
    //getters
    public string UnitName { get { return unitName; } }
    public UnitType UnitType => unitType;
    public CombatSpriteDepot CombatSprites { get { return combatSprites; } }
    public Projectile RangedProjectile => rangedProjectile;
    public string UnitDescription { get { return unitDescription; } }
    public Sprite PortraitSprite { get { return portraitSprite; } }

    public List<WeaponType> AllowedPrimaryWeapons { get { return allowedPrimaryWeapons; } }
    public Weapon BasePrimaryWeapon { get { return basePrimaryWeapon; } }
    public Weapon BaseSecondaryWeapon { get { return baseSecondaryWeapon; } }

    public int Vitality { get { return vitality; } }
    public int Morale { get { return morale; } }
    public float SurrenderChance => Mathf.Clamp01((10f - Mathf.Clamp(morale, 0, 10)) / 10f);
    public int Agility { get { return agility; } }
    public int Strength { get { return strength; } }
    public float AttackDelay { get { return attackDelay; } }
    public float ReloadTime { get { return Mathf.Max(0f, reloadTime); } }
    public int MaximumRangedRange => Mathf.Max(1, maximumRangedRange);

    public int RecruitmentCost => recruitmentCost;
    public int UnitMaintenance => unitMaintenance;
    public int RecruitmentTime => recruitmentTime; 
    public UnitArmor StartingArmor { get { return startingArmor; } }
}

public enum UnitType
{
    Melee, //only have melee available
    Ranged, //archers, crossbows, skirmishers who have a ranged option and weaker melee secondary
    [System.Obsolete("Artillery is classified as Ranged.")]
    Artillery = Ranged
}

/// <summary>
/// UnitArmor is the affinity of an armor to resist types of attacks. Plating alleviates arrows and spears, padding against blunt force, and mail against slashes. Weight is the sum of these three and reduces movement.
/// </summary>
[System.Serializable]
public class UnitArmor
{
    public int plating;
    public int padding;
    public int mail;
    public int weight => plating + padding + mail;

    public UnitArmor(int plating, int padding, int mail)
    {
        this.plating = plating;
        this.padding = padding;
        this.mail = mail;
    }
}