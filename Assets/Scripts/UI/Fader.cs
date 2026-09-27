using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Used to fade in/out a transition image
/// </summary>
public class Fader : MonoBehaviour
{
    public static Fader i { get; private set; }
    Image image;
    private void Awake()
    {
        i = this;
        image = GetComponent<Image>();
    }

    /// <summary>
    /// Fades in or out an image that covers the entire canvas. Make sure the image is NOT a raycast target, otherwise UI won't work properly.
    /// </summary>
    /// <param name="lerpTime"></param>
    /// <param name="fadeIn"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public IEnumerator FadeImage(float lerpTime, bool fadeIn, Color color, bool lerpColor = false)
    {
        float startingAlpha = image.color.a;

        if(!lerpColor) image.color = new Color(color.a, color.g, color.b); //set the color of the image (not alpha) immediately instead of lerping to it

        float elapsed = 0f;

        float targetAlpha;
        if(fadeIn) targetAlpha = 1f;
        else targetAlpha = 0f; //for fadeout

        while(elapsed < lerpTime)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / lerpTime;
            float newAlpha = Mathf.Lerp(startingAlpha, targetAlpha, percent);
            
            image.color = new Color(color.r, color.g, color.b, newAlpha);

            yield return null;
        }

        yield break;
    }
}
