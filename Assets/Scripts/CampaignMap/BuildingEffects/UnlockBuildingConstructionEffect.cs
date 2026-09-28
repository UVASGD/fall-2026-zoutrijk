using System.Collections.Generic;
using UnityEngine;

public class UnlockBuildingConstructionEffect : GenericTurnEndEffect
{
    [SerializeField] List<CityBuilding> unlockedBuildings;
    public override void OnEndTurnEffect(MapCity ownerCity)
    {
        foreach (var building in unlockedBuildings)
        {
            if (!ownerCity.availableToBuildBuildings.Contains(building)) //if does not already recognize this building
            {
                ownerCity.availableToBuildBuildings.Add(building);
            }
        }

    }
}
