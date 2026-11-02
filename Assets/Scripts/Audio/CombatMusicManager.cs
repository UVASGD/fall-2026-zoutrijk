using UnityEngine;

public class CombatMusicManager : MonoBehaviour
{
    public static CombatMusicManager i;
    void Awake()
    {
        if (i == null) i = this;
    }

    [SerializeField] AudioClip deploymentTheme;
    [SerializeField] AudioClip preClash;

    /// <summary>
    /// Plays the deployment theme
    /// </summary>
    public void PlayDeploymentTheme()
    {
        AudioManager.Instance.PlayMusic(deploymentTheme);
    }

    public void PlayTacticalTheme()
    {
        AudioManager.Instance.PlayMusic(preClash);
    }
}