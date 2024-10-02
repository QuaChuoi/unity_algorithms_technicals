using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/ListObstacleVariable")]
public class ListObstacleVariable : ScriptableObject
{
    public List<ObstacleObj> obstacleObjs = new List<ObstacleObj>();
}
