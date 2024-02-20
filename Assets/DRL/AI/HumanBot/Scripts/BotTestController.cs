using UnityEngine;

using static BotAgent;

public class BotTestController : MonoBehaviour
{

    public float radius = 3f;

    HumanBotAgent agent;
    HumanBotParameters p;

    private float speed;

    void Start()
    {
        agent = GetComponent<HumanBotAgent>();
        p = GetComponent<HumanBotParameters>();
        speed = p.targetSpeed;
    }

    void FixedUpdate()
    {
        StandNearTarget(radius);
	}

    void StandNearTarget(float radius)
    {
		if (Horizontal(agent.p.root.transform.position - agent.p.target.position).magnitude < radius)
		{
			p.targetSpeed = 0;
		}
		else
		{
			p.targetSpeed = speed;
		}
	}
}
