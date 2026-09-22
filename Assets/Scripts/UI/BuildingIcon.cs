using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// The building icon visible in the CityPanel, with a hover component
/// </summary>
public class BuildingIcon : MonoBehaviour
{
    public Image iconImage; //fields are public because they are instantiated often, so containment isn't worth the hassle
    public TextHoverTrigger textHoverTrigger;
}