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

    public BattleContext(General player, General enemy, int playerSize, int enemySize)
    {
        playerGeneral = player;
        enemyGeneral = enemy;
        playerArmySize = playerSize;
        enemyArmySize = enemySize;
    }
}