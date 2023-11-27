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
    /*[SerializeField]
    private SafetyRegionLeft safetyRegionLeft;
    [SerializeField]
    private SafetyRegionRight safetyRegionRight;
    [SerializeField]
    private FaceSafetyRegion faceSafetyRegionLeft;
    [SerializeField]
    private FaceSafetyRegion faceSafetyRegionRight;*/

    [Header("Attention Settings")]
    [SerializeField]
    private bool focusOnSafetyRegions = true;
    [SerializeField]
    private bool focusOnSalientRegions = true;
    [SerializeField]
    private float focusTimeMin = 0.4f, focusTimeMax = 0.8f; // Values for exploring behavior    
    [SerializeField]
    private GameObject currentFocus = null;
    [SerializeField]
    private List<int> objectsFocusedOn;
    private float focusTimer;

    [SerializeField]
    private List<GameObject> currentObjects;

    [Header("Debug/Visualization")]
    [SerializeField]
    private TextMeshProUGUI currentFocusDebugText;

    private List<GameObject> lineOfSightObjects;
    private List<GameObject> salientObjects;
    private List<GameObject> safetyRegionObjects;
    private void Start()
    {
        focusTimer = 0;
        saliencyController.enabled = focusOnSalientRegions;
        objectsFocusedOn = new List<int>();
        currentObjects = new List<GameObject>();
        lineOfSightObjects = new List<GameObject>();
        if (focusOnSalientRegions)
            salientObjects = new List<GameObject>();
        
        if (focusOnSafetyRegions)
            safetyRegionObjects = new List<GameObject>();     
    }

    private void Update()
    {
        currentFocusDebugText.text = "Current focus: "+(currentFocus != null ? currentFocus.gameObject.name : "none");
        
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

        if (focusTimer > 0)
        {
            focusTimer -= Time.deltaTime;
            return;
        }

        if (currentFocus!=null) objectsFocusedOn.Add(currentFocus.gameObject.GetInstanceID());

        currentFocus = null;
        if (currentObjects.Count() > 0)
        {
            currentFocus = currentObjects[0];
        } 

        // Debug.Log("Currently focusing on: "+(currentFocus != null ? currentFocus.gameObject.name : "null"));
        focusTimer = UnityEngine.Random.Range(focusTimeMin, focusTimeMax);
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
        
        if (focusOnSafetyRegions)
        {
            foreach(GameObject obj in safetyRegionObjects)
            {
                if (!objectsFocusedOn.Contains(obj.GetInstanceID()))
                    currentObjectsSet.Add(obj);
            }
                
        }
        
        currentObjects = new List<GameObject>(currentObjectsSet.ToList());        
    }

    public GameObject GetCurrentFocus()
    {
        return currentFocus;
    }
}
