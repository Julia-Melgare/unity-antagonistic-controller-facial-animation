using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttentionModelOutput : MonoBehaviour
{
    [Header("Stimuli Inputs")]
    [SerializeField]
    private SaliencyController saliencyController;
    [SerializeField]
    private FrustrumLineOfSight frustrumLineOfSight;

    [SerializeField]
    private List<FixationObject> currentObjects;

    private List<FixationObject> lineOfSightObjects;
    private List<FixationObject> salientObjects;

    void Start()
    {
        currentObjects = new List<FixationObject>();
        lineOfSightObjects = new List<FixationObject>();
        salientObjects = new List<FixationObject>();        
    }

    public List<FixationObject> GetObjectList()
    {
        UpdateCurrentObjects();
        return currentObjects;
    }

    private void UpdateCurrentObjects()
    {
        lineOfSightObjects = frustrumLineOfSight.GetObjects();
        salientObjects = saliencyController.GetSalientObjects();
        
        //Get all available objects in a single unique list
        var currentObjectsSet = new HashSet<FixationObject>();
                    
        foreach(FixationObject obj in salientObjects)
        {                
            obj.motionSaliencyScore = frustrumLineOfSight.GetObjectSpeed(obj);
            var objType = obj.gameObject.GetComponent<ObjectType>();
            if (objType != null)
                obj.objectType = objType.type;

            currentObjectsSet.Add(obj);
        }

        foreach(FixationObject obj in lineOfSightObjects)
        {                
            obj.imageSaliencyScore = saliencyController.GetObjectSaliency(obj);
            var objType = obj.gameObject.GetComponent<ObjectType>();
            if (objType != null)
                obj.objectType = objType.type;
            currentObjectsSet.Add(obj);           
        }       
        
        currentObjects = new List<FixationObject>(currentObjectsSet.ToList());
    }
}
