using UnityEngine;

public class HumanBotParameters : BotParameters
{
	[Header("Human Bot Parameters")]
	public float height = 1.8f;

	[Header("Reward Factors")]
	public float headSpringReward = 1f;
	public float headDamperReward = 1f;
	public float rootReward = 1f;
	public float lifeReward = 1f;
	public float dyingReward = -3f;

	[Header("Win")]
	public int arcFrames = 100;

	[Header("Death")]
	public bool dieOnFall = false;
	public int maxDyingFrames = 0;
	public float dyingHeightFactor = 0.6f;

	[Header("Body Parts")]
	public BotJoint root;
	public BotJoint head, rightHand, leftHand, rightFoot, leftFoot;
}
