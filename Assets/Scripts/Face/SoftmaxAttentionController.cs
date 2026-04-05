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
    public float focusBoost = 20f;
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
        if (currentFocus != null)
        {
            // Count fixation time
            currentFixationTime += Time.deltaTime;
            // Make current focus less interesting over time
            currentFocus.currentIOR += IORFactor;
        }

        fixationObjects.Clear();

        //Collect objects
        fixationObjects.AddRange(imageSaliencyController.GetSalientObjects());
        fixationObjects.AddRange(motionSaliencyController.GetSalientObjects());
        if (fixationObjects.Count() <= 0) return;

        //Collect their scores
        float[] scores = (from fixationObject in fixationObjects select fixationObject.GetSaliencyScore()).ToArray();
        Debug.Log("[Softmax Attetion] score list: "+ string.Join(',', scores));

        //Sample using softmax
        float[] scores_probs = SoftmaxFunction.Softmax(scores, softmaxTemperature);
        Debug.Log("[Softmax Attetion] scores probabilities: "+ string.Join(',', scores_probs));
        int targetIndex = SoftmaxFunction.SoftmaxSample(scores, softmaxTemperature);

        //Choose the next target and see if it's a different object than what we're currently looking at
        FixationObject nextTarget = fixationObjects.ElementAt(targetIndex);
        Debug.Log("[Softmax Attention] chosen target: "+ nextTarget.gameObject.name);
        if (nextTarget != currentFocus) //If we are switching targets
        {
            //Reset current target modifiers
            currentFocus.scoreBoost = 0f;
            currentFocus.currentIOR = 0f;
            currentFixationTime = 0f;
            //Switch target and apply score boost to keep focus
            currentFocus = nextTarget;
            currentFocus.scoreBoost = focusBoost;
        }
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
