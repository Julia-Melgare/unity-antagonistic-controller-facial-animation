using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.XR;

public class VisionTraining : MonoBehaviour
{

	private BotParameters p;
	private VisionRays rays;
	private BufferSensorComponent sensorComponent;

	void Awake()
	{
		p = GetComponent<BotParameters>();
		rays = GetComponent<VisionRays>();
		sensorComponent = GetComponent<BufferSensorComponent>();

		AgentEvents events = GetComponent<AgentEvents>();
		events.collectObservationsEvent += CollectObservations;
	}

	/*
	public void CollectObservations(VectorSensor _)
	{
		rays.Clear();

		var direction = p.target.position - rays.eye.position;
		direction.y = 0;
		direction = Vector3.RotateTowards(direction, Vector3.down, Mathf.PI / 3f, Mathf.Infinity);
		if (p.targetSpeed == 0)
		{
			direction = Vector3.down;
		}

		rays.CastRay(direction);

	}
	*/

	public void CollectObservations(VectorSensor _)
	{
		rays.Clear();

		for(int i = 0; i < sensorComponent.MaxNumObservables; i++)
		{
			var direction = Random.onUnitSphere;
			rays.CastRay(direction);
		}
	}

}
