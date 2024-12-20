using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class FixationPointDebug : MonoBehaviour
{
    [SerializeField]
    private AttentionController attentionController;
    [SerializeField]
    private FixationController fixationController;
    [SerializeField]
    private bool followExactEyeMovement = false;
    [SerializeField]
    private Transform leftEyeTransform;
    [SerializeField]
    private Transform rightEyeTransform;
    
    void Awake()
    {
        Material mat = gameObject.GetComponent<Renderer>().material;
        mat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);

        if (leftEyeTransform == null) leftEyeTransform = GameObject.Find("Eye_L").transform;
        if (rightEyeTransform == null) rightEyeTransform = GameObject.Find("Eye_R").transform;
    }
    void Update()
    {
        Vector3 fixationPoint = Vector3.zero;
        if(attentionController != null && attentionController.isActiveAndEnabled)
        {
            fixationPoint = attentionController.GetCurrentFocus().GetFixationPoint();
        }
        
        if(fixationController != null && fixationController.isActiveAndEnabled)
        {
            fixationPoint = fixationController.GetCurrentFixationPoint();
        }

        if(!attentionController.isActiveAndEnabled && !fixationController.isActiveAndEnabled)
        {
            Transform headTransform = GameObject.Find("mixamorig:Head").transform;
            Debug.DrawRay(headTransform.position, (headTransform.forward.normalized + Vector3.down/5f) * 100f, Color.blue);
            int characterLayerMask = LayerMask.NameToLayer("Character");
            if (Physics.Raycast(headTransform.position, headTransform.forward.normalized + Vector3.down/5f, out RaycastHit hit, 100f, ~(1<<characterLayerMask)))
            {
                fixationPoint = hit.point;
            }
            else
            {
                Debug.Log("didn't hit anything");
            }
        }

        if (followExactEyeMovement)
        {
            float distanceToFixation = Vector3.Distance(leftEyeTransform.position, fixationPoint);
            Ray r = new Ray(leftEyeTransform.position, leftEyeTransform.forward);
            transform.position = Vector3.MoveTowards(transform.position, r.GetPoint(distanceToFixation), 200f * Time.deltaTime);
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, fixationPoint, 125f*Time.deltaTime);
        }     
    }
}
