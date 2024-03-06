using System;
using Cinemachine;
using UnityEngine;

public class LocomotionTarget : MonoBehaviour
{
    [SerializeField]
    private CinemachinePath pathTrajectory;
    [SerializeField]
    private CinemachinePathBase.PositionUnits positionUnits = CinemachinePathBase.PositionUnits.Distance;
    [SerializeField]
    private Transform characterTransform;
    [SerializeField]
    private float maxMoveSpeed = 100f;

    public float stepsAhead = 7f;
    public float height = 3f;

    private float currentPosInPath;
    private float moveSpeed = 1f;

    void Update()
    {
        float distance = Vector3.Distance(transform.position, characterTransform.position);
        float difference = stepsAhead - distance;
        
        if (Math.Abs(difference) > .001f)
        {
            moveSpeed = maxMoveSpeed * difference;
        }
        else
        {
            moveSpeed = 0;
        }

        Vector3 positionInPath = GetPositionInPath(currentPosInPath + moveSpeed * Time.deltaTime);
        transform.position = new Vector3(positionInPath.x, characterTransform.position.y + height, positionInPath.z);
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
