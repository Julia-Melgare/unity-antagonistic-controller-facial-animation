using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class GazeTargetCalculator : MonoBehaviour
{
    [SerializeField]
    private AttentionController attentionController;

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
        // check current attentionController fixation and count the key in dict if its one of the relevant objects
        totalFixations++;
    }
}
