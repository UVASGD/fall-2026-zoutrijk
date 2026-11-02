using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Handles text popups in the top left of the screen during battle, notifying the player about events transpiring.
/// Similar logic may be used for the general speech text popup
/// </summary>
public class BattlefieldAnnouncer : MonoBehaviour
{
    public static BattlefieldAnnouncer i;
    public static BattlefieldAnnouncer generalSpeechBubble; //I call this a doubleton (crap architecture but terse coding)
    [SerializeField] bool cornerAnnouncer = false; //whether or not this is the main UI announcer in the corner (general's speeches use a separate one)
    [SerializeField] bool isGeneralSpeechBubble = false;
    public event Action<string> onBattleFieldMessage;
    [SerializeField] Transform textParent; //should have a horizontal layout group for the text objects
    [SerializeField] GameObject textPrefab; //white announcement text for whatever message, must have a TMPro text comp

    private float textLagTime = 2f; //time where the text is fully visible before fading away
    private float textFadeawayTime = 1f; //time where the text fades away

    void Awake()
    {
        if(i==null && cornerAnnouncer) i = this;
        else if(isGeneralSpeechBubble) generalSpeechBubble = this;
    }

    void OnEnable()
    {
        onBattleFieldMessage += AnnounceBattlefieldMessage;
    }

    void OnDisable()
    {
        onBattleFieldMessage -= AnnounceBattlefieldMessage;
    }

    private void AnnounceBattlefieldMessage(string message)
    {
        TMP_Text newText = Instantiate(textPrefab, textParent).GetComponent<TMP_Text>();

        newText.text = message;
        
        StartCoroutine(StartTextPopupCountdown(newText));
    }

    private IEnumerator StartTextPopupCountdown(TMP_Text tmpText)
    {
        yield return new WaitForSeconds(textLagTime);
        float elapsedTime = 0f;
        float percentage;
        while(elapsedTime < textFadeawayTime)
        {
            elapsedTime += Time.deltaTime;
            percentage = elapsedTime / textFadeawayTime;
            tmpText.alpha = 1 - percentage; //so it disappears linearly

            yield return null;
        }
    

        Destroy(tmpText.gameObject); //finally destroy once the fadeout is complete    
        yield return null;
    }

    public void TestBattlefieldAnnouncement()
    {
        onBattleFieldMessage?.Invoke("This is a test announcement");
    }

    public void BattlefieldAnnouncement(string announcement)
    {
        onBattleFieldMessage?.Invoke(announcement);
    }
}