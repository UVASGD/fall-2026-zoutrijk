using UnityEngine;

/// <summary>
/// Global editor settings. Mainly contains readonly bools for viewing debug information.
/// </summary>
public class GlobalEditorSettings : MonoBehaviour
{
    public static GlobalEditorSettings i;
    
    void Awake()
    {
        if (i == null) i = this;
    }

    [Header("Debug")]
    [SerializeField] private bool richDebugLogs = true; //how detailed logging is on the debug view. Disable for cleaner experience.
    public bool RichDebugLogs => richDebugLogs;
}
