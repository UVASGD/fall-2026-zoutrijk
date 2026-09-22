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

    /// <summary>
    /// add noble to noble house
    /// </summary>
    /// <param name="noble">the noble to add</param>
    public void AddNoble(Noble noble)
    {
        characters.Add(noble);
    }

    /// <summary>
    /// remove noble from noble house
    /// </summary>
    /// <param name="noble">the noble to remove</param>
    /// <returns>removal success</returns>
    public bool RemoveNoble(Noble noble)
    {
        bool ret = false;
        if(noble.Equals(this.headOfHouse))
        {
            RemoveHeadOfHouse();
            ret = true;
        }
        else if(characters.Contains(noble))
        {
            characters.Remove(noble);
            ret = true;
        }
        // edge case: no nobles left
        if(this.characters.Count == 0)
        {
            // TODO: function to destroy faction
        }
        return ret;
    }

    public Noble RemoveHeadOfHouse()
    {
        // remove head of house from normal noble list
        // doesn't matter if they were there or not
        if(characters.Contains(this.headOfHouse))
        {
            characters.Remove(this.headOfHouse);
        }
        // edge case: no nobles left
        if(this.characters.Count == 0)
        {
            // TODO: function to destroy faction (same as above)
            return null;
        }
        // find and implement successor
        Noble successor = characters[0];
        foreach(var character in characters)
        {
            // TODO: decide how to find successor
            // currently just finds first oldest male
            if(character.Male && character.Age > successor.Age)
            {
                successor = character;
            }
        }
        this.headOfHouse = successor;
        return this.headOfHouse;
    }
}