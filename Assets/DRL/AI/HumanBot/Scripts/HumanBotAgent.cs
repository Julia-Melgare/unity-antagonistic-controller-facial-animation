using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

using static Unity.MLAgents.StatAggregationMethod;

public class HumanBotAgent : JointAgent
{

	[System.NonSerialized]
	public new HumanBotParameters p;

    protected DuelMaster duelEnv;

	protected float heightFactor;

	protected bool dying = false;
    protected int dyingFrames = 0;

    protected float deathReward = 0;
    protected float winReward = 0;
	protected bool won = false;
	protected bool dead = false;

    protected Referential rootReferential;

	private float speed;

    private int arcFrameCounter = 0;

	private Vector3 previousRootPosition;
	private Vector3 previousTargetPosition;
	private float previousTargetSpeed;
	private Vector3 previousTargetSpeedVector;

	public override void Initialize()
    {
        base.Initialize();

        p = GetComponent<HumanBotParameters>();

		rootReferential = new UnorthogonalReferential(p.root.transform);
        referential = rootReferential;

		heightFactor = p.height / EstimateHeight();

		if (p.environment is DuelMaster)
        {
            duelEnv = (DuelMaster)p.environment;
        }

        speed = p.targetSpeed;

		UpdateObjective();
	}

    public override void OnEpisodeBegin()
    {
        RandomSpeed();

		if(pullJoint != null) Destroy(pullJoint);

        arcFrameCounter = 0;
		dyingFrames = 0;

		base.OnEpisodeBegin();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        arcFrameCounter++;

		ArcUpdate();

        KillCondition();

        if (Randomizing() && p.pushing && Random.value <= 0.001f) Push();
		if (Randomizing() && p.pulling) Pull();

	}

	public override void OnEpisodeEnd()
	{
		base.OnEpisodeEnd();

        WinCondition();

		RecordStat("Win/Win Reward", winReward);
		RecordStat("Death/Death Reward", deathReward);
		RecordStat("Win/Won", won ? 1f : 0f);
		RecordStat("Death/Dead", dead ? 1f : 0f);

		won = false;
		dead = false;
		winReward = 0f;
		deathReward = 0f;
	}

	protected Normalizer headSpringNorm = new Normalizer(2f);
	protected Normalizer headDamperNorm = new Normalizer(1f);
	protected Normalizer rootSpringNorm = new Normalizer(10f);

	protected float maxHeight = 0, minHeight = 10, maxRoot = 0;
	protected Normalizer rootDamperNorm = new Normalizer(1f);

	public virtual void CalculateRewards()
    {
        if (!p.training) return;

        // height error
        float headError = GetHeadError();
        float headSpeed = Vector3.Dot(Vector3.up, p.head.body.velocity);
		float headDamper = headDamperNorm.Normalize(headSpeed) * p.headDamperReward;
        float headSpring = -headSpringNorm.Normalize(headError) * p.headSpringReward;

        RecordStat("Head/Head Spring Error", headError, Histogram);
        RecordStat("Head/Head Damper Speed", headSpeed, Histogram);
		RecordStat("Head/Head Max Speed", headDamperNorm.max);
		RecordStat("Head/Head Max Error", headSpringNorm.max);
		maxHeight = Mathf.Max(maxHeight, p.head.transform.position.y);
		minHeight = Mathf.Min(minHeight, p.head.transform.position.y);
		RecordStat("Head/Head Max Height", maxHeight);
		RecordStat("Head/Head Min Height", minHeight);
		RecordStat("Head/Head Height", GetCurrentHeight());
		RecordStat("Head/Head Spring Reward", headSpring, Histogram);
		RecordStat("Head/Head Damper Reward", headDamper, Histogram);

		dying = IsDying();

        float dyingReward = dying ? p.dyingReward : p.lifeReward;
		RecordStat("Death/Dying Reward", dyingReward);
		RecordStat("Death/Alive", dying ? 0f : 1f);

		// horizontal error
		var rootVelocity = (p.root.transform.position - previousRootPosition) / (Time.fixedDeltaTime * p.decisionPeriod);

        var rootSpeed = Vector3.Dot(rootVelocity, previousTargetSpeedVector.normalized);
        var rootError = Horizontal(previousTargetSpeedVector - rootVelocity).magnitude;
        
        //float rootDamper = -rootDamperNorm.Normalize(Mathf.Min(rootSpeed, p.targetSpeed)) * p.rootReward;
        float rootSpring = -rootSpringNorm.Normalize(rootError) * p.rootReward;
        //if(p.targetSpeed == 0f) rootDamper = -rootDamperNorm.Normalize(p.root.body.velocity.magnitude) * p.rootReward;

		float rootDamper = Mathf.Exp(-rootError*rootError) * p.rootReward;

		float derivative = -2f * rootError * rootDamper;

		RecordStat("Root/Root Max Speed", rootDamperNorm.max);
		RecordStat("Root/Root Max Error", rootSpringNorm.max);
		maxRoot = Mathf.Max(maxRoot, Horizontal(p.root.transform.position).magnitude);
		RecordStat("Root/Root Max Distance", maxRoot);
		RecordStat("Root/Root Spring Error", rootError, Histogram);
		RecordStat("Root/Root Spring Reward", rootSpring, Histogram);

		var horizontalVelocity = Horizontal(rootVelocity).magnitude;
		var totalVelocity = rootVelocity.magnitude;

		RecordStat("Root/Root Speed", rootSpeed, Histogram);
		RecordStat("Root/Root Speed Error", rootError, Histogram);
		RecordStat("Root/Root Damper Reward", rootDamper, Histogram);
		RecordStat("Root/Root Damper Reward Derivative", derivative, Histogram);
        RecordStat("Root/Root Total Velocity", totalVelocity, Histogram);
        RecordStat("Root/Root Horizontal Velocity", horizontalVelocity, Histogram);

		RecordStat("Root/Root Speed " + previousTargetSpeed + "m.s", rootSpeed, Histogram);
		RecordStat("Root/Root Speed Error " + previousTargetSpeed + "m.s", rootError, Histogram);
		RecordStat("Root/Root Damper Reward " + previousTargetSpeed + "m.s", rootDamper, Histogram);
		RecordStat("Root/Root Damper Reward Derivative " + previousTargetSpeed + "m.s", derivative, Histogram);
		RecordStat("Root/Root Total Velocity " + previousTargetSpeed + "m.s", totalVelocity, Histogram);
        RecordStat("Root/Root Horizontal Velocity " + previousTargetSpeed + "m.s", horizontalVelocity, Histogram);

		RecordStat("Root/Root Target Speed", previousTargetSpeed, Histogram);

		// Add rewards
		double reward = 0;

        // If dying
        reward += dyingReward;

        if(dying)
        {
			// Get head back up
			reward += headSpring;
			reward += headDamper;
		}

        // Get root to target
        //reward += rootSpring;
        reward += rootDamper;

		// scale reward with period of decisions/rewards
		reward *= p.decisionPeriod;

		RecordStat("Environment/Total Frame Reward", (float)reward, Histogram);
		if(dying)
		{
			RecordStat("Environment/Total Dying Frame Reward", (float)reward, Histogram);
		} else
		{
			RecordStat("Environment/Total Alive Frame Reward", (float)reward, Histogram);
		}

		AddReward((float) reward);
    }

	public virtual void CollectImitatedObservations(VectorSensor sensor)
	{
		Vector3 targetSpeed = Horizontal(p.target.position - p.root.transform.position).normalized * p.targetSpeed;

		Vector3 speed = Horizontal(p.root.body.velocity);

		var diff = targetSpeed - speed;

		if (p.animating)
		{
			diff = Vector3.zero;
		}

		diff.y = GetHeadError();

		sensor.AddObservation(referential.InverseTransformVector(diff));
        sensor.AddObservation(p.targetSpeed);
		sensor.AddObservation(dying);

        base.CollectObservations(sensor);

	}

	public override void CollectObservations(VectorSensor sensor)
    {

        CalculateRewards();
        
        UpdateObjective();
        
        CollectImitatedObservations(sensor);

    }

	public void UpdateObjective()
	{
		previousRootPosition = p.root.transform.position;
        previousTargetPosition = p.target.position;
        previousTargetSpeed = p.targetSpeed;
		previousTargetSpeedVector = Horizontal(p.target.position - p.root.transform.position).normalized * previousTargetSpeed;
	}

    public override void SetTarget()
    {
        if (p.targetMover)
        {
            Vector3 targetPosition = p.head.transform.position;
            if(Randomizing() && p.randomTarget) {
				p.targetMover.randomRadius(targetPosition, 20, 30);
            } else
            {
                p.target.position = targetPosition + Vector3.forward * 8;
            }
        }
    }

    public void RandomSpeed()
    {
		if (Randomizing() && p.randomSpeed)
		{
			float rand = Random.value;
			float i = 0;
			foreach(float speed in p.speeds)
			{
				i++;
				if(rand <= i/p.speeds.Length)
				{
					p.targetSpeed = speed;
					break;
				}
			}
		}
	}

	public override void Push()
	{
		float strength = 1f * p.totalMass;
		Vector3 force = Random.insideUnitSphere * strength;
		Vector3 torque = Random.insideUnitSphere * strength;
		Vector3 position = p.root.transform.position;
		position.y = Random.Range(
			Mathf.Min(p.rightFoot.transform.position.y, p.leftFoot.transform.position.y),
			p.head.transform.position.y);
		p.root.body.AddForceAtPosition(force, position, ForceMode.Impulse);
		p.root.body.AddTorque(torque, ForceMode.Impulse);
	}

    ConfigurableJoint pullJoint;
    float pullTimer = -1f;

    public void Pull()
    {
        if (pullJoint == null && Random.value <= 0.001f)
        {
			int i = Random.Range(0, joints.Count + 1);
			Rigidbody rb = i == joints.Count ? body : joints[i].body;
			pullJoint = rb.gameObject.AddComponent<ConfigurableJoint>();

			pullJoint.xMotion = ConfigurableJointMotion.Free;
			pullJoint.yMotion = ConfigurableJointMotion.Free;
			pullJoint.zMotion = ConfigurableJointMotion.Free;
			pullJoint.angularXMotion = ConfigurableJointMotion.Free;
			pullJoint.angularYMotion = ConfigurableJointMotion.Free;
			pullJoint.angularZMotion = ConfigurableJointMotion.Free;

			var drive = new JointDrive();
			drive.maximumForce = 10000f * rb.mass;
			drive.positionSpring = 100f * rb.mass;
			drive.positionDamper = 10f * rb.mass;
			
			pullJoint.xDrive = drive;
			pullJoint.yDrive = drive;
			pullJoint.zDrive = drive;
			pullJoint.slerpDrive = drive;

			pullJoint.rotationDriveMode = RotationDriveMode.Slerp;

			pullJoint.targetRotation = Random.rotationUniform;
			pullJoint.targetPosition = Random.insideUnitSphere * 0.2f;

            pullTimer = Time.time + Random.value*2;
		}

        if(pullJoint != null && pullTimer < Time.time)
        {
			Destroy(pullJoint);
		}

    }

	public virtual void ArcUpdate()
	{
		if (p.training && arcFrameCounter > p.arcFrames)
		{
			arcFrameCounter = 0;

			SetTarget();
			RandomSpeed();
		}
	}

	public virtual void WinCondition()
    {
		if(!dead)
		{
			Win();
		}
	}

	public void Win()
	{
		float reward = WinRewards();

		AddReward(reward);
        
        won = true;
	}

	public virtual float WinRewards()
    {
        if (!p.training) return 0f;

		float reward = 0f;

		float winR = p.winReward;
		winReward += winR;
		reward += winR;

		float energyPerFrame = consumedEnergy / frameCounter;
        float energyReward = -energyPerFrame * p.energyPenalty;
		RecordStat("Energy/Total Consumed Energy", consumedEnergy, Histogram);
		RecordStat("Energy/Frame Consumed Energy", energyPerFrame, Histogram);
		RecordStat("Energy/Energy Reward", energyReward, Histogram);
		reward += energyReward;

		consumedEnergy = 0;

		RecordStat("Win/Total Win Reward", reward, Histogram);

		return reward;
	}


	public void KillCondition()
    {
		// if not standing up high enough
		dying = IsDying();

		if (dying && p.dieOnFall)
		{
			dyingFrames++;
			if (dyingFrames > p.maxDyingFrames)
			{
				Kill();
			}
		}
		else
		{
			dyingFrames = 0;
		}
	}

	public void Kill()
	{
		float reward = DeathRewards();

		AddReward(reward);

		dead = true;

		OnEpisodeEnd();
		EndEpisode();

		//if (!p.training) Destroy(gameObject);
	}

	public float DeathRewards()
    {
		deathReward = p.deathPenalty;

		//float lifeReward = frameCounter;
		RecordStat("Death/Death Frames", frameCounter, Histogram);
		//RecordStat("Death/Life Reward", lifeReward);

		float reward = deathReward;

		RecordStat("Death/Total Death Reward", reward);

		return reward;
	}

    private float GetHeadError()
    {
        CheckJoint(ref p.head);
		var headError = (p.height - GetCurrentHeight())/p.height;
        return Mathf.Clamp01(headError);
    }

	private float EstimateHeight()
	{
		var rightDistance = Vector3.Dot(p.head.transform.position - p.rightFoot.transform.position, Vector3.up);
		var leftDistance = Vector3.Dot(p.head.transform.position - p.leftFoot.transform.position, Vector3.up);

		var height = Mathf.Max(rightDistance, leftDistance, 0f);

		return height;
	}

    public float GetCurrentHeight()
    {
		return heightFactor*EstimateHeight();
    }

    public bool IsDying()
    {
        return GetCurrentHeight() < p.dyingHeightFactor * p.height;
    }

	public override void UpdateParameters()
	{
		base.UpdateParameters();

		p = GetComponent<HumanBotParameters>();

		if (p.training)
		{
			UpdateParameter("rootReward", ref p.rootReward);
			UpdateParameter("headSpringReward", ref p.headSpringReward);
			UpdateParameter("headDamperReward", ref p.headDamperReward);
			UpdateParameter("winReward", ref p.winReward);
			UpdateParameter("deathPenalty", ref p.deathPenalty);
			UpdateParameter("dyingHeightFactor", ref p.dyingHeightFactor);
			p.maxDyingFrames = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("maxDyingFrames", p.maxDyingFrames);
			UpdateParameter("lifeReward", ref p.lifeReward);
			UpdateParameter("dyingReward", ref p.dyingReward);
		}
	}

}