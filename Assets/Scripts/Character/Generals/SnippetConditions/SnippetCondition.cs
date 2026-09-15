using UnityEngine;

[System.Serializable]
public abstract class SnippetCondition
{
    [SerializeField] string conditionTitle;
    [SerializeField] bool invertLogic; //whether the logic for the snippet condition should be inverted (if it is NOT met, it is considered a pass)
    [SerializeField] bool orCause; //means that this condition passing will result in an automatic pass for the entire snippet.
    public bool InvertLogic => invertLogic;
    public bool ORCause => orCause;
    /// <summary>
    /// The abstract function that governs logic 
    /// </summary>
    /// <param name="orator"></param>
    /// <returns></returns>
    public abstract bool OnCauseCheck(BattleContext context);
}