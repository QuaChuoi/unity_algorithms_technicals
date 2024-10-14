using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.VisualScripting;

public class BoidTransform : MonoBehaviour
{
    [SerializeField] private Boundery boundery;
    [SerializeField] private ListBoidVariable boids;
    [SerializeField] private ListObstacleVariable obstacles;
    private float searchRadius = 2f;
    private float obstaclesearchRadius = 6f;
    private float visionAngle = 270f;
    private float forwardSpeed = 6f;
    private float rushSpeed = 16f;
    private float rushTime = 0.6f;
    private float normalSpeed = 6f;
    private float turnSpeed = 18f;
    private TransformAccessArray transformAccessArray;
    private NativeArray<BoidData> boidData;
    private struct BoidData
    {
        public float3 position;
        public float3 velocity;
    }
    [BurstCompile]
    private struct BoidMovementsJob : IJobParallelForTransform
    {
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<BoidData> boidData;
        public float turnSpeed;
        public float forwardSpeed;
        public float deltaTime;
        public float searchRadius;
        public float visionAngle;
        public float xLimit;
        public float yLimit;
        public void Execute(int index, TransformAccess transform)
        {
            Vector3 velocity = boidData[index].velocity;
            velocity = Vector2.Lerp(velocity, CalculateVelocity(transform), (float)(turnSpeed / 2 * deltaTime));
            transform.position += velocity * deltaTime;
            // rotation
            if (velocity != Vector3.zero)
            {
                Quaternion currentRotation = transform.rotation;
                Quaternion targetRotation = Quaternion.LookRotation(velocity);
                Quaternion interpolatedRotation = Quaternion.Slerp(currentRotation, targetRotation, turnSpeed * deltaTime);
                Vector3 eulerAngles = interpolatedRotation.eulerAngles;
                eulerAngles.z = (velocity.x <= 0) ? 60 : 295;
                transform.rotation = Quaternion.Euler(eulerAngles);
            }
            boidData[index] = new BoidData
            {
                position = transform.position,
                velocity = velocity,
            };

        }
        private Vector3 CalculateVelocity(TransformAccess transform)
        {
            var separation = Vector2.zero;
            var alignment = Vector2.zero;
            var cohesion = Vector2.zero;
            var boxLimit = BoxLimit(transform.position);
            Vector2 currentForward = transform.localToWorldMatrix.MultiplyVector(Vector3.forward);
            var boidsInRange = BoidsInRange(transform.position, currentForward);
            var boidCount = boidsInRange.Length;
            // var obstacleInRange = ObstacleInRange();
            // if (obstacleInRange.Count > 0)
            // {
            //     StartCoroutine(ChangeSpeed());
            // }
            for (var i=0; i<boidCount; i++)
            {
                separation -= Separation(transform.position, boidsInRange[i].position.xy);
                alignment += (Vector2)boidsInRange[i].velocity.xy;
                cohesion += (Vector2)boidsInRange[i].position.xy;
            }
            separation = separation.normalized;
            alignment = Alignment(alignment, currentForward, boidCount);
            cohesion = Cohesion(cohesion, transform.position, boidCount);
            Vector3 velocity = (currentForward
                // + Separation(boidsInRange) * 1.6f
                + separation * 1.6f
                // + Alignment(boidsInRange) * 0.2f
                + alignment
                // + Cohesion(boidsInRange) * 1.2f
                + cohesion * 1.2f
                // + ObstacleSeparation(obstacleInRange) * 1.9f
                + boxLimit * 3f
                ).normalized * forwardSpeed;
                // Debug.Log("forward :" + currentForward + " | separation:" + separation + " | velocity:" + velocity);
            return velocity;
        }
        private NativeArray<BoidData> BoidsInRange(float3 position, float2 forward)
        {
            NativeList<BoidData> boidsInRange = new NativeList<BoidData>(Allocator.Temp);
            for (int i=0; i < boidData.Length; i++)
            {
                if (math.distance(position, boidData[i].position) < searchRadius && InVisionCone(position.xy, forward, boidData[i].position.xy))
                {
                    boidsInRange.Add(boidData[i]);
                }
            }
            // convert native list to native array
            NativeArray<BoidData> boids = new NativeArray<BoidData>(boidsInRange.AsArray(), Allocator.Temp);
            boidsInRange.Dispose();
            return boids;
        }
        private Vector2 Separation(Vector2 currentPosition, Vector2 boidPosition)
        {
            float ratio = Mathf.Clamp01((boidPosition - currentPosition).magnitude / searchRadius);
            return (1 - ratio)* (boidPosition - currentPosition);
        }
        private bool InVisionCone(Vector2 position, Vector2 forward, Vector2 boidPosition)
        {
            Vector2 directionToPosition = boidPosition - position;
            float dotProduct = Vector2.Dot(forward.normalized, directionToPosition);
            float cosHalfVisonAngle = Mathf.Cos(visionAngle * 0.5f * Mathf.Deg2Rad);
            return dotProduct > cosHalfVisonAngle;
        }
        private Vector2 Alignment(Vector2 direction, Vector2 forward, int boidCount)
        {
            if (boidCount != 0) direction /= boidCount;
            else direction = forward;
            return direction.normalized;
        }
        private Vector2 Cohesion(Vector2 center, Vector2 position, int boidCount)
        {
            if (boidCount != 0) center /= boidCount;
            else center = position;
            return (center - position).normalized;
        }
        private Vector2 BoxLimit(Vector2 position)
        {
            if (position.x <= -xLimit)
            {
                return Vector2.right;
            }
            if (position.x >= xLimit)
            {
                return Vector2.left;
            }
            if (position.y <= -yLimit)
            {
                return Vector2.up;
            }
            if (position.y >= yLimit)
            {
                return Vector2.down;
            }
            return Vector2.zero;
        }
    }
    private void Start()
    {
        var boidCount = boids.boidTranform.Count;
        transformAccessArray = new TransformAccessArray(boidCount);
        boidData = new NativeArray<BoidData>(boidCount, Allocator.Persistent);
        for (int i = 0; i < boidCount; i++)
        {
            transformAccessArray.Add(boids.boidTranform[i].transform);
            boidData[i] = new BoidData
            {
                position = boids.boidTranform[i].transform.position,
                velocity = boids.boidTranform[i].transform.forward,
            };
        }
    }
    private void FixedUpdate()
    {
        // velocity = Vector2.Lerp(velocity, CalculateVelocity(), (float)(turnSpeed / 2 * Time.fixedDeltaTime));
        // transform.position += velocity * Time.fixedDeltaTime;
        // LookRotation();
        var boidMovementsJob = new BoidMovementsJob{
            boidData = boidData,
            turnSpeed = turnSpeed,
            forwardSpeed = forwardSpeed,
            searchRadius = searchRadius,
            visionAngle = visionAngle,
            xLimit = boundery.XLimit,
            yLimit = boundery.YLimit,
            deltaTime = Time.fixedDeltaTime,
        };
        JobHandle boidMovementsJobHandle = boidMovementsJob.Schedule(transformAccessArray);
        boidMovementsJobHandle.Complete(); 
        
    }
    private void Update() 
    {
        // var boidMovementsJob = new BoidMovementsJob{
        //     boidData = boidData,
        //     turnSpeed = turnSpeed,
        //     forwardSpeed = forwardSpeed,
        //     searchRadius = searchRadius,
        //     visionAngle = visionAngle,
        //     deltaTime = Time.deltaTime,
        // };
        // JobHandle boidMovementsJobHandle = boidMovementsJob.Schedule(transformAccessArray);
        // boidMovementsJobHandle.Complete(); 
    }
    private void OnDestroy() 
    {
        transformAccessArray.Dispose();
        boidData.Dispose();
    }
    // private Vector3 CalculateVelocity()
    // {
    //     var boidsInRange = BoidsInRange();
    //     var obstacleInRange = ObstacleInRange();
    //     if (obstacleInRange.Count > 0)
    //     {
    //         StartCoroutine(ChangeSpeed());
    //     }
    //     Vector2 velocity = ((Vector2)transform.forward
    //         + Separation(boidsInRange) * 1.6f
    //         + Alignment(boidsInRange) * 0.2f
    //         + Cohesion(boidsInRange) * 1.2f
    //         + ObstacleSeparation(obstacleInRange) * 1.9f
    //         ).normalized * forwardSpeed;
    //     return velocity;
    // }

    // private List<BoidMovements> BoidsInRange()
    // {
    //     var listBoid = boids.boidMovements.FindAll(boid => boid != this && (boid.transform.position - transform.position).magnitude < searchRadius && InVisionCone(boid.transform.position));
    //     return listBoid;
    // }
    
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
    // private bool InVisionCone(Vector2 targetPosition)
    // {
    //     Vector2 directionToPosition = targetPosition - (Vector2)transform.position;
    //     float dotProduct = Vector2.Dot(transform.forward, directionToPosition);
    //     float cosHalfVisonAngle = Mathf.Cos(visionAngle * 0.5f * Mathf.Deg2Rad);
    //     return dotProduct > cosHalfVisonAngle;
    // }
    private void OnDrawGizmosSelected() 
    {
        // Gizmos.color = Color.green;
        // Gizmos.DrawWireSphere(transform.position, searchRadius);

        // var boidsInRange = BoidsInRange();
        // foreach (var boid in boidsInRange)
        // {
        //     Gizmos.color = Color.yellow;
        //     Gizmos.DrawLine(transform.position, boid.transform.position);
        // }

        // Gizmos.color = Color.red;
        // Gizmos.DrawWireSphere(transform.position, obstaclesearchRadius);

        // var obstaclesInRange = ObstacleInRange();
        // foreach (var obstacle in obstaclesInRange)
        // {
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawLine(transform.position, obstacle.transform.position);
        // }
    }

    // tự tạo khoảng cách với các boid trong range
    // private Vector2 Separation(List<Transform> boidsInRange)
    // {
    //     Vector2 direction = Vector2.zero;
    //     foreach (var boid in boidsInRange)
    //     {
    //         float ratio = Mathf.Clamp01((boid.transform.position - transform.position).magnitude / searchRadius);
    //         direction -= ratio * (Vector2)(boid.transform.position - transform.position);
    //     }
    //     return direction.normalized;
    // }
    // private Vector2 ObstacleSeparation(List<ObstacleObj> obstaclesInRange)
    // {
    //     Vector2 direction = Vector2.zero;
    //     foreach (var obstacle in obstaclesInRange)
    //     {
    //         float ratio = Mathf.Clamp01((obstacle.transform.position - transform.position).magnitude / obstaclesearchRadius);
    //         direction -= ratio * (Vector2)(obstacle.transform.position - transform.position);
    //     }
    //     return direction.normalized;
    // }

    // di chuyển theo hướng trung bình của các boid trong range
    // private Vector2 Alignment(List<Transform> boidsInRange)
    // {
    //     Vector2 averageDirection = Vector2.zero;
    //     foreach (var boid in boidsInRange)
    //     {
    //         averageDirection += (Vector2)boid.velocity;
    //     }
    //     if (boidsInRange.Count != 0) averageDirection /= boidsInRange.Count;
    //     else averageDirection = velocity;

    //     return averageDirection.normalized;
    // }

    //di chuyển về trung tâm của các boid trong range
    // private Vector2 Cohesion(List<Transform> boidsInRange)
    // {
    //     Vector2 direction;
    //     Vector2 center = Vector2.zero;
    //     foreach (var boid in boidsInRange)
    //     {
    //         center += (Vector2)boid.transform.position;
    //     }
    //     if (boidsInRange.Count != 0) center /= boidsInRange.Count;
    //     else center = transform.position;
    //     direction = center - (Vector2)transform.position;
    //     return direction.normalized;
    // }
}
