using UnityEngine;

/// <summary>
/// Sets up a battle from a list of field armies. automatically places each type of unit in formation with itself on its half of the map.
/// </summary>
public class BattleSetup : MonoBehaviour
{
    public static BattleSetup i;
    void Awake()
    {
        if (i == null)
        {
            i = this;
        }
    }

    void Start()
    {
        BattleMapMessenger bMessenger = FindAnyObjectByType<BattleMapMessenger>(); //get the battle map messenger

        SetupBattle(bMessenger.playerArmy, bMessenger.enemyArmy);
    }

    public void SetupBattle(FieldArmy playerArmy, FieldArmy enemyArmy)
    {
        BattleManager.i.SetBattleSpeed(0); //set the battle speed to paused to start
        
        //pick a region for the player to be able to place their units

        //
    }
}