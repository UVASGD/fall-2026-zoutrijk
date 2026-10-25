using System;
using UnityEditor;
[CustomEditor(typeof(CityBuilding))]
public class CityBuildingEditor : ScriptableObjectButtonEditor<CityBuilding>
{
    protected override string HeaderText => "City Building Editor";

    protected override string[] ButtonLabels => new string[]
    {
        "Passive Income Effect",
        "Trait Chance Impact",
        "Unlock New Construction",
        "Compound Investment"
    };

    protected override Action<CityBuilding>[] ButtonActions => new Action<CityBuilding>[]
    {
        (b) => b.AddOnTurnEndCause(new PassiveIncomeEffect()),
        (b) => b.AddOnTurnEndCause(new TraitChanceEffect()),
        (b) => b.AddOnTurnEndCause(new UnlockBuildingConstructionEffect()),
        (b) => b.AddOnTurnEndCause(new CompoundInterestEffect())
    };
}