using UnityEngine;

public class WorldspaceHealthbar : MonoBehaviour
{
    private const float baseFillWidth = 90f;
    [HideInInspector] public bool barPrompted = false;
    [SerializeField] GameObject graphicsParent;
    [SerializeField] RectTransform fillImage;
    float verticalOffset = 50f; //the vertical offset of this bar off the fieldcharacter.
    
    /// <summary>
    /// Called the first time damage is taken to make the bar visible (to reduce clutter on undamaged units)
    /// </summary>
    /// <param name="newPct"></param>
    public void PromptHealthbar()
    {
        graphicsParent.SetActive(true);
        barPrompted = true;
    }

    /// <summary>
    /// called in when damage is taken
    /// </summary>
    /// <param name="newPct"></param>
    public void UpdateBarFill(float newPct)
    {
        fillImage.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, baseFillWidth * newPct);
    }

    /// <summary>
    /// Takes the world position of an object and moves 
    /// </summary>
    /// <param name="worldposition"></param>
    public void UpdateBarPosition(Vector2 worldposition)
    {
        Vector2 screenPos = Camera.main.ScreenToWorldPoint(worldposition);
        gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(screenPos.x, screenPos.y + verticalOffset);
    }
}