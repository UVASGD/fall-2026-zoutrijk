using UnityEngine;

/// <summary>
/// 
/// </summary>
public class RelativeNumbersSnippet : SnippetCondition
{
    [Header("If less than 1, will proc if the manpower ratio is below, and vice versa")]
    [SerializeField] float ratioThreshold;
    public override bool OnCauseCheck(BattleContext context)
    {
        //grab the ratio of player manpower to enemy manpower
        float relativeNumbers = (float)context.relativeStrength;
        if(relativeNumbers < 1.0f)
        {
            //check if the ratio threshold is greater, and return true if so
            if(ratioThreshold >= relativeNumbers) return true;
        }
        else //vice versa
        {
            if(ratioThreshold <= relativeNumbers) return true;
        }

        return false;
    }
}