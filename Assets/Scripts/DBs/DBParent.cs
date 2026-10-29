using System.Collections.Generic;
using UnityEngine;

public class DBParent : MonoBehaviour
{
    void Awake()
    {
        UnitDB.Init();
        BuildingDB.Init();
        TraitDB.Init();
    }
}