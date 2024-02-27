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
    
    void Awake()
    {
        Material mat = gameObject.GetComponent<Renderer>().material;
        mat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
    }
    void Update()
    {
        if(attentionController != null && attentionController.isActiveAndEnabled)
        {
            transform.position = attentionController.GetCurrentFocus().GetFixationPoint();
            return;
        }
        
        if(fixationController != null && fixationController.isActiveAndEnabled)
        {
            transform.position = fixationController.GetCurrentFixationPoint();
            return;
        }        
    }
}
