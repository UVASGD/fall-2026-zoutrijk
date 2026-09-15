using UnityEngine;
/// <summary>
/// Gives this building a chance to give all stationed generals points towards a particular trait.
/// </summary>
[System.Serializable]
public class TraitChanceEffect : GenericTurnEndEffect
{
    [SerializeField] string traitKey; //the key of the trait so it can be found in the TraitDB that is NOT created yet as of 9/15/26
    [SerializeField] int incrementPoints; //how many points are gained per proc
    [SerializeField] float procChance; //the chance (out of 1) that a trait point will proc for each general.
    public override void OnEndTurnEffect(MapCity ownerCity)
    {
        Debug.LogWarning($"Functionality for TraitChanceEffect not implemented as of 9/15/26");
    }
}
