using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ObjectAnimation stores a list of sprites and its delay between frames (in seconds), as well as a calculated value for the entire animation's timing
/// </summary>
[System.Serializable]
public class ObjectAnimation
{
    //this class is a storage container for a sprite animation. It has a list of sprites and the delay between each animation, in seconds.
    [SerializeField] private List<Sprite> _animationSprites = new List<Sprite>();
    [SerializeField] private float _animationDelay;

    public List<Sprite> animationSprites { get { return _animationSprites; } }
    public float animationDelay { get { return _animationDelay; } }
    public float animationTime => animationDelay * animationSprites.Count; //returns the total time for the animation
}