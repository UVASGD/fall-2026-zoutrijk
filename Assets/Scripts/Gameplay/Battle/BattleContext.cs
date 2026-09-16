/// <summary>
/// Context for a battle as used by SpeechSnippets to check against their list of conditions
/// </summary>
public class BattleContext
{
    public General playerGeneral;
    public General enemyGeneral;
    public int playerArmySize;
    public int enemyArmySize;
    public float relativeStrength => (float)playerArmySize/enemyArmySize;

    public bool playerAttacking;

    public BattleContext(General player, General enemy, int playerSize, int enemySize, bool playerAttacking)
    {
        playerGeneral = player;
        enemyGeneral = enemy;
        playerArmySize = playerSize;
        enemyArmySize = enemySize;
        this.playerAttacking = playerAttacking;
    }
}