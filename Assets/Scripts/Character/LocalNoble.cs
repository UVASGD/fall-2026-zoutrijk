using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Noble/Create new Noble")]
public class Noble : ScriptableObject
{
    [SerializeField] private string nobleName;
    [SerializeField] private General associatedGeneral;
    [SerializeField] private NobleHouse house;
    [SerializeField] private Noble mother;
    [SerializeField] private Noble father;
    [SerializeField] private int age;
    [SerializeField] private bool male = true; //whether or not the character is male. Important for familymaking.
    [SerializeField] private Sprite portrait;

    public string NobleName { get { return nobleName; } }
    public NobleHouse House { get { return house; } }
    public Noble Mother { get { return mother; } }
    public Noble Father { get { return father; } }
    public int Age { get { return age; } }
    public bool Male { get { return male; } }
    public Sprite Portrait { get { return portrait; } }
    public General AssociatedGeneral { get { return associatedGeneral; } }
    public Noble(string nobleName, Noble mother, Noble father, NobleHouse house, int age, bool male)
    {
        this.nobleName = nobleName;
        this.mother = mother;
        this.father = father;
        this.house = house;
        this.age = age;
        this.male = male;

        this.house.AddNoble(this); //finally, add to the house.
    }
}