using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionSelection : MonoBehaviour
{
    public static ActionSelection i;
    [SerializeField] Button moveButton;
    [SerializeField] Button meleeButton;
    [SerializeField] Button rangedButton;
    [SerializeField] Button specialButton;

    #region init
    void Awake()
    {
        if(i==null) i = this;
    }
    void OnEnable()
    {        
        moveButton.onClick.AddListener(onMoveButtonClicked);
        meleeButton.onClick.AddListener(onMeleeButtonClicked);
        rangedButton.onClick.AddListener(onRangedButtonClicked);
        specialButton.onClick.AddListener(onSpecialButtonClicked);
    }
    void OnDisable()
    {
        
        moveButton.onClick.RemoveListener(onMoveButtonClicked);
        meleeButton.onClick.RemoveListener(onMeleeButtonClicked);
        rangedButton.onClick.RemoveListener(onRangedButtonClicked);
        specialButton.onClick.RemoveListener(onSpecialButtonClicked);
    }
    #endregion
    void onMoveButtonClicked()
    {
        
    }
    void onMeleeButtonClicked()
    {
        
    }
    void onRangedButtonClicked()
    {
        
    }
    void onSpecialButtonClicked()
    {
        
    }
}