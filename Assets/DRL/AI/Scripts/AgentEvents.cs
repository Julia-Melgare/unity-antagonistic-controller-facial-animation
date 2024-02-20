using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class AgentEvents : MonoBehaviour
{
	public delegate void OnEpisodeBeginEnd();
	public event OnEpisodeBeginEnd beginEpisodeEvent;
	public event OnEpisodeBeginEnd endEpisodeEvent;
	public void SignalBeginEpisode()
	{
		if (beginEpisodeEvent != null) beginEpisodeEvent();
	}
	public void SignalEndEpisode()
	{
		if (endEpisodeEvent != null) endEpisodeEvent();
	}

	public delegate void OnCollectObservations(VectorSensor sensor);
	public event OnCollectObservations collectObservationsEvent;
	public void SignalCollectObservations(VectorSensor sensor)
	{
		if (collectObservationsEvent != null) collectObservationsEvent(sensor);
	}

	public delegate void OnActionReceived(ActionBuffers action);
	public event OnActionReceived actionReceivedEvent;
	public void SignalActionReceived(ActionBuffers action)
	{
		if (actionReceivedEvent != null) actionReceivedEvent(action);
	}
}
