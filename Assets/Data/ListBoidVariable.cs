using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/List GameObject Variable")]
public class ListBoidVariable : ScriptableObject
{
    public List<Transform> boidTranform = new List<Transform>();
}
