using System;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Assertions;

public class VisionRays : MonoBehaviour
{

	public Transform eye;
	public float visionDistance = 10f;
	public LayerMask visionMask;

	private BotAgent agent;
	private BufferSensorComponent sensorComponent;

	private List<RaycastHit> rays;

	void Awake()
    {
		agent = GetComponent<BotAgent>();

		AgentEvents events = GetComponent<AgentEvents>();
		events.collectObservationsEvent += CollectObservations;

		sensorComponent = GetComponent<BufferSensorComponent>();

		rays = new List<RaycastHit>();
	}

	public void CollectObservations(VectorSensor _)
	{
		float[] obs = new float[sensorComponent.ObservableSize];
		// Might overflow sensorComponent.MaxNumObservable
		foreach (var hit in rays)
		{
			Array.Clear(obs, 0, obs.Length);

			int i = 0;
			var normal = agent.referential.InverseTransformVector(hit.normal);
			obs[i++] = normal.x;
			obs[i++] = normal.y;
			obs[i++] = normal.z;

			var position = agent.referential.InverseTransformVector(hit.point);
			obs[i++] = position.x;
			obs[i++] = position.y;
			obs[i++] = position.z;

			Assert.IsTrue(i == obs.Length);

			sensorComponent.AppendObservation(obs);
		}
	}

	public void AddRay(RaycastHit hit)
	{
		rays.Add(hit);
	}

	public void CastRay(Vector3 direction)
	{
		//Debug.DrawRay(eye.position, direction * 10, Color.blue, agent.p.decisionPeriod * Time.fixedDeltaTime, false);
		if (Physics.Raycast(eye.position, direction, out RaycastHit hitInfo, visionDistance, visionMask))
		{
			AddRay(hitInfo);
			//Debug.DrawRay(hitInfo.point, hitInfo.normal * distance, Color.blue, p.decisionPeriod * Time.fixedDeltaTime, false);
		}
	}

	public void Clear()
	{
		rays.Clear();
	}

}
