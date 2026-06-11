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
    private MotionSaliencyController motionSaliencyController;
    [SerializeField]
    private PathDirectionObject pathLookAhead;

    [Header("Parameters")]
    public float softmaxTemperature = 1f; // control the “softness” or “peakiness” of the output probability distribution
    public float focusBoost = 20f;
    public float IORFactor = 0.5f;
    public float minFixationTime = 0.2f;
    public float timeSinceLastPathLookModifier = 0.8f;
    
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
            currentFocus.currentIOR += IORFactor * (1 - GetFocusSaliencyScore());
        }

        fixationObjects.Clear();

        //Collect objects
        fixationObjects.AddRange(imageSaliencyController.GetSalientObjects());
        fixationObjects.AddRange(motionSaliencyController.GetSalientObjects());
        fixationObjects.Add(pathLookAhead.fixationObject);
        if (fixationObjects.Count() <= 0) return;

        //Collect their scores
        float[] scores = (from fixationObject in fixationObjects select fixationObject.GetSaliencyScore()).ToArray();

        //Add path score
        scores.Append(pathLookAhead.GetPathSaliencyScore(timeSinceLastPathLook, timeSinceLastPathLookModifier));
        //Debug.Log("[Softmax Attetion] score list: "+ string.Join(',', scores));

        //Sample using softmax
        float[] scores_probs = SoftmaxFunction.Softmax(scores, softmaxTemperature);
        //Debug.Log("[Softmax Attetion] scores probabilities: "+ string.Join(',', scores_probs));
        int targetIndex = SoftmaxFunction.SoftmaxSample(scores, softmaxTemperature);

        //Choose the next target and see if it's a different object than what we're currently looking at
        FixationObject nextTarget = fixationObjects.ElementAt(targetIndex);
        //Debug.Log("[Softmax Attention] chosen target: "+ nextTarget.gameObject.name);
        if (nextTarget != currentFocus && currentFixationTime >= minFixationTime) //If we are switching targets 
        {
            //Reset current target modifiers
            currentFocus.scoreBoost = 0f;
            currentFocus.currentIOR = 0f;
            currentFixationTime = 0f;
            //Switch target and apply score boost to keep focus
            currentFocus = nextTarget;
            currentFocus.scoreBoost = focusBoost; //* Mathf.Pow(GetFocusSaliencyScore(), 2f);
        }
    }

    private float GetFocusSaliencyScore()
    {
        if (currentFocus == pathLookAhead.fixationObject)
        {
            float normalizedSlope = Mathf.Clamp01(pathLookAhead.GetGroundSlopeAngle() / 30.0f);
            float normalizedTimeSinceLastLook = 1.0f - Mathf.Exp(-timeSinceLastPathLookModifier * timeSinceLastPathLook);
            return 0.5f * normalizedSlope + 0.5f * normalizedTimeSinceLastLook;
        }

        return 0.5f * currentFocus.imageSaliencyScore + 0.5f * currentFocus.motionSaliencyScore;
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
