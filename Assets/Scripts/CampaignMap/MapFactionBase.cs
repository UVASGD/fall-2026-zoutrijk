using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Faction/New Faction Base")]
public class MapFactionBase : ScriptableObject
{
    [SerializeField] string factionTag;
    [SerializeField] string factionName;
    [SerializeField] string factionAdjective;
    [SerializeField] Sprite factionBanner;
    [SerializeField] List<string> startingRegions;
    [SerializeField] Color mapColor;
    [SerializeField] Color secondaryColor;
    [SerializeField] List<CultureType> acceptedCultures;

    //getters
    public string FactionTag => factionTag;
    public string FactionName => factionName;
    public string FactionAdjective => factionAdjective;
    public Sprite FactionBanner => factionBanner;
    public List<string> StartingRegions => startingRegions;
    public Color MapColor => mapColor;
    public Color SecondaryColor => secondaryColor;
    public List<CultureType> AcceptedCultures => acceptedCultures;
}
public enum CultureType
{
    Dolebian,
    Livernic,
    Opterian,
    Saffrontyr
}