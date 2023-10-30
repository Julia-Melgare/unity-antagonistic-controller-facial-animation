using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceSafetyRegion : MonoBehaviour
{
    [SerializeField]
    private Collider safetyRegionSphere;

    [SerializeField]
    private Transform eyeTransform;

    public GameObject closestObstacle = null;
    public float closestDistanceToEye = float.MaxValue;

    public void OnTriggerStay(Collider other)
    {
        float distanceToEye = Vector3.Distance(other.gameObject.transform.position, eyeTransform.position);
        if (distanceToEye < closestDistanceToEye)
        {
            closestDistanceToEye = distanceToEye;
            closestObstacle = other.gameObject;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        closestObstacle = null;
        closestDistanceToEye = float.MaxValue;
    }
}
