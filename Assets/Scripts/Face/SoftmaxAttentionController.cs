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
    [SerializeField]
    private PathDirectionObject pathLookAhead;

    [Header("Parameters")]
    public float softmaxTemperature = 1f; // control the “softness” or “peakiness” of the output probability distribution
    public float focusBoost = 20f;
    public float IORFactor = 0.5f;
    public float minFixationTime = 0.2f;
    [SerializeField]
    private List<FixationObject> fixationObjects;

    [SerializeField]
    private FixationObject currentFocus;
    [SerializeField]
    private float currentFixationTime = 0;

    private float timeSinceLastPathLook = 0f;

    private void Start()
    {
        fixationObjects = new List<FixationObject>();
    }

    void Update()
    {
        if (currentFocus != null)
        {
            // Count time since last path look
            if (currentFocus != pathLookAhead.fixationObject)
            {
                timeSinceLastPathLook += Time.deltaTime;
            }
            else
            {
                timeSinceLastPathLook = 0f;
            }
            // Count fixation time
            currentFixationTime += Time.deltaTime;
            // Make current focus less interesting over time
            currentFocus.currentIOR += IORFactor;
        }

        fixationObjects.Clear();

        //Collect objects
        fixationObjects.AddRange(imageSaliencyController.GetSalientObjects());
        fixationObjects.AddRange(motionSaliencyController.GetSalientObjects());
        fixationObjects.Add(pathLookAhead.fixationObject);
        if (fixationObjects.Count() <= 0) return;

        //Collect their scores
        float[] scores = (from fixationObject in fixationObjects select fixationObject.GetSaliencyScore()).ToArray();
        scores.Append(pathLookAhead.GetGroundSlopeAngle() * timeSinceLastPathLook); // TODO: Make sure this value is normalized between 0 and 1
        Debug.Log("[Softmax Attetion] score list: "+ string.Join(',', scores));

        //Sample using softmax
        float[] scores_probs = SoftmaxFunction.Softmax(scores, softmaxTemperature);
        Debug.Log("[Softmax Attetion] scores probabilities: "+ string.Join(',', scores_probs));
        int targetIndex = SoftmaxFunction.SoftmaxSample(scores, softmaxTemperature);

        //Choose the next target and see if it's a different object than what we're currently looking at
        FixationObject nextTarget = fixationObjects.ElementAt(targetIndex);
        Debug.Log("[Softmax Attention] chosen target: "+ nextTarget.gameObject.name);
        if (nextTarget != currentFocus && currentFixationTime >= minFixationTime) //If we are switching targets
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
