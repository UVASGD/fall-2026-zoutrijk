using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugView : MonoBehaviour
{
    public static DebugView i;
    
    [SerializeField] GameObject debugViewParent;
    [SerializeField] TMP_Text debugText;
    private bool debugViewActive = false;

    void Awake()
    {
        if(i==null) i = this; //singleton pattern
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            ToggleDebugView();
        }
    }

    void ToggleDebugView()
    {
        debugViewActive = !debugViewActive;
        
        debugViewParent.gameObject.SetActive(debugViewActive);
    }

    public void UpdateDebugViewText(string text)
    {
        debugText.text = text;
    }
}