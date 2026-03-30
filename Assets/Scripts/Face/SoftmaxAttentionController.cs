using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoftmaxAttentionController : MonoBehaviour
{
    [SerializeField]
    private SaliencyController imageSaliencyController;
    [SerializeField]
    private OpticalFlowController motionSaliencyController;

    [SerializeField]
    private List<FixationObject> fixationObjects;

    [SerializeField]
    private FixationObject currentFocus;

    private void Start()
    {
        fixationObjects = new List<FixationObject>();
    }

    void Update()
    {
        //Collect objects
        //Calculate their scores
        //Sample using softmax
        //Profit: choose that target and see how we're going to switch
        //Like maybe the current target can gain a priority boost on their score when they're chosen, but then we start to apply an IOR
    }
}
