using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Static class that contains various utility functions and constants for use elsewhere in this project.
/// </summary>
public static class ZUtilities
{
    #region fields
    public const float BASE_MOVEMENT_RANGE = 30f;
    public const float DISCIPLINE_MOVEMENT_RANGE_MULTIPLIER = 0.05f;
    #endregion fields

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

    /// <summary>
    /// Reuseable 2D lerper that attaches a coroutine to the runner monobehavior. TLDR: make sure the mono calling the coroutine does NOT get disabled until after the lerp should be done.
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="toMove"></param>
    /// <param name="targetPos"></param>
    /// <param name="lerpTime"></param>
    public static void Generic2DLerp(this MonoBehaviour runner, GameObject toMove, Vector2 targetPos, float lerpTime)
    {
        runner.StartCoroutine(LerpHelper(toMove, targetPos, lerpTime));
    }

    /// <summary>
    /// Helper function for lerper that is attached to the runner 
    /// </summary>
    /// <param name="moving"></param>
    /// <param name="lerpTime"></param>
    /// <param name="runner"></param>
    /// <returns></returns>
    private static IEnumerator LerpHelper(GameObject moving, Vector2 targetPos, float lerpTime)
    {
        float elapsed = 0f;
        Vector2 startingPos = moving.transform.position;

        while(elapsed < lerpTime)
        {
            elapsed += Time.deltaTime;
            float percentage = elapsed / lerpTime;

            moving.transform.position = Vector2.Lerp(startingPos, targetPos, percentage);

            yield return null;
        }

        yield return null;
    }
}