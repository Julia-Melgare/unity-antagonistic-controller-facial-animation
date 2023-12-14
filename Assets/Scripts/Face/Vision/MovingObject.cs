using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingObject : MonoBehaviour
{
    public Vector3 velocity;
    private Vector3 lastPosition; 
    
    void Start()
    {
        lastPosition = transform.position;
    }
    void Update()
    {
        velocity = transform.position - lastPosition;
        lastPosition = transform.position;
    }
}
