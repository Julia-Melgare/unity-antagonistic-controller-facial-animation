using System;
using Unity.MLAgents;
using UnityEngine;

public class BotParameters : MonoBehaviour
{

	[Header("Training")]
	public int maxFrames = 0;
	public bool training = false;
	public bool randomizing = false;
	public bool randomTarget = false;
	public bool randomInit = false;
	public bool pushing = false;
	public bool pulling = false;
	public bool randomSpeed = false;
	public float[] speeds;

	[Header("Behavior")]
	public bool recording = false;
	public bool animating = false;
	public bool freezing = false;
	public bool kinematic = false;
	public bool immobile = false;
	public int decisionPeriod = 5;

	[Header("Reward")]
	public float rewardFactor = 1f;
	public float winDistance = 0.5f;
	public float energyPenalty = 1f;
	public float winReward = 1000f;
	public float deathPenalty = -1000f;
	public float targetSpeed = 1.37f;

	[Header("Mass")]
	public float totalMass = 80f;
	public float bodyPercentage = 0f;

	[Header("Forces")]
	public float torqueFactor = 1f;
	public float rotationSpring = 100f;
	public float rotationDamper = 10f;
	public float rotationSpeedFactor = 1f;
	public float maxTorque = 1000f;
	public float maxAngularVelocity = 10;
	public bool springControl = true;

	public float breakTorque = float.PositiveInfinity;
	public float breakForce = float.PositiveInfinity;

	[Header("Components")]
	public Transform target;
	[NonSerialized]
	public TargetMover targetMover = null;
	public Env environment = null;

	[NonSerialized]
	public BotAgent agent;
	public Animator animator = null;

}
