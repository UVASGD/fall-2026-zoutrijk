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
    [SerializeField] private bool richDebugLogs = true;
    /// <summary>
    ///How detailed logging is on the debug view. Disable for cleaner dev console while playtesting.
    /// </summary>
    public bool RichDebugLogs => richDebugLogs;
}
