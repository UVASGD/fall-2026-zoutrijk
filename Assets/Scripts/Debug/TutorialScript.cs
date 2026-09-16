using UnityEngine;

public class TutorialScript : MonoBehaviour
{
    [SerializeField] private RectTransform testObject;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] TutorialScriptRegular tutorialScriptRegular;
    void Update()
    {
        MoveObject();
    }

    private void MoveObject()
    {
        testObject.position += new Vector3(moveSpeed * Time.deltaTime, 0f, 0f);
    }
}