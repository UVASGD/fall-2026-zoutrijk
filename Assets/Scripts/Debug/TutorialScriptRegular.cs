using UnityEngine;

[System.Serializable]
public class TutorialScriptRegular
{
    [SerializeField] private int var1;
    [SerializeField] private string var2;

    public TutorialScriptRegular(int var1, string var2)
    {
        this.var1 = var1;
        this.var2 = var2;
    }
}