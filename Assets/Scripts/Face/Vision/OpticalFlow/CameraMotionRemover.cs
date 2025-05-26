using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraMotionRemover : MonoBehaviour
{
    [Header("Script Inputs")]
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private ComputeShader cameraMovementShader;

    [Header("Compute Shader Inputs")]
    [SerializeField]
    private RenderTexture opticalFlowTexture;
    [SerializeField]
    private RenderTexture currentDepthTexture;
    [SerializeField]
    private RenderTexture previousDepthTexture;
    private Matrix4x4 currViewProjMatrix;
    private Matrix4x4 prevViewProjMatrix;
    private Matrix4x4 invCurrViewProjMatrix;

    private RenderTexture outputTexture;

    private int kernel;

    public bool debug;
    public RawImage debugImage;

    void Start()
    {
        kernel = cameraMovementShader.FindKernel("CSMain");

        // Properly initialize output texture
        outputTexture = new RenderTexture(opticalFlowTexture.width, opticalFlowTexture.height, 0, RenderTextureFormat.ARGBFloat);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        // Initialize matrices
        currViewProjMatrix = cam.projectionMatrix * cam.worldToCameraMatrix;
        prevViewProjMatrix = currViewProjMatrix;
    }

    void LateUpdate()
    {
        // Save the current matrix as previous for next frame
        prevViewProjMatrix = currViewProjMatrix;

        // Save the current depth texture as previous for next frame
        Graphics.Blit(currentDepthTexture, previousDepthTexture);

        // Compute the current view-projection matrix
        Matrix4x4 proj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
        Matrix4x4 view = cam.worldToCameraMatrix;
        currViewProjMatrix = proj * view;
        invCurrViewProjMatrix = currViewProjMatrix.inverse;

        Debug.Log(currViewProjMatrix);
        Debug.Log(prevViewProjMatrix);
        Debug.Log(invCurrViewProjMatrix);

        DispatchComputeShader();
        if (debug)
            debugImage.texture = outputTexture;
    }
    
    void DispatchComputeShader()
    {
        cameraMovementShader.SetMatrix("_CurrViewProj", currViewProjMatrix);
        cameraMovementShader.SetMatrix("_PrevViewProj", prevViewProjMatrix);
        cameraMovementShader.SetMatrix("_InvCurrViewProj", invCurrViewProjMatrix);

        cameraMovementShader.SetTexture(kernel, "_DepthTexture", currentDepthTexture);
        cameraMovementShader.SetTexture(kernel, "_OpticalFlow", opticalFlowTexture);
        cameraMovementShader.SetTexture(kernel, "_CameraFlowOut", outputTexture);

        int threadGroupsX = Mathf.CeilToInt(outputTexture.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(outputTexture.height / 8f);
        cameraMovementShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
    }
}
