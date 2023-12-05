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
    /*[SerializeField]
    private SafetyRegionLeft safetyRegionLeft;
    [SerializeField]
    private SafetyRegionRight safetyRegionRight;
    [SerializeField]
    private FaceSafetyRegion faceSafetyRegionLeft;
    [SerializeField]
    private FaceSafetyRegion faceSafetyRegionRight;*/

    [Header("Attention Settings")]
    //[SerializeField]
    //private bool focusOnSafetyRegions = true;
    [SerializeField]
    private float lookAtPathWeight = 78.8f;
    [SerializeField]
    private float lookAtObjectsWeight = 21.2f;
    [SerializeField]
    private float focusTimeMin = 0.5f, focusTimeMax = 3f; // Values for exploring behavior
    [SerializeField]
    private float lookAtPathTimeMin = 0.2f, lookAtPathTimeMax = 0.5f; // Values for exploring behavior
    [SerializeField]
    private float memoryTime = 20f;

    [SerializeField]
    private bool focusOnSalientRegions = true;

    [SerializeField]
    private GameObject currentFocus = null;
    [SerializeField]
    private List<int> objectsFocusedOn;
    private bool focusing = false;

    [SerializeField]
    private List<GameObject> currentObjects;

    [Header("Debug/Visualization")]
    [SerializeField]
    private TextMeshProUGUI currentFocusDebugText;

    private List<GameObject> lineOfSightObjects;
    private List<GameObject> salientObjects;
    //private List<GameObject> safetyRegionObjects;

    private Coroutine currentFocusRoutine;

    private void Start()
    {
        saliencyController.enabled = focusOnSalientRegions;
        objectsFocusedOn = new List<int>();
        currentObjects = new List<GameObject>();
        lineOfSightObjects = new List<GameObject>();
        if (focusOnSalientRegions)
            salientObjects = new List<GameObject>();
        
        //if (focusOnSafetyRegions)
        //    safetyRegionObjects = new List<GameObject>();     
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

        lineOfSightObjects = frustrumLineOfSight.GetObjects();
        if (focusOnSalientRegions)
            salientObjects = saliencyController.GetSalientObjects();
        
        /*if (focusOnSafetyRegions)
        {
            if (safetyRegionLeft!=null && safetyRegionLeft.targetObstacle.obstacle!=null) safetyRegionObjects.Add(safetyRegionLeft.targetObstacle.obstacle.gameObject);
            if (safetyRegionRight!=null && safetyRegionRight.targetObstacle.obstacle!=null) safetyRegionObjects.Add(safetyRegionRight.targetObstacle.obstacle.gameObject);
            if (faceSafetyRegionLeft!=null && faceSafetyRegionLeft.closestObstacle!=null) safetyRegionObjects.Add(faceSafetyRegionLeft.closestObstacle);
            if (faceSafetyRegionRight!=null && faceSafetyRegionRight.closestObstacle!=null) safetyRegionObjects.Add(faceSafetyRegionRight.closestObstacle);
        }*/
        
        UpdateCurrentObjects();

        if (currentObjects.Count() > 0)
        {
            currentFocusRoutine = StartCoroutine(FocusOnObject(currentObjects[0], UnityEngine.Random.Range(focusTimeMin, focusTimeMax)));
        }        
    }

    private void UpdateCurrentObjects()
    {
        var currentObjectsSet = new HashSet<GameObject>();
        foreach(GameObject obj in lineOfSightObjects)
        {
            if (!objectsFocusedOn.Contains(obj.GetInstanceID()))
                currentObjectsSet.Add(obj);
        }
            

        if (focusOnSalientRegions)
        {
            foreach(GameObject obj in salientObjects)
            {
                if (!objectsFocusedOn.Contains(obj.GetInstanceID()))
                    currentObjectsSet.Add(obj);
            }
                
        }
        
        /*if (focusOnSafetyRegions)
        {
            foreach(GameObject obj in safetyRegionObjects)
            {
                if (!objectsFocusedOn.Contains(obj.GetInstanceID()))
                    currentObjectsSet.Add(obj);
            }                
        }*/
        
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
            objectsFocusedOn.Add(currentFocusID);
            StartCoroutine(ForgetObject(currentFocusID, memoryTime));
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
        objectsFocusedOn.Add(currentFocusID);
        StartCoroutine(ForgetObject(currentFocusID, memoryTime));
        focusing = false;
        yield return null;
    }

    private IEnumerator ForgetObject(int objectID, float memoryTime)
    {
        yield return new WaitForSeconds(memoryTime);
        objectsFocusedOn.Remove(objectID);
    }

    public GameObject GetCurrentFocus()
    {
        return currentFocus;
    }

    public bool isFocusingOnPath()
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
