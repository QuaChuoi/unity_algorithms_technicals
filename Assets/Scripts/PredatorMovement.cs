using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PredatorMovement : MonoBehaviour
{
    [SerializeField] private ListBoidVariable targets;
    private float forwardSpeed = 7f;
    private float turnSpeed = 10f;
    private float searchRadius = 6f;
    private float visionAngle = 200f;
    private float chaseColdown = 6f;
    private BoidMovement chaseTarget;
    public Vector3 velocity { get; private set; }
    private bool canRunClosestTarget = true;
    private void Start() {

    }
    private void FixedUpdate() 
    {
        MoveForward();
        LookRotation();
}

    private void MoveForward()
    {
        velocity = Vector2.Lerp(velocity, CalculateVelocity(), (float)(turnSpeed / 2 * Time.fixedDeltaTime));
        transform.position += velocity * Time.fixedDeltaTime;
    }
    
    private void LookRotation()
    {
        Quaternion targetRotation = Quaternion.LookRotation(velocity) * Quaternion.Euler(velocity.x>0 ? 0 :180, 90, 90);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
    }

    private List<BoidMovement> TargetInRange()
    {
        var listBoid = targets.boidMovements.FindAll(boid => boid != this && (boid.transform.position - transform.position).magnitude < searchRadius && InVisionCone(boid.transform.position));
        return listBoid;
    }

        private bool InVisionCone(Vector2 targetPosition)
    {
        Vector2 directionToPosition = targetPosition - (Vector2)transform.position;
        float dotProduct = Vector2.Dot(transform.up, directionToPosition);
        float cosHalfVisonAngle = Mathf.Cos(visionAngle * 0.5f * Mathf.Deg2Rad);
        return dotProduct > cosHalfVisonAngle;
    }

    private void OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        var targetsInRange = TargetInRange();
        foreach (var target in targetsInRange)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, target.transform.position);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + velocity);    
    }

    // private void OnDrawGizmos() {
    //     Gizmos.color = Color.white;
    //     Gizmos.DrawWireSphere(transform.position, searchRadius);

    //     var targetsInRange = TargetInRange();
    //     foreach (var target in targetsInRange)
    //     {
    //         Gizmos.color = Color.white;
    //         Gizmos.DrawLine(transform.position, target.transform.position);
    //     }

    //     Gizmos.color = Color.red;
    //     Gizmos.DrawLine(transform.position, transform.position + velocity);    

    //     // vẽ gizmos biểu thị 3 trục của tranform bằng 3 màu  khác nhau
    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawLine(transform.position, transform.position + transform.right);
    //     Gizmos.color = Color.green;
    //     Gizmos.DrawLine(transform.position, transform.position + transform.up);
    //     Gizmos.color = Color.blue;
    //     Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    // }

    private Vector3 CalculateVelocity()
    {
        var targetsInRange = TargetInRange();
        if (targetsInRange.Count > 0 & canRunClosestTarget) 
        {
            StartCoroutine(RunClosestTarget(targetsInRange)); 
        }
        
        Vector2 velocity = ((Vector2)transform.up
            + ForwardTarget(targetsInRange) * 2
            ).normalized * forwardSpeed;
        return velocity;
    }

    private Vector2 ForwardTarget(List<BoidMovement> targetsInRange)
    {
        if (chaseTarget == null) return Vector2.zero;
        Vector2 direction = chaseTarget.transform.position - transform.position;
        return direction.normalized; 
    }

    private void ClosestTarget(List<BoidMovement> targetsInRange)
    {
        if (targetsInRange == null || targetsInRange.Count == 0)
        {
            return;
        }
        BoidMovement closestTarget = targetsInRange[0];
        float closestDistance = Vector3.Distance(transform.position, closestTarget.transform.position);
        foreach (var target in targetsInRange)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }

        chaseTarget = closestTarget;
    }

    private IEnumerator RunClosestTarget(List<BoidMovement> targetsInRange)
    {
        canRunClosestTarget = false;
        ClosestTarget(targetsInRange);
        yield return new WaitForSeconds(chaseColdown);
        canRunClosestTarget = true;
    }
}
