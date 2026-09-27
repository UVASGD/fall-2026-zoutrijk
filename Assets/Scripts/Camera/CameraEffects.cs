using System.Collections;
using UnityEngine;

/// <summary>
/// Important camera effect functions are contained, such as lerping, timed zooming, etc. Should be useable in both the campaign and battle cameras.
/// </summary>
public class CameraEffects : MonoBehaviour
{
    [SerializeField] AnimationCurve zoomTimingCurve; //used to make the zoom look cool
    [SerializeField] Camera camera;
    /// <summary>
    /// Smoothly interpolates the camera to another position
    /// </summary>
    /// <param name="targetPos"></param>
    public void LerpCamera(Vector2 targetPos, float lerpTime)
    {
        ZUtilities.Generic2DLerp(this, gameObject, targetPos, lerpTime, zoomTimingCurve); //using ZUtilities generic lerper, of course
    }

    /// <summary>
    /// Instantly snaps the camera to a target position
    /// </summary>
    /// <param name="targetPos"></param>
    public void FocusCamera(Vector2 targetPos)
    {
        this.transform.position = targetPos;
    }

    /// <summary>
    /// Zooms the camera in to a specified position
    /// </summary>
    /// <param name="targetSize"></param>
    /// <param name="useZoomCurve"></param>
    public IEnumerator ZoomCamera(float targetSize, float lerpTime, bool useZoomCurve = true)
    {
        //linearly interpolate the size of the camera until it is the target size.
        float elapsed = 0f;
        float startingSize = camera.orthographicSize;

        while(elapsed <= lerpTime)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed/lerpTime;
            //if statement for if useZoomCurve is relevant
            if (useZoomCurve)
                camera.orthographicSize = Mathf.Lerp(startingSize, targetSize, zoomTimingCurve.Evaluate(percent));
            else
                camera.orthographicSize = Mathf.Lerp(startingSize, targetSize, percent);

            yield return null;
        }

        yield break; //break just in case
    }
}