using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Code taken from another one of my projects, creates a generic DB for a scriptable object type using a script that inherits. Very modular.
public class ScriptableObjectDB<T> : MonoBehaviour where T : ScriptableObject //Locks the input to a ScriptableObject
{
    static Dictionary<string, T> objects;
    public static void Init()
    {
        objects = new Dictionary<string, T>();

        var objectArray = Resources.LoadAll<T>("");
        foreach (var obj in objectArray)
        {
            if (objects.ContainsKey(obj.name))
            {
                Debug.LogError($"There are two objects with the name {obj.name}");
                continue;
            }
            objects[obj.name] = obj;
        }
    }
    public static T GetObjectByName(string name)
    {
        if (!objects.ContainsKey(name))
        {
            Debug.LogError($"object with the name {name} not found in database");
            return null;
        }
        return objects[name];
    }
    public static List<T> GetAllObjects()
    {
        return objects.Values.ToList();
    }
}