using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldObjectPreviewUI : MonoBehaviour
{
    public static WorldObjectPreviewUI i;

    [SerializeField] GameObject graphicsParent;
    [SerializeField] Image portraitRenderer;
    [SerializeField] TMP_Text headerText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] Image hpBarFill; //the red fill of the info bar HP fill
    [SerializeField] TMP_Text healthText;
    [SerializeField] Button closeButton; //X button that closes the menu

    void Awake()
    {
        if(i==null)
            i = this;
        else
            Destroy(this.gameObject);
    }

    void OnEnable()
    {
        closeButton.onClick.AddListener(HideMenu); //initializes the close button to hide the menu onClick
    }

    void OnDisable()
    {
        closeButton.onClick.RemoveAllListeners();
    }

    private MapObject currentObject; //the current object being displayed
    public void OpenMenu(MapObject mapObject)
    {
        currentObject = mapObject;

        RefreshPreviewMenu();

        graphicsParent.gameObject.SetActive(true);
    }

    /// <summary>
    /// Refreshes the menu based on the currently displayed object
    /// </summary>
    private void RefreshPreviewMenu()
    {
        WorldObjectPreviewData data = currentObject.ExposeObjectInfo(); //assigns all datatypes without constructing a new class or multiple getters

        portraitRenderer.sprite = data.portrait;
        descriptionText.text = data.description;

        hpBarFill.rectTransform.localScale = new Vector3(data.healthPercentage, 1, 1); //set the fill to match the current HP value
        healthText.text = $"{(int)(data.healthPercentage * data.maxHealth)}/{data.maxHealth} HP"; //set the text value of the health text

    }

    public void UpdateCurrentObject(MapObject newObject)
    {
        currentObject = newObject;
        RefreshPreviewMenu();
    }

    public void HideMenu()
    {
        graphicsParent.gameObject.SetActive(false);
    }
}

/// <summary>
/// a class that is passed to populate the UI for the worldObjectPreviewData.
/// </summary>
public class WorldObjectPreviewData
{
    public string objectName;
    public Sprite portrait;
    public string description;
    public float healthPercentage;
    public int maxHealth;
    public WorldObjectPreviewData(string objectName, Sprite portrait, string description, float healthPercentage, int maxHealth)
    {
        this.objectName = objectName;
        this.portrait = portrait;
        this.description = description;
        this.healthPercentage = healthPercentage;
        this.maxHealth = maxHealth;
    }
}