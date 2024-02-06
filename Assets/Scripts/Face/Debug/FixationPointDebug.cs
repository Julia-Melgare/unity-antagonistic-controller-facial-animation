using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixationPointDebug : MonoBehaviour
{
    [SerializeField]
    private AttentionController attentionController;
    [SerializeField]
    private FixationController fixationController;
    void Update()
    {
        if(attentionController != null)
        {
            transform.position = attentionController.GetCurrentFocus().transform.position;
            return;
        }
        
        if(fixationController != null)
        {
            transform.position = fixationController.GetCurrentFixationPoint();
            return;
        }        
    }
}
