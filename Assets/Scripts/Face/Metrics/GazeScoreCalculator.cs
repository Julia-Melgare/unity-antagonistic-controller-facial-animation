using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class GazeScoreCalculator : MonoBehaviour
{
    [SerializeField]
    private AttentionController attentionController;

    [SerializeField]
    private int runDuration = 2365;

    [SerializeField]
    private List<GameObject> relevantObjects;

    [Header("Metrics")]
    [SerializeField]
    private int totalFixations = 0; // can be either frames or dependant on time? I think we should just count frames for now?

    [SerializeField]
    private Dictionary<GameObject, int> fixationsPerRelevantObject; // frames where the agent focused on each relevant object

    void Start()
    {
        fixationsPerRelevantObject = relevantObjects.ToDictionary(k => k, k => 0); // initialize dictionary with relevant objects and zero as value        
    }

    // Update is called once per frame
    void Update()
    {
        if (totalFixations >= runDuration)
            UnityEditor.EditorApplication.isPlaying = false;

        // check current attentionController fixation and count the key in dict if its one of the relevant objects
        GameObject currentFixationTarget = attentionController.GetCurrentFocus().gameObject;
        if (fixationsPerRelevantObject.ContainsKey(currentFixationTarget))
        {
            fixationsPerRelevantObject[currentFixationTarget]++;
        }
        totalFixations++;
    }

    void OnApplicationQuit()
    {
        Debug.Log("-------- Gaze Score Metrics --------");
        Debug.Log("Total simulation frames: "+totalFixations);
        int totalRelevantFixations = 0;
        foreach (var obj in fixationsPerRelevantObject)
        {
            Debug.Log("Frames focusing on " + obj.Key.name + ": " + obj.Value + "(" + ((float)obj.Value/totalFixations) + ")");
            totalRelevantFixations += obj.Value;
        }

        Debug.Log("Overall Gaze Score: " + ((float)totalRelevantFixations/totalFixations));
    }
}
