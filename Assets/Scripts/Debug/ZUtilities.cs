using System;
using UnityEngine;

/// <summary>
/// Static class that contains various utility functions for use elsewhere in this project.
/// </summary>
public static class ZUtilities
{
    /// <summary>
    /// Destroys all children of a GameObject. Use responsibly. Cannot lead to data loss.
    /// </summary>
    /// <param name="gameObject"></param>
    public static void DestroyAllChildren(GameObject gameObject)
    {
        foreach (Transform child in gameObject.transform)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Returns a fresh copy of an object that is not tied to the original. Use when modifying ScriptableObjects that you don't want to overwrite the disc versions of.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static object DeepClone(object source)
    {
        if (source == null) return null;
        try
        {
            var type = source.GetType();
            string json = JsonUtility.ToJson(source);
            var clone = Activator.CreateInstance(type);
            JsonUtility.FromJsonOverwrite(json, clone);
            return clone;
        }
        catch (Exception)
        {
            return null;
        }
    }
}