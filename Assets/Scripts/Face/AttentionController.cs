using System.Collections;
using System.Collections.Generic;
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
    private HashSet<GameObject> currentObjects;

    private List<GameObject> lineOfSightObjects;
    private List<GameObject> salientObjects;
    private List<GameObject> safetyRegionObjects;
    private void Start()
    {
        focusTimer = 0;
        objectsFocusedOn = new List<int>();
        currentObjects = new HashSet<GameObject>();
        lineOfSightObjects = new List<GameObject>();
        salientObjects = new List<GameObject>();
        safetyRegionObjects = new List<GameObject>();
    }

    private void Update()
    {
        lineOfSightObjects = frustrumLineOfSight.GetObjects();
        salientObjects = saliencyController.GetSalientObjects();
        if (safetyRegionLeft.targetObstacle.obstacle!=null) safetyRegionObjects.Add(safetyRegionLeft.targetObstacle.obstacle.gameObject);
        if (safetyRegionRight.targetObstacle.obstacle!=null) safetyRegionObjects.Add(safetyRegionRight.targetObstacle.obstacle.gameObject);
        UpdateCurrentObjects();
        //Debug.Log(currentObjects);
        // if (focusTimer > 0)
        // {
        //     focusTimer -= Time.deltaTime;
        //     return;
        // }

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

        // // Update current focus
        // if (currentFocus!=null) objectsFocusedOn.Add(currentFocus.gameObject.GetInstanceID());
        // currentFocus = nearestObstacle;

        // Debug.Log("Currently focusing on: "+(currentFocus != null ? currentFocus.gameObject.name : "null"));
        // focusTimer = focusTime;
    }

    private void UpdateCurrentObjects()
    {
        foreach(GameObject obj in lineOfSightObjects)
        {
            currentObjects.Add(obj);
        }
        foreach(GameObject obj in salientObjects)
        {
            currentObjects.Add(obj);
        }
        foreach(GameObject obj in safetyRegionObjects)
        {
            currentObjects.Add(obj);
        }        
    }

    public GameObject GetCurrentFocus()
    {
        return currentFocus.gameObject;
    }
}
