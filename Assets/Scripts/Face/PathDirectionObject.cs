using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PathDirectionObject : MonoBehaviour
{
    [SerializeField]
    private CinemachinePath pathTrajectory;
    [SerializeField]
    private CinemachinePathBase.PositionUnits positionUnits = CinemachinePathBase.PositionUnits.Distance;
    [SerializeField]
    private Transform eyeTransform;
    [SerializeField]
    private RigidBodyController rigidBodyController;
    [SerializeField]
    private Terrain terrain;
    public LayerMask Ground;

    [SerializeField]
    private float maxMoveSpeed = 100f;
    [SerializeField]
    private float maxStepsAhead = 9f;
    [SerializeField]
    private float minStepsAhead = 2f;

    public float stepsAhead = 7f;
    public float heightOffset = 3f;
    private float height = 3f;

    private float currentPosInPath;
    private float moveSpeed = 1f;
    private CinemachineSmoothPath defaultCurve = null;

    void Start()
    {
        if (!rigidBodyController.followPath) InitializeDefaultCurve();
        if (eyeTransform == null) eyeTransform = transform.parent.gameObject.transform; // if Face is not assigned, assume it's the parent object
    }

    void Update()
    {
        CheckGround();
        float distance = Mathf.Abs(Vector3.Distance(transform.position, eyeTransform.position));
        float difference = stepsAhead - distance;
        
        if (Mathf.Abs(difference) > .001f)
        {
            moveSpeed = maxMoveSpeed * difference;
        }
        else
        {
            moveSpeed = 0;
        }

        if (rigidBodyController.followPath)
        {
            Vector3 positionInPath = GetPositionInPath(currentPosInPath + moveSpeed * Time.deltaTime);
            transform.position = new Vector3(positionInPath.x, height + heightOffset, positionInPath.z); // fix y to desired height
        }
        else //Assume simple trajectory estimation
        {
            UpdateDefaultCurve();
            Vector3 positionInPath = GetPositionInDefaultCurve(currentPosInPath + moveSpeed * Time.deltaTime);
            transform.position = new Vector3(positionInPath.x, height + heightOffset, positionInPath.z);
        }

        UpdateStepsAheadValue(rigidBodyController.groundSlopeAngle);
        //UpdateHeight(rigidBodyController.groundSlopeAngle);
    }

    private void UpdateStepsAheadValue(float slopeAngle, float maxSlopeAngle = 30)
    {
        stepsAhead = maxStepsAhead - ((slopeAngle * (maxStepsAhead - minStepsAhead))/maxSlopeAngle);
    }

    /*private void UpdateHeight(float slopeAngle, float maxSlopeAngle = 30)
    {
        float maxHeight = eyeTransform.position.y;
        float minHeight = rigidBodyController.transform.position.y;
        height = (maxHeight - minHeight)/2;//maxHeight - (minHeight + (slopeAngle * (maxHeight - minHeight))/maxSlopeAngle);    
    }*/

    private void UpdateDefaultCurve()
    {
        float userInput = Input.GetAxis("Horizontal");
        if (userInput == 0)
        {
            defaultCurve.m_Waypoints[1].position = Vector3.MoveTowards(defaultCurve.m_Waypoints[1].position, (defaultCurve.m_Waypoints[0].position + defaultCurve.m_Waypoints[2].position)/2f, 4 * Time.deltaTime);
        }
        else
        {
            defaultCurve.m_Waypoints[1].position.z += Input.GetAxis("Horizontal");
        }        
        defaultCurve.m_Waypoints[1].position.z = Mathf.Clamp(defaultCurve.m_Waypoints[1].position.z, 118f, 130f);
        defaultCurve.InvalidateDistanceCache();
    }

    private void CheckGround()
    {
        if (terrain != null)
        {
            height = terrain.SampleHeight(transform.position);
        }

        else
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, .1f, Ground))
            {
                height += 0.01f;
            }
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1f, Ground))
            {
                height -= 0.01f;
            }
            if (Physics.Raycast(transform.position, Vector3.up, out hit, 100f, Ground))
            {
                height += hit.distance;
            }
        }        
    }

    private void InitializeDefaultCurve()
    {
        GameObject path = new GameObject("PathTrajectory");
        path.transform.parent = rigidBodyController.transform;
        defaultCurve = path.AddComponent<CinemachineSmoothPath>();
        CinemachineSmoothPath.Waypoint startWaypoint = new CinemachineSmoothPath.Waypoint();
        CinemachineSmoothPath.Waypoint middleWaypoint = new CinemachineSmoothPath.Waypoint();
        CinemachineSmoothPath.Waypoint endWaypoint = new CinemachineSmoothPath.Waypoint();
        defaultCurve.m_Waypoints = new CinemachineSmoothPath.Waypoint[3];
        defaultCurve.m_Waypoints[0] = startWaypoint;
        defaultCurve.m_Waypoints[1] = middleWaypoint;
        defaultCurve.m_Waypoints[2] = endWaypoint;
        defaultCurve.m_Waypoints[0].position = rigidBodyController.transform.position;
        defaultCurve.m_Waypoints[2].position = rigidBodyController.transform.position + (rigidBodyController.transform.forward * maxStepsAhead * 2f);
        defaultCurve.m_Waypoints[1].position = (defaultCurve.m_Waypoints[0].position + defaultCurve.m_Waypoints[2].position)/2f;
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

    public Vector3 GetPositionInDefaultCurve(float distanceAlongCurve)
    {
        if (defaultCurve != null)
        {
            currentPosInPath = defaultCurve.StandardizeUnit(distanceAlongCurve, positionUnits);
            return defaultCurve.EvaluatePositionAtUnit(currentPosInPath, positionUnits);
        }
        return Vector3.zero;
    }

    public float GetGroundSlopeAngle()
    {
        return rigidBodyController.groundSlopeAngle;
    }
}
