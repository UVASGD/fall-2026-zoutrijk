using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/Create New Tutorial SO")]
public class TutorialSO : ScriptableObject
{
    [SerializeField] List<int> numbers;
    public List<int> Numbers { get { return numbers; } }
}