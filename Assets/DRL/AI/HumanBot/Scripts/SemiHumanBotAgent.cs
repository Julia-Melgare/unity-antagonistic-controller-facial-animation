using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class SemiHumanBotAgent : HumanBotAgent
{

	public bool mirroringObservations = false;
	public bool mirroringActions = false;

	private List<BotJoint> sideJoints;

	public override void Initialize()
	{
		base.Initialize();

		if (mirroringObservations) referential = new SymmetricReferential(referential.root, referential);

		SetupJoints();

		//FreezeAll();

	}

	public override void OnActionReceived(ActionBuffers action)
	{

		int i = 0;
		foreach (BotJoint joint in controlledJoints)
		{
			joint.OnActionReceived(ref i, action);
		}

		//Debug.Log(i);
	}

	public void FreezeAll()
	{
		body.constraints = RigidbodyConstraints.FreezeAll;
		foreach (var joint in joints)
		{
			joint.body.constraints = RigidbodyConstraints.FreezeAll;
		}
	}

	public void SetupJoints()
	{
		joints = new List<BotJoint>();
		controlledJoints = new List<BotJoint>();

		// Setup side joints
		sideJoints = mirroringActions ? leftJoints : rightJoints;

		// Setup observation joints
		if (mirroringObservations)
		{
			joints.AddRange(centerJoints);
			joints.AddRange(rightJoints);
			joints.AddRange(leftJoints);
		}
		else
		{
			joints.AddRange(centerJoints);
			joints.AddRange(leftJoints);
			joints.AddRange(rightJoints);
		}

		// Setup action joints
		controlledJoints.AddRange(sideJoints);
		if (!mirroringActions) controlledJoints.AddRange(centerJoints);

		// Initialize joints that will be controlled by this agent
		foreach (var joint in controlledJoints)
		{
			joint.Initialize(p);
		}

		joints.TrimExcess();
		controlledJoints.TrimExcess();

	}

}