using System;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(CityBuilding))]
public class CityBuildingEditor : ScriptableObjectButtonEditor<CityBuilding>
{
    protected override string HeaderText => "City Building Editor";

    protected override string[] ButtonLabels => new string[]
    {
        "Passive Income Effect",
        "Trait Chance Impact"
    };

    protected override Action<CityBuilding>[] ButtonActions => new Action<CityBuilding>[]
    {
        (b) => b.AddOnTurnEndCause(new PassiveIncomeEffect()),
        (b) => b.AddOnTurnEndCause(new TraitChanceEffect())
    };
}