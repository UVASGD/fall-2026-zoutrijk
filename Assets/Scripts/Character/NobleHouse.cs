using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Noble/Create new Noble House")]
public class NobleHouse : ScriptableObject
{
    [SerializeField] private string houseName;
    private Region homeland; //the home region for this house.
    [SerializeField] private List<Noble> characters;
    [SerializeField] private Noble headOfHouse;
    public List<Noble> Characters { get { return characters; } }
    public Noble HeadOfHouse { get { return headOfHouse; } }
    public string HouseName { get { return houseName; } }
    public Region Homeland { get { return homeland; } }

    public void AddNoble(Noble noble)
    {
        characters.Add(noble);
    }
}