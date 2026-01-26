using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpticalFlowController : MonoBehaviour
{
    [Header("Script Inputs")]
    [SerializeField]
    private ComputeShader opticalFlowAccumShader;

    [Header("Compute Shader Inputs")]
    [SerializeField]
    private RenderTexture opticalFlowTexture;
    [SerializeField]
    private int bufferSize = 12;

    [Header("Debug/Visualization")]
    [SerializeField]
    private RawImage accumOpticalFlowImage;

    private int width = 256;
    private int height = 256;

    private int kernel;
    private RenderTexture accumOpticalFlowTexture;
    private Queue<RenderTexture> opticalFlowBuffer;

    void Start()
    {            
        kernel = opticalFlowAccumShader.FindKernel("AccumulateFlow");
        width = opticalFlowTexture.width;
        height = opticalFlowTexture.height;

        opticalFlowTexture.format = RenderTextureFormat.ARGBFloat;
        opticalFlowTexture.depth = 0;
        opticalFlowTexture.enableRandomWrite = true;
        opticalFlowTexture.Create();

        accumOpticalFlowTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        accumOpticalFlowTexture.enableRandomWrite = true;
        accumOpticalFlowTexture.Create();

        opticalFlowBuffer = new Queue<RenderTexture>(); 
    }

    void Update()
    {
        var opticalFlowFrame = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        opticalFlowFrame.enableRandomWrite = true;
        opticalFlowFrame.Create();
        Graphics.Blit(opticalFlowTexture, opticalFlowFrame);

        if (opticalFlowBuffer.Count < 2)
        {
            opticalFlowBuffer.Enqueue(opticalFlowFrame);
            return;
        }

        if (opticalFlowBuffer.Count <= bufferSize)
        {
            opticalFlowBuffer.Enqueue(opticalFlowFrame);
            var blankTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            blankTexture.enableRandomWrite = true;
            blankTexture.Create();
            DispatchComputeShader(opticalFlowFrame, blankTexture, accumOpticalFlowTexture, opticalFlowBuffer.Count);            
        }
        else
        {
            var oldFlow = opticalFlowBuffer.Dequeue();
            opticalFlowBuffer.Enqueue(opticalFlowFrame);
            DispatchComputeShader(opticalFlowFrame, oldFlow, accumOpticalFlowTexture, opticalFlowBuffer.Count);
        }
    }

    public void DispatchComputeShader(RenderTexture addFlow, RenderTexture subFlow, RenderTexture accumFlow, int framesInBuffer)
    {
        opticalFlowAccumShader.SetTexture(kernel, "AddFlow", addFlow);
        opticalFlowAccumShader.SetTexture(kernel, "SubFlow", subFlow);
        opticalFlowAccumShader.SetTexture(kernel, "AccumulatedFlow", accumFlow);

        opticalFlowAccumShader.SetInt("Width", width);
        opticalFlowAccumShader.SetInt("Height", height);
        opticalFlowAccumShader.SetInt("FramesInBuffer", framesInBuffer);        

        opticalFlowAccumShader.Dispatch(kernel, Mathf.CeilToInt(width/8f), Mathf.CeilToInt(height/8f), 1);

        accumOpticalFlowImage.texture = accumOpticalFlowTexture; 
    }
}
