using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Static function that allows for an object to be sent to another scene with out any additive shennanigans. Used for sending battlemap data from campaign to battle, and v.v.
/// </summary>
public static class SceneTransfer
{
    public static GameObject PassedObject { get; private set; }

    public static void LoadSceneWithObject(string sceneName, GameObject objectToPass)
    {
        PassedObject = objectToPass;

        PassedObject.transform.SetParent(null); //unparent it
        Object.DontDestroyOnLoad(PassedObject); //send it to dontdestroyonload

        SceneManager.sceneLoaded += OnSceneLoaded; //subscribe to loading event
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single); //not additive, key point
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if(PassedObject != null)
        {
            SceneManager.MoveGameObjectToScene(PassedObject, scene);
        }
    }
}