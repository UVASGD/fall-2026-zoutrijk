using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main controller for the flow of combat states. Make sure to outsource as much functionality as possible to prevent a script 2000+ lines long.
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager i;
    public BattleState battleState { get; private set; }
    private Stack<BattleState> currentStates;
    public int battleSpeed {get; private set;} //can either be 0 (paused), 1 (normal), or 2 (sped up)
    public float TimeScaleMultiplier => battleSpeed <= 0 ? 0f : battleSpeed;
    void Awake()
    {
        if (i == null) i = this;
        MouseController.i.updateBattleState += UpdateBattleState;
        SpeedButton.onSpeedButtonClicked += SetBattleSpeed;
        currentStates = new Stack<BattleState>();

        battleSpeed = 1;
    }

    void OnDestroy()
    {
        SpeedButton.onSpeedButtonClicked -= SetBattleSpeed;
    }

    void Update()
    {
        MouseController.i.HandleUpdate(battleState);
    }

    public void SetBattleSpeed(int speed)
    {
        if(GlobalEditorSettings.i.RichDebugLogs) Debug.Log($"Battle speed set to {speed}");
        battleSpeed = Mathf.Max(0, speed);
    }

    /// <summary>
    /// Setter for battle state. Not to be called directly (to avoid a circular dependency). Invoke an event instead.
    /// </summary>
    /// <param name="state"></param>
    private void UpdateBattleState(BattleState state)
    {
        currentStates.Push(state);
        battleState = currentStates.Peek();
    }

    /// <summary>
    /// Performs an attack from one unit towards any object that can take damage (the ObjectHP interface)
    /// </summary>
    /// <param name="attacker">The unit performing the attack.</param>
    /// <param name="receiver">The unit receiving the attack.</param>
    /// <param name="attackType">The type of attack being performed.</param>
    public void PerformAttack(FieldCharacter attacker, ObjectHP receiver, AttackType attackType)
    {
        if(attackType == AttackType.Melee)
        {
            int attackerDamage = attacker.Unit.strength + attacker.UnitWeapon.BaseDamage;
            receiver.TakeDamage(attackerDamage, attacker.UnitWeapon.DamageType);

        }
        else if(attackType == AttackType.Ranged)
        {
            
        }
    }

    /// <summary>
    /// Pops the current BattleState and restores the one before, allowing and undoing of the last action in battle.
    /// </summary>
    public void PopBattleState()
    {
        currentStates.Pop();
        battleState = currentStates.Peek(); //peek the top value of the stack.
    }

    /// <summary>
    /// Clears the stack of BattleStates, due to a new turn or irreversible action.
    /// </summary>
    public void ClearBattleStates()
    {
        currentStates.Clear();
    }
}

public enum BattleState
{
    SelectUnit,
    UnitSelected,
    CheckingLOS //checking ranged line of sight
}
/// <summary>
/// Whether or not an attack from a Unit is melee or ranged.
/// </summary>
public enum AttackType
{
    Melee,
    Ranged,
    None //placeholder for when the alt attack type for a unit is no different from the regular attack type.
    //For example, archers' alt is a weak melee. Swordsmen do not have an alt, so theirs would be null.
}