using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class DemonstrationAnimator : StateMachineBehaviour
{

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var agent = animator.GetComponent<Agent>();
        if (agent != null)
        {
			agent.EndEpisode();
		}
    }

}
