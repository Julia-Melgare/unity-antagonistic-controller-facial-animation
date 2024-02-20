using System;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public abstract class JointAgent : BotAgent
{

	[NonSerialized]
	public List<BotJoint> joints, leftJoints, centerJoints, rightJoints, controlledJoints;

	public override void Initialize()
	{
		base.Initialize();

		GetJoints();

		foreach (BotJoint joint in joints)
		{
			joint.Initialize(p);
		}

		IgnoreCollisions();

		SetFreezing(p.freezing);

	}

	public override void OnEpisodeBegin()
	{
		base.OnEpisodeBegin();

		SetAnimating(p.animating);
		SetRecording(p.recording);

		consumedEnergy = 0;
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();

		if(p.training)
		{
			foreach (BotJoint joint in joints)
			{
				consumedEnergy += joint.GetConsumedEnergy();
			}
		}

	}

	public override void CollectObservations(VectorSensor sensor)
	{

		base.CollectObservations(sensor);

		foreach (BotJoint joint in joints)
		{
			joint.CollectObservations(referential, sensor);
		}

	}

	public override void OnActionReceived(ActionBuffers action)
	{

		base.OnActionReceived(action);

		int i = 0;
		foreach (BotJoint joint in controlledJoints)
		{
			joint.OnActionReceived(ref i, action);
		}
		//Debug.Log(i);
	}

	public override void Restart()
	{
		base.Restart();
		foreach (BotJoint joint in joints)
		{
			joint.Restart();
		}
	}

	public void CheckJoint(ref BotJoint joint)
	{
		if (joint != null) return;

		Transform parent = joint.transform.parent;
		if (parent == null) return;
		while (parent.GetComponent<ConfigurableJoint>() == null)
		{
			parent = parent.parent;
			if (parent == null) return;
		}
		joint = parent.GetComponent<BotJoint>();
	}

	public void IgnoreCollisions()
	{
		if (p.training)
		{
			Physics.IgnoreLayerCollision(gameObject.layer, gameObject.layer, true);
		}
		else
		{
			Collider[] colliders = GetComponentsInChildren<Collider>();
			for (int i = 0; i < colliders.Length; i++)
			{
				for (int j = i + 1; j < colliders.Length; j++)
				{
					Physics.IgnoreCollision(colliders[i], colliders[j], true);
				}
			}
		}

	}

	public virtual void GetJoints()
	{
		var jointArray = GetComponentsInChildren<BotJoint>();

		joints = new List<BotJoint>();
		leftJoints = new List<BotJoint>();
		centerJoints = new List<BotJoint>();
		rightJoints = new List<BotJoint>();
		controlledJoints = new List<BotJoint>();

		foreach (BotJoint joint in jointArray)
		{
			switch (joint.side)
			{
				case BotJoint.SIDE.LEFT:
					leftJoints.Add(joint); break;
				case BotJoint.SIDE.CENTER:
					centerJoints.Add(joint); break;
				case BotJoint.SIDE.RIGHT:
					rightJoints.Add(joint); break;
			}
		}

		joints.AddRange(centerJoints);
		joints.AddRange(rightJoints);
		joints.AddRange(leftJoints);

		controlledJoints.AddRange(centerJoints);
		controlledJoints.AddRange(rightJoints);
		controlledJoints.AddRange(leftJoints);

		joints.TrimExcess();
		leftJoints.TrimExcess();
		centerJoints.TrimExcess();
		rightJoints.TrimExcess();
		controlledJoints.TrimExcess();

	}

	public override void SetKinematic(bool kinematic = true)
	{
		base.SetKinematic(kinematic);
		foreach (var joint in joints)
		{
			joint.body.isKinematic = kinematic;
		}
	}

	public override void SetFreezing(bool freezing)
	{
		base.SetFreezing(freezing);
		foreach (var joint in joints)
		{
			joint.SetFreezing(freezing);
		}
	}

}
