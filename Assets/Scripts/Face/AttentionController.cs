using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class AttentionController : MonoBehaviour
{
    [Header("Stimuli Inputs")]
    [SerializeField]
    private SaliencyController saliencyController;
    [SerializeField]
    private FrustrumLineOfSight frustrumLineOfSight;
    [SerializeField]
    private GameObject pathLookAheadTransform;

    [Header("Attention Settings")]
    [SerializeField]
    private float lookAtPathWeight = 78.8f;
    [SerializeField]
    private float focusTimeMin = 0.5f, focusTimeMax = 3f; // Values for exploring behavior
    [SerializeField]
    private float lookAtPathTimeMin = 0.2f, lookAtPathTimeMax = 0.5f; // Values for exploring behavior
    [SerializeField]
    private float inhibitionOfReturnTime = 20f;

    [SerializeField]
    private bool focusOnSalientRegions = true;

    [Header("Object Decision DDM Settings")]
    [SerializeField]
    private float decisionThreshold = 0.3f;
    [SerializeField]
    private float noiseLevel = 0.01f;
    [SerializeField]
    private float maxDecisionTime = 0.2f;

    [SerializeField]
    private GameObject currentFocus = null;
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

    private List<GameObject> lineOfSightObjects;
    private List<GameObject> salientObjects;
    private Dictionary<GameObject, float> driftPerObject;

    private Coroutine currentFocusRoutine;

    private void Start()
    {
        saliencyController.enabled = focusOnSalientRegions;
        objectsFocusedOn = new Dictionary<int, float>();
        currentObjects = new List<GameObject>();
        lineOfSightObjects = new List<GameObject>();
        if (focusOnSalientRegions)
            salientObjects = new List<GameObject>();    
    }

    private void Update()
    {
        if (currentFocusDebugText != null) currentFocusDebugText.text = "Current focus: "+(currentFocus != null ? currentFocus.gameObject.name : "none");

        // If the agent is already focusing on something, return
        if (isFocusing) return;

        // If the agent is choosing an object to focus, keep doing that
        if (isChoosingObject)
        {
            if (objectDecisionTimer <= 0) // Time is up, take object that converged the most
            {
                isChoosingObject = false;
                currentFocusRoutine = StartCoroutine(FocusOnObject(driftPerObject.OrderByDescending(x => x.Value).First().Key, UnityEngine.Random.Range(focusTimeMin, focusTimeMax)));
                return;
            }
            // Keep computing decision normally
            ChooseObjectStep();
            return;
        }

        // Decide if agent will look at path or at salient objects
        float decision = Random.Range(0, 100);
        //Debug.Log(decision);
        if (decision <= lookAtPathWeight)
        {
            // Focus on the path
            currentFocusRoutine = StartCoroutine(FocusOnPath(UnityEngine.Random.Range(lookAtPathTimeMin, lookAtPathTimeMax)));
            return;
        }

        // Focus on the objects
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
    }

    private void ChooseObjectStep()
    {
        // Compute DDM step
        GameObject objDecision = ComputeObjectDDM();

        // If an object converged, start focusing on it
        if (objDecision != null)
        {
            isChoosingObject = false;
            currentFocusRoutine = StartCoroutine(FocusOnObject(objDecision, UnityEngine.Random.Range(focusTimeMin, focusTimeMax)));
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
                driftPerObject[obj] += ((saliencyController.GetObjectSaliency(obj) + frustrumLineOfSight.GetObjectSpeed(obj)) * (inhibitionOfReturnTime - objectsFocusedOn.GetValueOrDefault(obj.GetInstanceID(), 0f)) * Time.deltaTime) + Random.Range(0, noiseLevel);
            else
                driftPerObject[obj] *= 0.9f;
            Debug.Log("[DDM] ("+(maxDecisionTime - objectDecisionTimer)+"s) "+obj.name+": "+driftPerObject[obj]);
        }
        var max = driftPerObject.OrderByDescending(x => x.Value).First();
        if (max.Value >= decisionThreshold)
        {
            Debug.Log("[DDM] ("+(maxDecisionTime - objectDecisionTimer)+"s) Choosing object "+max.Key.name);
            return max.Key;
        }
        return null;
    }

    private void UpdateCurrentObjects()
    {
        lineOfSightObjects = frustrumLineOfSight.GetObjects();
        if (focusOnSalientRegions)
            salientObjects = saliencyController.GetSalientObjects();
        
        var currentObjectsSet = new HashSet<GameObject>();
        foreach(GameObject obj in lineOfSightObjects)
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

    private void OnFastMovement(GameObject movingObj)
    {
        if (!isFocusing)
        {
            StartCoroutine(FocusOnObject(movingObj, UnityEngine.Random.Range(focusTimeMin, focusTimeMax)));
            return;
        }
        StopCoroutine(currentFocusRoutine);
        isFocusing = false;
        if (currentFocus != null && currentFocus.GetInstanceID() != pathLookAheadTransform.gameObject.GetInstanceID())
        {
            int currentFocusID = currentFocus.gameObject.GetInstanceID();
            objectsFocusedOn.TryAdd(currentFocusID, inhibitionOfReturnTime);
            StartCoroutine(ForgetObject(currentFocusID, inhibitionOfReturnTime));
        }
        StartCoroutine(FocusOnObject(movingObj, UnityEngine.Random.Range(focusTimeMin, focusTimeMax)));
    }

    private IEnumerator FocusOnPath(float fixationTime)
    {
        currentFocus = pathLookAheadTransform;
        isFocusing = true;
        yield return new WaitForSeconds(fixationTime);
        isFocusing = false;
        yield return null;
    }

    private IEnumerator FocusOnObject(GameObject obj, float fixationTime)
    {
        currentFocus = obj;
        isFocusing = true;        
        yield return new WaitForSeconds(fixationTime);
        int currentFocusID = obj.gameObject.GetInstanceID();
        objectsFocusedOn.TryAdd(currentFocusID, inhibitionOfReturnTime);
        StartCoroutine(ForgetObject(currentFocusID, inhibitionOfReturnTime));
        isFocusing = false;
        yield return null;
    }

    private IEnumerator ForgetObject(int objectID, float IORTime)
    {
        while (objectsFocusedOn[objectID] > 0 && objectsFocusedOn[objectID] <= IORTime)
        {
            objectsFocusedOn[objectID] -= 1;
            yield return new WaitForSeconds(1f);
        }
        objectsFocusedOn.Remove(objectID);
    }

    public GameObject GetCurrentFocus()
    {
        return currentFocus;
    }

    public bool IsFocusingOnPath()
    {
        return currentFocus != null && currentFocus.GetInstanceID() == pathLookAheadTransform.gameObject.GetInstanceID();
    }

    private void OnEnable()
    {
        frustrumLineOfSight.onFastMovement += OnFastMovement;
    }

    private void OnDisable()
    {
        frustrumLineOfSight.onFastMovement -= OnFastMovement;
    }
}
