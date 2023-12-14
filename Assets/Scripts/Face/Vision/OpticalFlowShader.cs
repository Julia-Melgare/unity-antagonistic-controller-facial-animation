using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpticalFlowShader : MonoBehaviour
{
    // Compute shader
    [Header("OpticalFlow Params")]
    [Tooltip("Defines the scale resolution from the source")] public int resolution = 1;
    private int kernelHandle;
    [Tooltip("Defines the compute Shader to use for Optical flow")] private ComputeShader compute;
    private RenderTexture opticalFlow;
    [HideInInspector] public int opticalFlowWidth;
    [HideInInspector] public int opticalFlowHeight;

    [Range(0, 1)]
    [Tooltip("Lambda deviation on the gradient magnitude")] public float lambda = 0.01f;
    [Range(0, 1)]
    [Tooltip("Velocity threshold")] public float threshold = 0.01f;
    [Tooltip("Scale up velocity")] public Vector2 scale = new Vector2(1.0f, 1.0f);

    [HideInInspector] public RenderTexture current;
    [HideInInspector] public RenderTexture previous;
    private Vector2 rtScale, rtOffset;

    private void Start()
    {
        InitOpticalFlow();
    }

    public void InitOpticalFlow()
    {
        InitSources();
        InitBuffers();
    }

    private void InitBuffers()
    {

        opticalFlow = new RenderTexture(opticalFlowWidth, opticalFlowHeight, 24, RenderTextureFormat.ARGBFloat);
        opticalFlow.filterMode = FilterMode.Trilinear;
        opticalFlow.wrapMode = TextureWrapMode.Clamp;
        opticalFlow.enableRandomWrite = true;
        opticalFlow.Create();

        //Bind variable to CS
        compute = Instantiate(compute);
        kernelHandle = compute.FindKernel("CSMain");
        compute.SetTexture(kernelHandle, "_OpticalFlowMap", opticalFlow);
        compute.SetVector("_Size", new Vector2((float)opticalFlow.width, (float)opticalFlow.height));
        compute.SetTexture(kernelHandle, "_Previous", previous);
        compute.SetTexture(kernelHandle, "_Current", current);
    }

    private void InitSources()
     {
        opticalFlowWidth = 180 / resolution;
        opticalFlowHeight = 180 / resolution;

        current = new RenderTexture(opticalFlowWidth, opticalFlowHeight, 0);
        previous = new RenderTexture(opticalFlowWidth, opticalFlowHeight, 0);
    }

    public void ComputeOpticalFlow(RenderTexture previous, RenderTexture current, RenderTexture output)
    {
        Graphics.Blit(previous, this.previous, rtScale, rtOffset);
        Graphics.Blit(current, this.current, rtScale, rtOffset);
        compute.SetFloat("_Lambda", lambda);
        compute.SetFloat("_Threshold", threshold);
        compute.SetVector("_Scale", scale);
        compute.Dispatch(kernelHandle, Mathf.CeilToInt((float)opticalFlow.width / 32), Mathf.CeilToInt((float)opticalFlow.height / 32), 1);
        Graphics.Blit(opticalFlow, output);
    }

    private void OnDisable()
    {
        if (opticalFlow != null)
        {
            opticalFlow.Release();
        }
        opticalFlow = null;

        if (previous != null) previous = null;
    }

    public RenderTexture GetOpticalFlowMap()
    {
        return opticalFlow;
    }

}
