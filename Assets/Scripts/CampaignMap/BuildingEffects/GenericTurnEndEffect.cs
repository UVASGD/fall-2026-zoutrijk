using UnityEngine;
/// <summary>
/// An abstract class that is inherited by custom turnEndEffect children with their own logic.
/// </summary>
[System.Serializable]
public abstract class GenericTurnEndEffect
{
    /// <summary>
    /// Crucially, effectNames are used in the building UI to describe the effects of a building. Duplicate and blank effectNames are not recognized, however,
    /// </summary>
    [TextArea(1,3)] [SerializeField] string effectName;

    public string EffectName => effectName;

    /// <summary>
    /// Abstract function that is overwritten by each subsequent TurnEndEffect. Takes the ownerCity automatically as a means to 
    /// </summary>
    /// <param name="ownerCity"></param>
    public abstract void OnEndTurnEffect(MapCity ownerCity);
}