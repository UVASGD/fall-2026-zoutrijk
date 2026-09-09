using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] List<AudioData> sfxList;
    [SerializeField] AudioSource sfxPlayer;
    [SerializeField] AudioSource musicPlayer;
    Dictionary<AudioId, AudioData> sfxLookup;
    AudioClip currentMusic;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    void Start()
    {
        sfxLookup = sfxList.ToDictionary(x => x.id);
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;

        sfxPlayer.PlayOneShot(clip); //Doesn't cancel other sound running async
    }
    public void PlaySfx(AudioId audioID)
    {
        if (!sfxLookup.ContainsKey(audioID)) return; //to prevent a possible crash

        var audioData = sfxLookup[audioID];

        PlaySfx(audioData.clip);
    }

    public void PlayMusic(AudioClip clip, bool loop = true) //this music player function originally had support for fadein, but that required the DG.Tweening library which this project does not currently utilize.
    {
        if (clip == null || clip == currentMusic) return;

        currentMusic = clip;

        musicPlayer.clip = clip;
        musicPlayer.loop = loop;

        musicPlayer.Play();
    }
}

public enum AudioId
{
    //put audioIDs here
    testBlip,
    playerPartySelect,
    enemyCharacterSelect
}

[System.Serializable]
public class AudioData
{
    public AudioId id;
    public AudioClip clip;
}