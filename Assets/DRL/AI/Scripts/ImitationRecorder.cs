using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.Assertions;

public class ImitationRecorder : MonoBehaviour
{

	public bool record = false;

	public RuntimeAnimatorController[] controllers;
	private int currentAnimationIndex = 0;

	public float timeScale = 1f;

	JointAgent agent;
	GameObject target;
	Transform targetTransform;
	BehaviorParameters behavior;
	Animator animator;

	float baseSpeed = 0;

	void Start()
    {

		agent = transform.GetComponent<JointAgent>();
		Assert.IsNotNull(agent);

		behavior = transform.GetComponent<BehaviorParameters>();

		agent.p.training = true;
		agent.MaxStep = 0;
		agent.IgnoreCollisions();
		behavior.BehaviorType = BehaviorType.HeuristicOnly;
		agent.p.torqueFactor = 0f;

		baseSpeed = agent.p.targetSpeed;

		// Copy target human bot

		// preset to animating before instantiating copy
		agent.SetAnimating(true);
		this.enabled = false;

		target = GameObject.Instantiate(gameObject, Vector3.zero, Quaternion.identity);
		Assert.IsNotNull(target);

		this.enabled = true;

		//reset main agent to not animating
		agent.SetAnimating(false);

		targetTransform = target.GetComponent<Transform>();
		Assert.IsNotNull(targetTransform);

		animator = target.GetComponent<Animator>();
		Assert.IsNotNull(animator);

		Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Structure"), gameObject.layer);

		//floor.GetComponent<Collider>().isTrigger = true;
		//floor.layer = LayerMask.NameToLayer("NonCollide");
		//Physics.gravity *= 0f;

		agent.body.isKinematic = true;
		//agent.body.constraints = RigidbodyConstraints.FreezeAll;

		foreach (var joint in agent.joints)
		{
			joint.joint.projectionMode = JointProjectionMode.PositionAndRotation;
			joint.joint.projectionDistance = 0.01f;
		}

		agent.SetRecording(record);

		PickAnimation();

		FixedUpdate();

		Time.timeScale = timeScale;
	}

    void FixedUpdate()
    {
		MoveTransform(targetTransform, transform);
	}

	public void MoveTransform(Transform source, Transform destination)
	{

		var body = destination.GetComponent<Rigidbody>();
		if (body)
		{
			body.isKinematic = true;
			body.MovePosition(source.position);
			body.MoveRotation(source.rotation);
			var joint = destination.GetComponent<BotJoint>();
			if(joint) CopyAction(source, joint);
		}

		foreach (Transform tf in source)
		{
			Transform eq = destination.Find(tf.gameObject.name);
			Assert.IsNotNull(eq);
			MoveTransform(tf, eq);
		}
	}

	void PickAnimation()
	{
		currentAnimationIndex = (currentAnimationIndex + 1) % controllers.Length;

		animator.runtimeAnimatorController = controllers[currentAnimationIndex];

		agent.p.targetSpeed = currentAnimationIndex == 0 ? 0f : baseSpeed;
	}

	void OnEpisodeEnd(GameObject obj)
	{
		if (obj != gameObject) return;

		PickAnimation();
	}

	private void OnEnable()
	{
		Events.destroyEvent += OnEpisodeEnd;
	}

	private void OnDisable()
	{
		Events.destroyEvent -= OnEpisodeEnd;
	}

	public void CopyActions(Transform source, JointAgent agent)
	{
		foreach (var joint in agent.joints)
		{
			Transform reference = null;
			foreach (Transform tf in source.GetComponentsInChildren<Transform>())
			{
				if (tf.gameObject.name.Equals(joint.gameObject.name))
				{
					reference = tf;
				}
			}
			Assert.IsNotNull(reference);

			CopyAction(reference, joint);

		}
	}

	public void CopyAction(Transform source, BotJoint joint)
	{
		//AssertReversibleEulerAngles(source.localEulerAngles);
		//AssertReversibleActions(joint, joint.name);

		joint.configuring = true;

		joint.SetDrive();

		Vector3 actions = joint.GetLocalRotationActions(source.localRotation);
		joint.actionX = actions.x;
		joint.actionY = actions.y;
		joint.actionZ = actions.z;
	}

	private float e = 0.00001f;

	

	public void AssertReversibleActions(BotJoint joint, string name)
	{
		Vector3 actions = joint.GetTargetRotationActions(joint.GetTargetRotation());
		UnityEngine.Assertions.Assert.AreApproximatelyEqual(actions.x, joint.actionX, e, name + " x");
		UnityEngine.Assertions.Assert.AreApproximatelyEqual(actions.y, joint.actionY, e, name + " y");
		UnityEngine.Assertions.Assert.AreApproximatelyEqual(actions.z, joint.actionZ, e, name + " z");
	}

	public void AssertReversibleEulerAngles(Vector3 eulerAngles)
	{
		Vector3 otherAngles = Quaternion.Euler(eulerAngles).eulerAngles;
		UnityEngine.Assertions.Assert.AreApproximatelyEqual(eulerAngles.x, otherAngles.x, e, "x");
		UnityEngine.Assertions.Assert.AreApproximatelyEqual(eulerAngles.y, otherAngles.y, e, "y");
		UnityEngine.Assertions.Assert.AreApproximatelyEqual(eulerAngles.z, otherAngles.z, e, "z");
	}

}
