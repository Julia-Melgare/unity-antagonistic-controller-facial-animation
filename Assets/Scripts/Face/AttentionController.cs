using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Voxus.Random;

public class AttentionController : MonoBehaviour
{
    [Header("Stimuli Inputs")]
    [SerializeField]
    private SaliencyController saliencyController;
    [SerializeField]
    private OpticalFlowController opticalFlowController;
    [SerializeField]
    private PathDirectionObject pathLookAhead;

    [Header("Attention Settings")]
    [SerializeField]
    private float lookAtPathWeight = 78.8f;
    [SerializeField]
    private float lookAtObjectsWeight = 21.2f;
    [SerializeField]
    private float focusTimeMean = 1.8692f, focusTimeStd = 0.9988f; // Exploratory: mean = 1.8692f, std = 0.9988f; Goal-driven: mean = 0.12128f, std = 0.06778f
    [SerializeField]
    private float saliencyFixationModifier = 0.3f;
    [SerializeField]
    private float lookAtPathTimeMin = 0.2f, lookAtPathTimeMax = 0.5f;
    [SerializeField]
    private float inhibitionOfReturnTime = 0.9f;

    [SerializeField]
    private bool focusOnSalientRegions = true;

    [Header("Path Decision DDM Settings")]
    [SerializeField]
    private float lookAtPathDecisionThreshold = 0.5f;
    [SerializeField]
    private float lookAtObjectsDecisionThreshold = -0.5f;

    [Header("Object Decision DDM Settings")]
    [SerializeField]
    private float decisionThreshold = 0.3f;
    [SerializeField]
    private float noiseLevel = 0.01f;
    [SerializeField]
    private float maxDecisionTime = 0.2f;

    [SerializeField]
    private GameObject currentFocus = null;
    private float currentFixationTime = 0f;
    [SerializeField]
    private Dictionary<int, float> objectsFocusedOn;
    private bool isFocusing = false;
    private bool isChoosingObject = false;
    private float objectDecisionTimer = 0f;

    [SerializeField]
    private List<GameObject> currentObjects;

    [Header("Debug/Visualization")]
    [SerializeField]
    private TextMeshProUGUI currentFocusDebugText;

    private List<GameObject> peripheralObjectsLeft;
    private List<GameObject> peripheralObjectsRight;
    private List<GameObject> salientObjects;
    private Dictionary<GameObject, float> driftPerObject;

    private Coroutine currentFocusRoutine;
    private RandomGaussian fixationTimeDistribution;

    private float timeSinceLastPathLook = 0f;
    private float pathDrift = 0f;
    
    private void Start()
    {
        saliencyController.enabled = focusOnSalientRegions;
        objectsFocusedOn = new Dictionary<int, float>();
        currentObjects = new List<GameObject>();
        peripheralObjectsLeft = new List<GameObject>();
        if (focusOnSalientRegions)
            salientObjects = new List<GameObject>();
        fixationTimeDistribution = new RandomGaussian(focusTimeStd, focusTimeMean); 
    }

    private void Update()
    {
        if (currentFocusDebugText != null) currentFocusDebugText.text = "Current focus: "+(currentFocus != null ? currentFocus.gameObject.name : "none");

        if (!IsFocusingOnPath()) timeSinceLastPathLook += Time.deltaTime; else timeSinceLastPathLook = 0f;
        ComputePathDDM();

        // If the agent is already focusing on something, return
        if (isFocusing) return;

        // If the agent is choosing an object to focus, keep doing that
        if (isChoosingObject)
        {
            if (objectDecisionTimer <= 0) // Time is up, take object that converged the most
            {
                isChoosingObject = false;
                var objDecision = driftPerObject.OrderByDescending(x => x.Value).First().Key;
                var fixationTime = Mathf.Abs(fixationTimeDistribution.Get()) * (GetObjSaliencyScore(objDecision) + saliencyFixationModifier);
                Debug.Log("[Attention] Focusing on object "+objDecision.name+" for "+fixationTime+"s");
                currentFocusRoutine = StartCoroutine(FocusOnObject(objDecision, fixationTime));
                return;
            }
            // Keep computing decision normally
            ChooseObjectStep();
            return;
        }

        // Decide if agent will look at path or at salient objects
        //Debug.Log(pathDrift);
        if (pathDrift >= lookAtPathDecisionThreshold)
        {
            // Focus on the path
            Debug.Log("[PathDDM] Focusing on path");
            currentFocusRoutine = StartCoroutine(FocusOnPath(Random.Range(lookAtPathTimeMin, lookAtPathTimeMax)));
            pathDrift = 0f;
            return;
        }

        if (pathDrift <= lookAtObjectsDecisionThreshold)
        {
            // Focus on the objects
            Debug.Log("[PathDDM] Focusing on object");
            isChoosingObject = true;        
            UpdateCurrentObjects();
            if (currentObjects.Count() > 0)
            {
                driftPerObject = new Dictionary<GameObject, float>();
                foreach (GameObject obj in currentObjects)
                {
                    driftPerObject.Add(obj, 0f);
                }
                objectDecisionTimer = maxDecisionTime;
                ChooseObjectStep();
            }
            else
            {
                //There is no object to look at
                isChoosingObject = false;
            }
            pathDrift = 0f;     
        }        
    }

    private void ComputePathDDM()
    {  
        UpdateCurrentObjects();
        float maxObjSaliency = GetMaxObjectSaliency();
        //Debug.Log("max obj saliency: "+maxObjSaliency);
        //Debug.Log("time since last path look: "+timeSinceLastPathLook);
        pathDrift += (pathLookAhead.GetGroundSlopeAngle() * timeSinceLastPathLook * lookAtPathWeight * Time.deltaTime) - (maxObjSaliency * lookAtObjectsWeight * Time.deltaTime) + Random.Range(0, noiseLevel);
        //Debug.Log("[PathDDM] pathDrift: " + pathDrift);

    }

    private void ChooseObjectStep()
    {
        // Compute DDM step
        GameObject objDecision = ComputeObjectDDM();

        // If an object converged, start focusing on it
        if (objDecision != null)
        {
            isChoosingObject = false;
            var fixationTime = Mathf.Abs(fixationTimeDistribution.Get()) * (GetObjSaliencyScore(objDecision) + saliencyFixationModifier);
            Debug.Log("[Attention] Focusing on object "+objDecision.name+" for "+fixationTime+"s");
            currentFocusRoutine = StartCoroutine(FocusOnObject(objDecision, fixationTime));
        }
        else
        {
            objectDecisionTimer -= Time.deltaTime;
        }
    }
    private GameObject ComputeObjectDDM()
    {
        foreach (GameObject obj in driftPerObject.Keys.ToList())
        {
            if (currentObjects.Contains(obj))
                driftPerObject[obj] += (GetObjSaliencyScore(obj) * Time.deltaTime) + Random.Range(0, noiseLevel);
            else
                driftPerObject[obj] *= 0.9f;
            //Debug.Log("[DDM] ("+(maxDecisionTime - objectDecisionTimer)+"s) "+obj.name+": "+driftPerObject[obj]);
        }
        var max = driftPerObject.OrderByDescending(x => x.Value).First();
        if (max.Value >= decisionThreshold)
        {
            //Debug.Log("[DDM] ("+(maxDecisionTime - objectDecisionTimer)+"s) Choosing object "+max.Key.name);
            return max.Key;
        }
        return null;
    }

    private void UpdateCurrentObjects()
    {
        peripheralObjectsLeft = opticalFlowController.objectsLeft;
        peripheralObjectsRight = opticalFlowController.objectsRight;
        if (focusOnSalientRegions)
            salientObjects = saliencyController.GetSalientObjects();
        
        var currentObjectsSet = new HashSet<GameObject>();
        foreach(GameObject obj in peripheralObjectsLeft)
        {
                currentObjectsSet.Add(obj);
        }

        foreach(GameObject obj in peripheralObjectsRight)
        {
                currentObjectsSet.Add(obj);
        }
            
        if (focusOnSalientRegions)
        {
            foreach(GameObject obj in salientObjects)
            {
                    currentObjectsSet.Add(obj);
            }
                
        }
        currentObjects = new List<GameObject>(currentObjectsSet.ToList());        
    }

    private float GetObjSaliencyScore(GameObject obj)
    {
        return (saliencyController.GetObjectSaliency(obj) * 0.5f + opticalFlowController.GetObjectSpeed(obj) * 1.0f) * (inhibitionOfReturnTime - objectsFocusedOn.GetValueOrDefault(obj.GetInstanceID(), 0f));
    }

    private float GetMaxObjectSaliency()
    {
        float maxObjSaliency = 0f;
        foreach (GameObject obj in currentObjects)
        {
            float objSaliency = GetObjSaliencyScore(obj);
            if (objSaliency > maxObjSaliency) maxObjSaliency = objSaliency;
        }
        return maxObjSaliency;
    }

    private void OnFastMovement(GameObject movingObj)
    {
        var fixationTime = Mathf.Abs(fixationTimeDistribution.Get()) * (GetObjSaliencyScore(movingObj) + saliencyFixationModifier);
        if (!isFocusing)
        {
            StartCoroutine(FocusOnObject(movingObj, fixationTime));
            return;
        }
        if (currentFocus == movingObj) return;
        if (objectsFocusedOn.ContainsKey(movingObj.GetInstanceID())) return;
        if (currentFocusRoutine != null) StopCoroutine(currentFocusRoutine);
        isFocusing = false;
        if (currentFocus != null && currentFocus.GetInstanceID() != pathLookAhead.gameObject.GetInstanceID())
        {
            int currentFocusID = currentFocus.gameObject.GetInstanceID();
            if (objectsFocusedOn.TryAdd(currentFocusID, inhibitionOfReturnTime))
                StartCoroutine(ForgetObject(currentFocusID, inhibitionOfReturnTime));
        }
        StartCoroutine(FocusOnObject(movingObj, fixationTime));
    }

    private IEnumerator FocusOnPath(float fixationTime)
    {
        currentFocus = pathLookAhead.gameObject;
        currentFixationTime = fixationTime;
        isFocusing = true;
        yield return new WaitForSeconds(fixationTime);
        isFocusing = false;
        yield return null;
    }

    private IEnumerator FocusOnObject(GameObject obj, float fixationTime)
    {
        currentFocus = obj;
        currentFixationTime = fixationTime;
        isFocusing = true;        
        yield return new WaitForSeconds(fixationTime);
        int currentFocusID = obj.gameObject.GetInstanceID();
        if (objectsFocusedOn.TryAdd(currentFocusID, inhibitionOfReturnTime))
            StartCoroutine(ForgetObject(currentFocusID, inhibitionOfReturnTime));
        isFocusing = false;
        yield return null;
    }

    private IEnumerator ForgetObject(int objectID, float IORTime)
    {        
        while (objectsFocusedOn[objectID] > 0 && objectsFocusedOn[objectID] <= IORTime)
        {
            objectsFocusedOn[objectID] -= 1f;
            yield return new WaitForSeconds(1f);
        }
        objectsFocusedOn.Remove(objectID);
        yield return null;
    }

    public GameObject GetCurrentFocus()
    {
        return currentFocus ?? pathLookAhead.gameObject;
    }

    public float GetCurrentFixationTime()
    {
        return currentFixationTime;
    }

    public bool IsFocusingOnPath()
    {
        return currentFocus != null && currentFocus.GetInstanceID() == pathLookAhead.gameObject.GetInstanceID();
    }

    /*private void OnEnable()
    {
        opticalFlowController.onFastMovement += OnFastMovement;
    }

    private void OnDisable()
    {
        opticalFlowController.onFastMovement -= OnFastMovement;
    }*/
}
