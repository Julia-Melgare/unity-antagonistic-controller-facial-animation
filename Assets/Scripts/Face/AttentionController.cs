using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;

public class AttentionController : MonoBehaviour
{
    [Header("Stimuli Inputs")]
    [SerializeField]
    private SaliencyController saliencyController;
    [SerializeField]
    private OpticalFlowController opticalFlowController;
    [SerializeField]
    private GameObject pathLookAheadTransform;

    [Header("Attention Settings")]
    [SerializeField]
    private float lookAtPathWeight = 78.8f;
    [SerializeField]
    private float lookAtObjectsWeight = 21.2f;
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
    private GameObject currentFocus = null;
    [SerializeField]
    private Dictionary<int, float> objectsFocusedOn;
    private bool focusing = false;

    [SerializeField]
    private List<GameObject> currentObjects;

    [Header("Debug/Visualization")]
    [SerializeField]
    private TextMeshProUGUI currentFocusDebugText;

    private List<GameObject> peripheralObjectsLeft;
    private List<GameObject> peripheralObjectsRight;
    private List<GameObject> salientObjects;

    private Coroutine currentFocusRoutine;

    private void Start()
    {
        saliencyController.enabled = focusOnSalientRegions;
        objectsFocusedOn = new Dictionary<int, float>();
        currentObjects = new List<GameObject>();
        peripheralObjectsLeft = new List<GameObject>();
        if (focusOnSalientRegions)
            salientObjects = new List<GameObject>();    
    }

    private void Update()
    {
        if (currentFocusDebugText != null) currentFocusDebugText.text = "Current focus: "+(currentFocus != null ? currentFocus.gameObject.name : "none");

        // If the agent is already focusing on something, return
        if (focusing) return;

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
        peripheralObjectsLeft = opticalFlowController.objectsLeft;
        peripheralObjectsRight = opticalFlowController.objectsRight;
        if (focusOnSalientRegions)
            salientObjects = saliencyController.salientObjects;
        
        UpdateCurrentObjects();

        if (currentObjects.Count() > 0)
        {
            //TODO: Use DDM here to pick object instead of using currentObjects[0]
            currentFocusRoutine = StartCoroutine(FocusOnObject(DecideFocusObject(), UnityEngine.Random.Range(focusTimeMin, focusTimeMax)));
        }        
    }

    private GameObject DecideFocusObject(int triesUntilConvergence = 10)
    {
        Dictionary<GameObject, float> driftPerObject = new Dictionary<GameObject, float>();
        foreach (GameObject obj in currentObjects)
        {
            driftPerObject.Add(obj, 0f);
        }
        for (int i = 0; i <= triesUntilConvergence; i++)
        {
            foreach (GameObject obj in currentObjects)
            {
                driftPerObject[obj] += ((saliencyController.GetObjectSaliency(obj) + opticalFlowController.GetObjectMovementLeft(obj)) * (inhibitionOfReturnTime - objectsFocusedOn.GetValueOrDefault(obj.GetInstanceID(), 0f)) * Time.deltaTime) + Random.Range(0, noiseLevel);
                //Debug.Log("[DDM] (Try "+i+") "+obj.name+": "+driftPerObject[obj]);
            }
            var max = driftPerObject.OrderByDescending(x => x.Value).First();
            if (max.Value >= decisionThreshold)
            {
                //Debug.Log("[DDM] (Try "+i+") Choosing object "+max.Key.name);
                return max.Key;
            }                
        }
        return currentObjects[0];
    }

    private void UpdateCurrentObjects()
    {
        var currentObjectsSet = new HashSet<GameObject>();
        foreach(GameObject obj in peripheralObjectsLeft)
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
        if (!focusing)
        {
            StartCoroutine(FocusOnObject(movingObj, UnityEngine.Random.Range(focusTimeMin, focusTimeMax)));
            return;
        }
        StopCoroutine(currentFocusRoutine);
        focusing = false;
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
        focusing = true;
        yield return new WaitForSeconds(fixationTime);
        focusing = false;
        yield return null;
    }

    private IEnumerator FocusOnObject(GameObject obj, float fixationTime)
    {
        currentFocus = obj;
        focusing = true;        
        yield return new WaitForSeconds(fixationTime);
        int currentFocusID = obj.gameObject.GetInstanceID();
        objectsFocusedOn.TryAdd(currentFocusID, inhibitionOfReturnTime);
        StartCoroutine(ForgetObject(currentFocusID, inhibitionOfReturnTime));
        focusing = false;
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

    /*private void OnEnable()
    {
        opticalFlowController.onFastMovement += OnFastMovement;
    }

    private void OnDisable()
    {
        opticalFlowController.onFastMovement -= OnFastMovement;
    }*/
}
