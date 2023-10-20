using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttentionController : MonoBehaviour
{
    [Header("Stimuli Inputs")]
    [SerializeField]
    private SaliencyController saliencyController;
    [SerializeField]
    private FrustrumLineOfSight frustrumLineOfSight;
    [SerializeField]
    private SafetyRegionLeft safetyRegionLeft;
    [SerializeField]
    private SafetyRegionRight safetyRegionRight;

    [Header("Attention Settings")]
    [SerializeField]
    private bool focusOnSafetyRegions = true;
    [SerializeField]
    private bool focusOnSalientRegions = true;
    [SerializeField]
    private float focusTime = 2f;    
    [SerializeField]
    private GameObject currentFocus = null;
    [SerializeField]
    private List<int> objectsFocusedOn;
    private float focusTimer;

    [SerializeField]
    private List<GameObject> currentObjects;

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
        lineOfSightObjects = frustrumLineOfSight.GetObjects();
        if (focusOnSalientRegions)
            salientObjects = saliencyController.GetSalientObjects();
        
        if (focusOnSafetyRegions)
        {
            if (safetyRegionLeft.targetObstacle.obstacle!=null) safetyRegionObjects.Add(safetyRegionLeft.targetObstacle.obstacle.gameObject);
            if (safetyRegionRight.targetObstacle.obstacle!=null) safetyRegionObjects.Add(safetyRegionRight.targetObstacle.obstacle.gameObject);
        }
        
        UpdateCurrentObjects();

        if (focusTimer > 0)
        {
            focusTimer -= Time.deltaTime;
            return;
        }

        // Collider salientObstacle = null;
        // Collider safetyRegionObstacle = null;
        // if (focusOnSalientRegions)
        // {
        //     salientObstacle = saliencyController.GetSalientObject();
        // }

        // if (focusOnSafetyRegions)
        // {
        //     if (safetyRegionLeft.targetObstacle.obstacle != null && safetyRegionRight.targetObstacle.obstacle != null)
        //         safetyRegionObstacle = safetyRegionLeft.targetObstacle.distance < safetyRegionRight.targetObstacle.distance ? safetyRegionLeft.targetObstacle.obstacle : safetyRegionRight.targetObstacle.obstacle;
        //     else
        //         safetyRegionObstacle = safetyRegionLeft.targetObstacle.obstacle ?? safetyRegionRight.targetObstacle.obstacle;
        // }

        // // Find nearest obstacle
        // Collider nearestObstacle;
        // if (focusOnSalientRegions && focusOnSafetyRegions)
        //     nearestObstacle = safetyRegionObstacle != null ? safetyRegionObstacle : salientObstacle;
        // else
        //     nearestObstacle = focusOnSalientRegions ? salientObstacle : safetyRegionObstacle;

        // Update current focus
        // if (currentFocus!=null) objectsFocusedOn.Add(currentFocus.gameObject.GetInstanceID());
        if (currentObjects.Count() > 0) currentFocus = currentObjects[0];

        // Debug.Log("Currently focusing on: "+(currentFocus != null ? currentFocus.gameObject.name : "null"));
        focusTimer = focusTime;
    }

    private void UpdateCurrentObjects()
    {
        var currentObjectsSet = new HashSet<GameObject>();
        foreach(GameObject obj in lineOfSightObjects)
            currentObjectsSet.Add(obj);

        if (focusOnSalientRegions)
        {
            foreach(GameObject obj in salientObjects)
                currentObjectsSet.Add(obj);
        }
        
        if (focusOnSafetyRegions)
        {
            foreach(GameObject obj in safetyRegionObjects)
                currentObjectsSet.Add(obj);
        }
        
        currentObjects = new List<GameObject>(currentObjectsSet.ToList());        
    }

    public GameObject GetCurrentFocus()
    {
        return currentFocus.gameObject;
    }
}
