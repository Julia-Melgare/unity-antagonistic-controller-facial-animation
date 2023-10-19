using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingObject : MonoBehaviour
{
    public Transform lastTransform; 
    void LateUpdate()
    {
        lastTransform = gameObject.transform;
    }
}
