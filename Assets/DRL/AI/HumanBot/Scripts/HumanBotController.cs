using System.Text.RegularExpressions;
using UnityEngine;

public class HumanBotController : MonoBehaviour
{

	public new Camera camera;

	private HumanBotAgent agent;
    private HumanBotParameters p;
	private GenericPlayerInput input;

    private Transform target;

    private float speed;

	void Start()
    {
        agent = GetComponent<HumanBotAgent>();
        p = GetComponent<HumanBotParameters>();
		input = GenericPlayerInput.GetInput(gameObject);
        var targ = new GameObject();
        target = targ.transform;
		p.target = target;

        speed = p.targetSpeed;
	}

    void FixedUpdate()
    {
        var moveAxis = input.GetMovementAxis();
        if(moveAxis.magnitude == 0f)
        {
            p.targetSpeed = 0f;
            return;
        } else
        {
            p.targetSpeed = speed;
        }

        var axis = camera.transform.forward;
        axis.y = 0;
        var normal = axis.normalized;
        Vector3 tangent = new Vector3(normal.z, 0, -normal.x);
		Vector3 axe = (moveAxis.x * tangent + moveAxis.y * normal).normalized;
		target.position = p.root.transform.position + axe * 10f;
	}
}
