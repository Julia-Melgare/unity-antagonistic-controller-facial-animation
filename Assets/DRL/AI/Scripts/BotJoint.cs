using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[ExecuteAlways]
//[RequireComponent(typeof(Rigidbody), typeof(ConfigurableJoint))]
public class BotJoint : MonoBehaviour
{

	public enum SIDE
	{
		LEFT = 0,
		CENTER = 1,
		RIGHT = 2
	}
	
    public float bodyPercentage = 0f;

	public SIDE side;
    
    public bool configuring = false;

    public bool mirroring = false;

    [Range(-1f, 1f)]
    public float actionX;
	[Range(-1f, 1f)]
	public float actionY;
	[Range(-1f, 1f)]
	public float actionZ;

	[NonSerialized]
	public Rigidbody body;
	[NonSerialized]
	public ConfigurableJoint joint;
	[NonSerialized]
	public BotParameters p;
	[NonSerialized]
	public BotJoint parent = null;
	[NonSerialized]
	public List<BotJoint> children;

	private bool initialized = false;

	private int collisionCount = 0;

	private RigidbodyCopy copy;

    private bool xLocked = false;
    private bool yLocked = false;
    private bool zLocked = false;

    [NonSerialized]
    public Quaternion startLocalRotation;

    private Vector3 lastPosition;
	private Vector3 realVelocity;

	private void Start()
	{
		Initialize();
	}

	public void Initialize()
    {
        if (initialized) return;

		body = GetComponent<Rigidbody>();
		joint = GetComponent<ConfigurableJoint>();

		if (IsBroken()) return;

		copy = new RigidbodyCopy(transform, body);

		body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

		xLocked = joint.angularXMotion != ConfigurableJointMotion.Locked;
		yLocked = joint.angularYMotion != ConfigurableJointMotion.Locked;
		zLocked = joint.angularZMotion != ConfigurableJointMotion.Locked;

		/*
        joint.xMotion = ConfigurableJointMotion.Locked;
		joint.yMotion = ConfigurableJointMotion.Locked;
		joint.zMotion = ConfigurableJointMotion.Locked;
		*/

        joint.enablePreprocessing = false;

		parent = joint.connectedBody ? joint.connectedBody.GetComponent<BotJoint>() : null;
		if (parent != null) parent.AddChild(this);

        startLocalRotation = transform.localRotation;

		initialized = true;
	}

	public void Initialize(BotParameters p)
    {
        Initialize();

        this.p = p;

        //if(!p.recording) body.collisionDetectionMode = CollisionDetectionMode.Discrete;

        Configure();
		Restart();

	}

    private void Configure()
    {
        Initialize();

        body.mass = p.totalMass * bodyPercentage/100f;

		body.maxAngularVelocity = p.maxAngularVelocity;

		joint.rotationDriveMode = RotationDriveMode.Slerp;
		var drive = joint.slerpDrive;
		drive.maximumForce = p.maxTorque * p.torqueFactor;
		joint.slerpDrive = drive;

		joint.breakForce = p.breakForce;
        joint.breakTorque = p.breakTorque;
    }

    private void FixedUpdate()
    {
        
        if (IsBroken()) 
			return;

		//if(Application.isEditor) Configure();
		CalculateVelocity();
        
    }

	public void Restart()
    {
        Initialize();
		if(copy != null) copy.paste(transform, body);
        if (!body.isKinematic)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
		lastPosition = transform.position;
	}

	[HideInInspector]
	private Vector3 normal, tangent, position, velocity, angularVelocity;
	[HideInInspector]
	private bool collision;


    public void CalculateObservations(Referential referential)
	{

		var primary = joint.axis;
		var secondary = joint.secondaryAxis;

        if(side == SIDE.RIGHT)
		{
            primary = -primary;
            secondary = -secondary;
        }

        primary = transform.TransformVector(primary);
        secondary = transform.TransformVector(secondary);
		normal = referential.InverseTransformVector(primary);
		tangent = referential.InverseTransformVector(secondary);
		position = referential.InverseTransformPoint(transform.position);
		velocity = referential.InverseTransformVector(realVelocity);
		angularVelocity = referential.InverseTransformVector(body.angularVelocity);
	    collision = collisionCount > 0;
	}

    public void CollectObservations(Referential referential, VectorSensor sensor)
    {

		if (!IsBroken())
        {
            CalculateObservations(referential);
		}

        sensor.AddObservation(normal);
        sensor.AddObservation(tangent);
        sensor.AddObservation(position);
        sensor.AddObservation(velocity);
        //sensor.AddObservation(collision);
	}

    public float SpaceMagnitude(ArticulationReducedSpace space)
    {
        float acc = 0;
        for(int i = 0; i<space.dofCount; i++)
        {
            acc += space[i] * space[i];
        }
        return Mathf.Sqrt(acc);
    }

    public void OnActionReceived(ref int i, ActionBuffers actions)
    {

        if(IsBroken())
        {
			if (p.springControl) i += 2;
            if (xLocked) i++;
            if (yLocked) i++;
            if (zLocked) i++;
            return;
        }

		if(p.springControl)
		{
			float spring = (actions.ContinuousActions[i++] + 1f) / 2f;
			float damper = (actions.ContinuousActions[i++] + 1f) / 2f;

			SetDrive(spring, damper);

			p.agent.RecordStat("Bot Joint/Spring", spring, StatAggregationMethod.Histogram);
			p.agent.RecordStat("Bot Joint/Damper", damper, StatAggregationMethod.Histogram);
		}

		Vector3 action = Vector3.zero;
        if (joint.angularXMotion != ConfigurableJointMotion.Locked)
			action.x = actions.ContinuousActions[i++];
        if (joint.angularYMotion != ConfigurableJointMotion.Locked)
			action.y = actions.ContinuousActions[i++];
        if (joint.angularZMotion != ConfigurableJointMotion.Locked)
			action.z = actions.ContinuousActions[i++];

        if(!configuring && !p.immobile)
        {
            actionX = action.x; actionY = action.y; actionZ = action.z;
		}

		if(p.immobile)
		{
			SetDrive(100, 100);
		}

		var targetRotation = GetTargetRotation();

        if(!p.immobile) joint.targetRotation = targetRotation;
        Vector3 error = joint.GetTargetAngularVelocityToTargetRotation(startLocalRotation);
        joint.targetAngularVelocity = error * p.rotationSpeedFactor;
    }

    public Quaternion GetTargetRotation()
    {
        Vector3 targetRotation = Vector3.zero;

		if (joint.angularXMotion != ConfigurableJointMotion.Locked)
		{
			targetRotation.x = actionX;
			targetRotation.x *= (joint.highAngularXLimit.limit - joint.lowAngularXLimit.limit) / 2;
			targetRotation.x += (joint.highAngularXLimit.limit + joint.lowAngularXLimit.limit) / 2;
		}
		if (joint.angularYMotion != ConfigurableJointMotion.Locked)
			targetRotation.y = actionY * joint.angularYLimit.limit;
		if (joint.angularZMotion != ConfigurableJointMotion.Locked)
			targetRotation.z = actionZ * joint.angularZLimit.limit;

		/*
		if (mirroring && side == SIDE.CENTER)
		{
			targetRotation.z = -targetRotation.z;
			targetRotation.y = -targetRotation.y;
		}
		*/

		return Quaternion.Euler(targetRotation);
	}

	public Vector3 GetTargetRotationActions(Quaternion targetRotation)
	{
		Vector3 rotations = targetRotation.eulerAngles;
		rotations = new Vector3(SmallestAngle(rotations.x), SmallestAngle(rotations.y), SmallestAngle(rotations.z));

		Vector3 actions = Vector3.zero;

		if (joint.angularXMotion != ConfigurableJointMotion.Locked)
		{
			actions.x = rotations.x;
			actions.x -= (joint.highAngularXLimit.limit + joint.lowAngularXLimit.limit) / 2;
			actions.x = actions.x % 360;
			actions.x /= (joint.highAngularXLimit.limit - joint.lowAngularXLimit.limit) / 2;
		}
		if (joint.angularYMotion != ConfigurableJointMotion.Locked)
		{
			actions.y = rotations.y / joint.angularYLimit.limit;
		}
		if (joint.angularZMotion != ConfigurableJointMotion.Locked)
		{
			actions.z = rotations.z / joint.angularZLimit.limit;
		}

		AssertCorrectAction(actions.x, rotations.x, joint.name + " x");
		AssertCorrectAction(actions.y, rotations.y, joint.name + " y");
		AssertCorrectAction(actions.z, rotations.z, joint.name + " z");

		return actions;

	}

	public void AssertCorrectAction(float action, float rotation, string name)
	{
		if (!(-1 <= action && action <= 1))
		{
			Debug.Log(name + " action value : " + action + " rotation angle : " + rotation);
			Debug.Break();
		}
	}

	public float SmallestAngle(float angle)
	{
		var smallerAngle = angle - 360;
		return Mathf.Abs(angle) > Mathf.Abs(smallerAngle) ? smallerAngle : angle;
	}

    public void SetDrive(float springCoefficient = 1f, float damperCoefficient = 1f)
    {
		var drive = joint.slerpDrive;
		drive.positionSpring = p.rotationSpring * springCoefficient * p.torqueFactor;
		drive.positionDamper = p.rotationDamper * damperCoefficient * p.torqueFactor;
		joint.slerpDrive = drive;
	}

	public Vector3 GetLocalRotationActions(Quaternion localRotation)
    {
		Quaternion targetRotation = joint.GetTargetRotationLocal(localRotation, startLocalRotation);
		return GetTargetRotationActions(targetRotation);
	}

	public float GetConsumedEnergy()
	{
		return joint ? joint.currentTorque.magnitude : 0;
	}

    private void CalculateVelocity()
    {
		realVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
		lastPosition = transform.position;
	}

    public bool IsBroken()
    {
        return joint == null;
    }

    public void AddChild(BotJoint child)
    {
        if(children == null) children = new List<BotJoint>();
        children.Add(child);
    }

	public void SetFreezing(bool freezing)
	{
		if(freezing) body.constraints = RigidbodyConstraints.FreezeAll;
	}

	public void ApplyDamage(Collision collision)
    {
        if (collision.rigidbody && collision.rigidbody.TryGetComponent<DamageStrength>(out var damage))
        {
            var deduction = 4f * damage.strength * collision.rigidbody.mass * collision.relativeVelocity.sqrMagnitude;
            DeductBreakForce(joint, deduction);
            if (children != null)
            {
                foreach (var child in children)
                {
                    DeductBreakForce(child.joint, deduction);
                }
            }
        }
    }

    public void DeductBreakForce(ConfigurableJoint joint, float deduction)
    {
        if (joint == null) return;
        joint.breakForce -= deduction;
        joint.breakTorque -= deduction;

        if (joint.breakForce < 0 || joint.breakTorque < 0)
        {
            Destroy(joint);
        }
    }

	void OnCollisionEnter(Collision collision)
	{
		collisionCount++;

		ApplyDamage(collision);
	}

	void OnCollisionExit()
    {
        collisionCount--;
    }

	void OnTriggerEnter()
    {
		// if making demonstrations, set to all contact pairs in settings since will be kinematic
		collisionCount++;
	}

	void OnTriggerExit()
	{
		collisionCount--;
	}

	void OnJointBreak(float breakForce)
	{
		var layer = gameObject.layer = LayerMask.NameToLayer("Default");
		foreach (Transform child in GetComponentsInChildren<Transform>())
		{
			child.gameObject.layer = layer;
		}
	}

	private void OnEnable()
	{
		if (joint)
		{
			joint.axis = joint.axis;
			joint.secondaryAxis = joint.secondaryAxis;
		}
	}

}
