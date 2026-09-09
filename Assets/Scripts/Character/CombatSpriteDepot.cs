using UnityEngine;
/// <summary>
/// CombatSpriteDepot stores idle, melee, ranged etc. sprite animations. Room for expansion is left in
/// </summary>
[CreateAssetMenu(menuName = "Unit/Create Combat Sprite Depot")]
public class CombatSpriteDepot : ScriptableObject
{
    //animations can be overridden by an inheritor? would make sense for unique types later
    [SerializeField] ObjectAnimation idle;
    [SerializeField] ObjectAnimation walking;
    [SerializeField] ObjectAnimation meleeBase;
    [SerializeField] ObjectAnimation rangedBase;

    public ObjectAnimation Idle {get {return idle;}}
    public ObjectAnimation Walking {get {return walking;}}
    public ObjectAnimation MeleeBase {get {return meleeBase;}}
    public ObjectAnimation RangedBase {get {return rangedBase;}}
    
}