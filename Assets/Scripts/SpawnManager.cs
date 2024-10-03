using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private ListBoidVariable boids;
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private float boidCount;

    [SerializeField] private ListObstacleVariable obstacles;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private float obstacleCount;

    private void Start()
    {
        if (boids.boidMovements.Count > 0) boids.boidMovements.Clear(); 
        for (int i = 0; i < boidCount; i++)
        {
            RandomInstantiate<BoidMovement>(boidPrefab, boids.boidMovements, new Vector2(15f, 10f));
        }

        if(obstacles.obstacleObjs.Count > 0) obstacles.obstacleObjs.Clear();
        if (obstaclePrefab != null)  
        {
            for (int i = 0; i < obstacleCount; i++)
            {
                RandomInstantiate<ObstacleObj>(obstaclePrefab, obstacles.obstacleObjs, new Vector2(8f, 8f));
            }
        }
    }

    private void RandomInstantiate<T>(GameObject prefab, List<T> list, Vector2 range)
    {
        float direction = Random.Range(0f, 360f);
        Vector3 position = new Vector2(Random.Range(-range.x, range.x), Random.Range(-range.y, range.y));
        GameObject gameObject = Instantiate(prefab, position, Quaternion.Euler(Vector3.forward * direction) * prefab.transform.localRotation);
        gameObject.transform.SetParent(transform);
        list.Add(gameObject.GetComponent<T>());
    }
}