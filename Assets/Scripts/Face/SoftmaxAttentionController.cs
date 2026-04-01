using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoftmaxAttentionController : AttentionController
{
    [Header("Stimuli Inputs")]
    [SerializeField]
    private SaliencyController imageSaliencyController;
    [SerializeField]
    private OpticalFlowController motionSaliencyController;

    [Header("Parameters")]
    public float softmaxTemperature = 1.5f;
    public float focusBoost = 10f;
    public float IORFactor = 0.5f;
    [SerializeField]
    private List<FixationObject> fixationObjects;

    [SerializeField]
    private FixationObject currentFocus;
    [SerializeField]
    private float currentFixationTime = 0;

    private void Start()
    {
        fixationObjects = new List<FixationObject>();
    }

    void Update()
    {
        fixationObjects.Clear();
        //Collect objects
        fixationObjects.AddRange(imageSaliencyController.GetSalientObjects());
        fixationObjects.AddRange(motionSaliencyController.GetSalientObjects());
        if (fixationObjects.Count() <= 0) return;

        //Calculate their scores
        float[] scores = (from fixationObject in fixationObjects select fixationObject.GetSaliencyScore()).ToArray();
        Debug.Log("[Softmax Attetion] score list: "+ string.Join(',', scores));
        //Sample using softmax
        float[] scores_probs = SoftmaxFunction.Softmax(scores, softmaxTemperature);
        Debug.Log("[Softmax Attetion] scores probabilities: "+ string.Join(',', scores_probs));
        int targetIndex = SoftmaxFunction.SoftmaxSample(scores, softmaxTemperature);
        currentFocus = fixationObjects.ElementAt(targetIndex);
        Debug.Log("[Softmax Attention] chosen target: "+ currentFocus.gameObject.name);
        //Profit: choose that target and see how we're going to switch
        //Like maybe the current target can gain a priority boost on their score when they're chosen, but then we start to apply an IOR
    }

    public override FixationObject GetCurrentFocus()
    {
        return currentFocus;
    }

    public override float GetCurrentFixationTime()
    {
        return currentFixationTime;
    }
}
