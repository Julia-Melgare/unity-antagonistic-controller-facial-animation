using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffortController : MonoBehaviour
{
    [Header("Character Parameters")]
    [SerializeField]
    private Rigidbody rigidBody;

    private float slopeEffort = 0f;

    void Update()
    {
        slopeEffort = CalculateSlopeEffort();
    }

    private float CalculateSlopeEffort()
    {
        return rigidBody.mass * Physics.gravity.magnitude * rigidBody.velocity.y;
    }
}
