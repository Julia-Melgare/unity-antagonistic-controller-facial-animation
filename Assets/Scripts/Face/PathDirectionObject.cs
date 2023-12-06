using System;
using Cinemachine;
using UnityEngine;

public class PathDirectionObject : MonoBehaviour
{
    [SerializeField]
    private CinemachinePath pathTrajectory;
    [SerializeField]
    private CinemachinePathBase.PositionUnits positionUnits = CinemachinePathBase.PositionUnits.Distance;
    [SerializeField]
    private Transform faceTransform;

    [SerializeField]
    private float maxMoveSpeed = 100f;
    public float stepsAhead = 7f;
    public float heightOffset = 3f;

    private float currentPosInPath;
    private float moveSpeed = 1f;

    void Start()
    {
        if (faceTransform == null) faceTransform = transform.parent.gameObject.transform; // if Face is not assigned, assume it's the parent object
    }

    void Update()
    {
        float distance = Mathf.Abs(Vector3.Distance(transform.position, faceTransform.position));
        float difference = stepsAhead - distance;
        
        if (Mathf.Abs(difference) > .001f)
        {
            moveSpeed = maxMoveSpeed * difference;
        }
        else
        {
            moveSpeed = 0;
        }

        if (pathTrajectory != null)
        {
            Vector3 positionInPath = GetPositionInPath(currentPosInPath + moveSpeed * Time.deltaTime);
            transform.position = new Vector3(positionInPath.x, faceTransform.position.y - heightOffset, positionInPath.z); // fix y to desired height
        }
        else //Assume simple trajectory continuation
        {
            Vector3 newPos = Vector3.MoveTowards(transform.position, faceTransform.position + faceTransform.forward * stepsAhead, Mathf.Abs(moveSpeed * Time.deltaTime));
            transform.position = new Vector3(newPos.x, faceTransform.position.y - heightOffset, newPos.z);
        }

        
    }

    public Vector3 GetPositionInPath(float distanceAlongPath)
    {
        if (pathTrajectory != null)
        {
            currentPosInPath = pathTrajectory.StandardizeUnit(distanceAlongPath, positionUnits);
            return pathTrajectory.EvaluatePositionAtUnit(currentPosInPath, positionUnits);
        }
        return Vector3.zero;
    }
}
