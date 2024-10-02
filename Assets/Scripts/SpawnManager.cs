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
    private void Start()
    {
        if (boids.boidMovements.Count > 0) boids.boidMovements.Clear(); 
        for (int i = 0; i < boidCount; i++)
        {
            float direction = Random.Range(0f, 360f);
            Vector3 position = new Vector2(Random.Range(-15f, 15f), Random.Range(-10f, 10f));
            GameObject boid = Instantiate(boidPrefab, position, Quaternion.Euler(Vector3.forward * direction) * boidPrefab.transform.localRotation);
            boid.transform.SetParent(transform);
            boids.boidMovements.Add(boid.GetComponent<BoidMovement>());
        }

        if(obstacles.obstacleObjs.Count > 0) obstacles.obstacleObjs.Clear();
        if (obstaclePrefab != null)  
        {
            float direction = Random.Range(0f, 360f);
            Vector3 position = new Vector2(Random.Range(-15f, 15f), Random.Range(-10f, 10f));
            GameObject obstacle = Instantiate(obstaclePrefab, position, Quaternion.Euler(Vector3.forward * direction) * obstaclePrefab.transform.localRotation);
            obstacle.transform.SetParent(transform);
            obstacles.obstacleObjs.Add(obstacle.GetComponent<ObstacleObj>());
        }
    }
}