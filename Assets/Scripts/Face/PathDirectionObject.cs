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

    private Vector3 previousForward;

    void Start()
    {
        if (eyeTransform == null) eyeTransform = transform.parent.gameObject.transform; // if Face is not assigned, assume it's the parent object
        previousForward = eyeTransform.forward;
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

        if (pathTrajectory != null)
        {
            Vector3 positionInPath = GetPositionInPath(currentPosInPath + moveSpeed * Time.deltaTime);
            transform.position = new Vector3(positionInPath.x, height + heightOffset, positionInPath.z); // fix y to desired height
        }
        else //Assume simple trajectory continuation
        {
            Vector3 newPos = Vector3.MoveTowards(transform.position, rigidBodyController.transform.position + (rigidBodyController.transform.forward * stepsAhead), Mathf.Abs(moveSpeed * Time.deltaTime));
            Debug.DrawRay(eyeTransform.position, previousForward*100f, Color.magenta);
            transform.position = new Vector3(newPos.x, height, newPos.z);
        }

        UpdateStepsAheadValue(rigidBodyController.groundSlopeAngle);
        //UpdateHeight(rigidBodyController.groundSlopeAngle);
        previousForward = rigidBodyController.transform.forward;        
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

    public Vector3 GetPositionInPath(float distanceAlongPath)
    {
        if (pathTrajectory != null)
        {
            currentPosInPath = pathTrajectory.StandardizeUnit(distanceAlongPath, positionUnits);
            return pathTrajectory.EvaluatePositionAtUnit(currentPosInPath, positionUnits);
        }
        return Vector3.zero;
    }

    public float GetGroundSlopeAngle()
    {
        return rigidBodyController.groundSlopeAngle;
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
}
