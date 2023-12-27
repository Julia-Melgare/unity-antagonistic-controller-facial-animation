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
    [SerializeField]
    private float stateUpdateTime = 0.9f;
    private Vector3 transformVelocity;
    private Vector3 lastPosition;
    private Vector3 lastTransformVelocity;
    private Vector3 lastRBVelocity;
    private int lastAnimatorState;

    private float stateUpdateTimer;
    
    void Start()
    {
        motionState = MotionState.Static;
        lastPosition = transform.position;
        stateUpdateTimer = 0f;
        CheckComponentAtribution();
    }
    void Update()
    {
        transformVelocity = transform.position - lastPosition;

        if (stateUpdateTimer <= 0)
        {
            var lastMotionState = motionState;
            UpdateMotionState();
            if (motionState != lastMotionState)
            {
                stateUpdateTimer = stateUpdateTime;
            }
        }
        else
        {
            stateUpdateTimer -= Time.deltaTime;
        }
        
        lastTransformVelocity = transformVelocity;
        lastRBVelocity = rigidBody.velocity;
        lastAnimatorState = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        lastPosition = transform.position;
    }

    private void UpdateMotionState()
    {
        // Check Static State
        if ((rigidBody.velocity.sqrMagnitude == 0 || transformVelocity.sqrMagnitude == 0 ) && !animator.isActiveAndEnabled)
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

    private void CheckComponentAtribution()
    {
        if (rigidBody == null)
        {
            rigidBody = gameObject.GetComponent<Rigidbody>();
        }
        if (animator == null)
        {
            animator = gameObject.GetComponent<Animator>();
        }
    }

    public Vector3 GetVelocity()
    {   var animClipVelocity = animator.GetCurrentAnimatorClipInfo(0)[0].clip.averageSpeed;
        var animVelocity = animator.velocity.sqrMagnitude > animClipVelocity.sqrMagnitude ? animator.velocity : animClipVelocity;
        var moveVelocity = rigidBody.velocity.sqrMagnitude > transformVelocity.sqrMagnitude ? rigidBody.velocity : transformVelocity;
        return animVelocity.sqrMagnitude > moveVelocity.sqrMagnitude ? animVelocity : moveVelocity;
    }
}
