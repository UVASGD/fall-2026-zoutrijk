using UnityEngine;

/// <summary>
/// Static class that contains various utility functions for use elsewhere.
/// </summary>
public static class ZUtilities
{
    public static void DestroyAllChildren(GameObject gameObject)
    {
        foreach (Transform child in gameObject.transform)
        {
            Object.Destroy(child.gameObject);
        }
    }
}