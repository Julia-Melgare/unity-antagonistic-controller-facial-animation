using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpticalFlowController : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField]
    private Camera peripheralCamLeft;
    [SerializeField]
    private Camera peripheralCamLeftAux;
    [SerializeField]
    private Camera peripheralCamRight;
    [SerializeField]
    private Camera peripheralCamRightAux;

    private RenderTexture prevLeftImg;
    private RenderTexture prevRightImg;

    void Start()
    {
        Graphics.Blit(peripheralCamLeft.activeTexture, prevLeftImg);
        Graphics.Blit(peripheralCamRight.activeTexture, prevRightImg);
        peripheralCamLeftAux.enabled = peripheralCamRightAux.enabled = false;        
    }

    void Update()
    {
        // Compute Optical Flow left
        // Compute Optical Flow Right
        
    }
}
