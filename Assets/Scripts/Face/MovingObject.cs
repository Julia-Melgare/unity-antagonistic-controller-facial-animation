using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingObject : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rigidBody;
    [SerializeField]
    private Animator animator;
    public MotionState motionState;
    public Vector3 transformVelocity;
    private Vector3 lastPosition;
    private Vector3 lastTransformVelocity;
    private Vector3 lastRBVelocity;
    private int lastAnimatorState;
    
    void Start()
    {
        motionState = MotionState.Static;
        lastPosition = transform.position;
    }
    void Update()
    {
        transformVelocity = transform.position - lastPosition;
        UpdateMotionState();
        lastTransformVelocity = transformVelocity;
        lastRBVelocity = rigidBody.velocity;
        lastAnimatorState = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        lastPosition = transform.position;
    }

    private void UpdateMotionState()
    {
        // Check Static State
        if (rigidBody.velocity.sqrMagnitude == 0 || transformVelocity.sqrMagnitude == 0 || !animator.isActiveAndEnabled)
        {
            motionState = MotionState.Static;
            return;
        }
        // Check Onset State
        if ((lastRBVelocity.sqrMagnitude == 0 && rigidBody.velocity.sqrMagnitude > 0) || (lastTransformVelocity.sqrMagnitude == 0 && transformVelocity.sqrMagnitude > 0))
        {
            motionState = MotionState.Onset;
            return;
        }
        // Check Continuous State
        if (((rigidBody.velocity.sqrMagnitude - lastRBVelocity.sqrMagnitude) <= 0.01f && Vector3.Dot(rigidBody.velocity.normalized, lastRBVelocity.normalized) >= 0.9f) || ((transformVelocity.sqrMagnitude - lastTransformVelocity.sqrMagnitude) <= 0.01f && Vector3.Dot(transformVelocity.normalized, lastTransformVelocity.normalized) >= 0.9f) || animator.GetCurrentAnimatorStateInfo(0).fullPathHash == lastAnimatorState)
        {
            motionState = MotionState.Continuous;
            return;
        }
        // Check Offset State
        if ((Vector3.Dot(rigidBody.velocity.normalized, lastRBVelocity.normalized) >= 0.9f && rigidBody.velocity.sqrMagnitude < lastRBVelocity.sqrMagnitude) || (Vector3.Dot(transformVelocity.normalized, lastTransformVelocity.normalized) >= 0.9f && transformVelocity.sqrMagnitude < lastTransformVelocity.sqrMagnitude))
        {
            motionState = MotionState.Offset;
            return;
        }
        // Check Change State
        if ((rigidBody.velocity.sqrMagnitude - lastRBVelocity.sqrMagnitude) > 0.01f || Vector3.Dot(rigidBody.velocity.normalized, lastRBVelocity.normalized) < 0.9f || (transformVelocity.sqrMagnitude - lastTransformVelocity.sqrMagnitude) > 0.01f || Vector3.Dot(transformVelocity.normalized, lastTransformVelocity.normalized) < 0.9f || animator.IsInTransition(0) || animator.GetCurrentAnimatorStateInfo(0).fullPathHash != lastAnimatorState)
        {
            motionState = MotionState.Change;
            return;
        }
    }
}
