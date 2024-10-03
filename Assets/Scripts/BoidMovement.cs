using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BoidMovement : MonoBehaviour
{
    [SerializeField] private Boundery boundery;
    [SerializeField] private ListBoidVariable boids;
    [SerializeField] private ListObstacleVariable obstacles;
    private float searchRadius = 2f;
    private float obstaclesearchRadius = 6f;
    private float visionAngle = 270f;
    public float forwardSpeed = 7f;
    public float rushSpeed = 16f;
    private float rushTime = 0.6f;
    public float normalSpeed = 7f;
    private float turnSpeed = 18f;
    public Vector3 velocity { get; private set; }
    private void Start() {
        
    }
    private void FixedUpdate()
    {
        velocity = Vector2.Lerp(velocity, CalculateVelocity(), (float)(turnSpeed / 2 * Time.fixedDeltaTime));
        transform.position += velocity * Time.fixedDeltaTime;
        LookRotation();
    }

    private Vector3 CalculateVelocity()
    {
        var boidsInRange = BoidsInRange();
        var obstacleInRange = ObstacleInRange();
        if (obstacleInRange.Count > 0)
        {
            StartCoroutine(ChangeSpeed());
        }
        Vector2 velocity = ((Vector2)transform.forward
            + Separation(boidsInRange) * 1.6f
            + Alignment(boidsInRange) * 0.2f
            + Cohesion(boidsInRange) * 1.2f
            + ObstacleSeparation(obstacleInRange) * 1.9f
            ).normalized * forwardSpeed;
        return velocity;
    }

    private void LookRotation()
    {
        // Store the current rotation
        Quaternion currentRotation = transform.rotation;

        // Calculate the new rotation
        Quaternion targetRotation = Quaternion.LookRotation(velocity);
        
        // Interpolate between the current rotation and the new rotation
        Quaternion interpolatedRotation = Quaternion.Slerp(currentRotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

        // Apply the interpolated rotation but keep the original z rotation
        Vector3 eulerAngles = interpolatedRotation.eulerAngles;
        eulerAngles.z = (velocity.x <= 0) ? 60 : 295;
        transform.rotation = Quaternion.Euler(eulerAngles);
    }

    private List<BoidMovement> BoidsInRange()
    {
        var listBoid = boids.boidMovements.FindAll(boid => boid != this && (boid.transform.position - transform.position).magnitude < searchRadius && InVisionCone(boid.transform.position));
        return listBoid;
    }
    //viết 1 funtion tượng tự nhu BoidsInRange() để tìm obstacle trong range
    private List<ObstacleObj> ObstacleInRange()
    {
        var listObstacle = obstacles.obstacleObjs.FindAll(obstacle => (obstacle.transform.position - transform.position).magnitude < obstaclesearchRadius);
        return listObstacle;
    }

    private IEnumerator ChangeSpeed()
    {
        forwardSpeed = rushSpeed;
        yield return new WaitForSeconds(rushTime);
        forwardSpeed = normalSpeed;
    }

    // ktra tầm nhìn
    private bool InVisionCone(Vector2 targetPosition)
    {
        Vector2 directionToPosition = targetPosition - (Vector2)transform.position;
        float dotProduct = Vector2.Dot(transform.forward, directionToPosition);
        float cosHalfVisonAngle = Mathf.Cos(visionAngle * 0.5f * Mathf.Deg2Rad);
        return dotProduct > cosHalfVisonAngle;
    }
    private void OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        var boidsInRange = BoidsInRange();
        foreach (var boid in boidsInRange)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, boid.transform.position);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, obstaclesearchRadius);

        var obstaclesInRange = ObstacleInRange();
        foreach (var obstacle in obstaclesInRange)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, obstacle.transform.position);
        }
    }

    // tự tạo khoảng cách với các boid trong range
    private Vector2 Separation(List<BoidMovement> boidsInRange)
    {
        Vector2 direction = Vector2.zero;
        foreach (var boid in boidsInRange)
        {
            float ratio = Mathf.Clamp01((boid.transform.position - transform.position).magnitude / searchRadius);
            direction -= ratio * (Vector2)(boid.transform.position - transform.position);
        }
        return direction.normalized;
    }
    private Vector2 ObstacleSeparation(List<ObstacleObj> obstaclesInRange)
    {
        Vector2 direction = Vector2.zero;
        foreach (var obstacle in obstaclesInRange)
        {
            float ratio = Mathf.Clamp01((obstacle.transform.position - transform.position).magnitude / obstaclesearchRadius);
            direction -= ratio * (Vector2)(obstacle.transform.position - transform.position);
        }
        return direction.normalized;
    }

    // di chuyển theo hướng trung bình của các boid trong range
    private Vector2 Alignment(List<BoidMovement> boidsInRange)
    {
        Vector2 averageDirection = Vector2.zero;
        foreach (var boid in boidsInRange)
        {
            averageDirection += (Vector2)boid.velocity;
        }
        if (boidsInRange.Count != 0) averageDirection /= boidsInRange.Count;
        else averageDirection = velocity;

        return averageDirection.normalized;
    }

    //di chuyển về trung tâm của các boid trong range
    private Vector2 Cohesion(List<BoidMovement> boidsInRange)
    {
        Vector2 direction;
        Vector2 center = Vector2.zero;
        foreach (var boid in boidsInRange)
        {
            center += (Vector2)boid.transform.position;
        }
        if (boidsInRange.Count != 0) center /= boidsInRange.Count;
        else center = transform.position;
        direction = center - (Vector2)transform.position;
        return direction.normalized;
    }
}
