using Unity.MLAgents.Sensors;
using UnityEngine;

using static Unity.MLAgents.StatAggregationMethod;

public class AttackerHumanBotAgent : HumanBotAgent
{

    public BotJoint attacker;
    public Transform attackerTip;

    protected bool attacking = false;

    protected Normalizer attackSpringNorm = new Normalizer(2f);
    protected Normalizer attackDamperNorm = new Normalizer(10f);

    public override void CalculateRewards()
    {
        if (!p.training) return;

        base.CalculateRewards();

        // is near enough to attack
        var rootError = Horizontal(p.target.position - p.root.transform.position).magnitude;

        attacking = rootError <= 1.5f;

        float attackingReward = attacking ? 1f : 0f;

        // attack error
        GetAttackErrorVelocity(out float attackError, out float attackSpeed);
        float attackDamper = attackDamperNorm.Normalize(attackSpeed);
        float attackSpring = -attackSpringNorm.Normalize(attackError);

        // Add rewards
        double reward = 0;

        // If close enough to attack
        reward += attackingReward;
        RecordStat("Attack/Attacking Reward", attackingReward);

        if (attacking)
        {
            // Get hand to target
            reward += attackSpring;
            reward += attackDamper;
            RecordStat("Attack/Attack Spring Error", attackError, Histogram);
            RecordStat("Attack/Attack Damper Speed", attackSpeed, Histogram);
            RecordStat("Attack/Attack Spring Reward", attackSpring, Histogram);
            RecordStat("Attack/Attack Damper Reward", attackDamper, Histogram);
        }

        // scale reward with period of decisions/rewards
        reward *= p.decisionPeriod;

		AddReward((float) reward);
    }

	public override void CollectObservations(VectorSensor sensor)
	{

        base.CollectObservations(sensor);

		sensor.AddObservation(attacking);

	}

	public override void WinCondition()
	{
		if (p.training)
		{
            if(p.animating)
            {
                base.WinCondition();
            } else
            {
				GetAttackErrorVelocity(out float attackError, out float attackSpeed);
				if (attackError < p.winDistance)
				{
					Win();
				}
			}
		}
	}

	public override float WinRewards()
    {
		if (!p.training) return 0f;

		float reward = base.WinRewards();

		GetAttackErrorVelocity(out float attackError, out float attackSpeed);
        float handSpeedReward = attackSpeed * 10f;
        RecordStat("Attack/Attack Win Speed", attackSpeed);
        RecordStat("Attack/Attack Speed Reward", handSpeedReward);
		reward += handSpeedReward;

        return reward;
    }

    public Vector3 GetAttackError()
    {
        CheckJoint(ref attacker);
        Vector3 attackVector = p.target.position - attackerTip.position;
        attackVector.y = p.head.transform.position.y - GetCurrentHeight() + p.height - attackerTip.position.y;
        return attackVector;
    }

    public void GetAttackErrorVelocity(out float out_error, out float out_speed)
    {
        GetErrorVelocity(GetAttackError(), attacker.body.GetPointVelocity(attackerTip.position), out out_error, out out_speed);
    }

}