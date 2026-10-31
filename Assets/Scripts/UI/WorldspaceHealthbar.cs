using UnityEngine;

public class WorldspaceHealthbar : MonoBehaviour
{
    private const float baseFillWidth = 45f;
    [HideInInspector] public bool barPrompted = false;
    [SerializeField] GameObject graphicsParent;
    [SerializeField] RectTransform fillImage;
    float verticalOffset = 35f; //the vertical offset of this bar off the fieldcharacter.
    
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
    /// Called on unit death to remove a healthbar from the screen.
    /// </summary>
    public void CancelHealthbar()
    {
        graphicsParent.SetActive(false);
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
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldposition);
        gameObject.GetComponent<RectTransform>().position = screenPosition + Vector3.up * verticalOffset;
    }
}