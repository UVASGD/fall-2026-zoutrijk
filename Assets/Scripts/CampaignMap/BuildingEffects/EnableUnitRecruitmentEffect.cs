using System.Collections.Generic;
using UnityEngine;

public class EnableUnitRecruitmentEffect : GenericTurnEndEffect
{
    [SerializeField] List<UnitRecruitmentCapability> allowedUnitRecruitment;

    public override void OnEndTurnEffect(MapCity ownerCity)
    {
        throw new System.NotImplementedException();
    }
}

/// <summary>
/// The recruit limit and unitbase for unit recruitment
/// </summary>
[System.Serializable]
public struct UnitRecruitmentCapability
{
    [SerializeField] UnitBase unitBase;
    [SerializeField] int recruitmentCap; //how many can be recruited per turn
}