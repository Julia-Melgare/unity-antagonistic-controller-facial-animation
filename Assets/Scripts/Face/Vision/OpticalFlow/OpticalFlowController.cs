using System.Collections;
using System.Collections.Generic;
using System.Text;
using NetMQ.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class OpticalFlowController : MonoBehaviour
{
    [Header("Script Inputs")]
    [SerializeField]
    private ComputeShader opticalFlowAccumShader;
    [SerializeField]
    private InferenceClient inferenceClient;
    [SerializeField]
    private Material opticalFlowScaleMaterial;

    [Header("Compute Shader Inputs")]
    [SerializeField]
    private RenderTexture opticalFlowTexture;
    [SerializeField]
    private int bufferSize = 12;

    [Header("Debug/Visualization")]
    [SerializeField]
    private RawImage accumOpticalFlowImage;

    public List<OpticalFlowObject> opticalFlowObjects;

    private int width = 256;
    private int height = 256;

    private int kernel;
    private RenderTexture accumOpticalFlowTexture;
    private Queue<RenderTexture> opticalFlowBuffer;

    private RenderTexture accumExportTexture;
    private bool awaitingResponse = false;
    private byte[] inferenceResultBytes;

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

        accumExportTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        accumExportTexture.enableRandomWrite = true;
        accumExportTexture.Create();

        opticalFlowObjects = new List<OpticalFlowObject>();
    }

    void Update()
    {
        AccumulateOpticalFlow();
        if (awaitingResponse)
            return;
        if (Input.GetKey(KeyCode.T))
        {
            InferOpticalFlow();
        }
        //InferOpticalFlow();
    }

    private void AccumulateOpticalFlow()
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
    private void DispatchComputeShader(RenderTexture addFlow, RenderTexture subFlow, RenderTexture accumFlow, int framesInBuffer)
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

    public void InferOpticalFlow()
    {
        opticalFlowObjects.Clear();
        var input = GetOpticalFlowImage();
        inferenceClient.Infer(input, output =>
        {
            inferenceResultBytes = output;
        }, error =>
        {
            Debug.LogError(error.Message);
        });
        awaitingResponse = true;
        StartCoroutine(WaitForResponse());
    }

    private byte[] GetOpticalFlowImage()
    {
        // Scale optical flow texture so that its visible
        Graphics.Blit(accumOpticalFlowTexture, accumExportTexture, opticalFlowScaleMaterial);
        // Set render target to target texture
        var currentRT = RenderTexture.active;
        RenderTexture.active = accumExportTexture;

        // Create a new texture and read the active Render Texture into it
        Texture2D image = new Texture2D(accumExportTexture.width, accumExportTexture.height);
        image.ReadPixels(new Rect(0, 0, accumExportTexture.width, accumExportTexture.height), 0, 0);
        image.Apply();

        // Encode to JPG
        byte[] bytes = image.EncodeToJPG();

        // Set render texture back to default
        RenderTexture.active = currentRT;
        return bytes;
    }

    private IEnumerator WaitForResponse()
    {
        while (inferenceResultBytes == null)
        {
            //Debug.Log("Awating response...");
            yield return null;
        }
        // Process result
        string response = Encoding.UTF8.GetString(inferenceResultBytes, 0, inferenceResultBytes.Length);
        awaitingResponse = false;
        ProcessInferenceResult(response);
    }

    private void ProcessInferenceResult(string result)
    {
        if (string.IsNullOrEmpty(result))
            return;

        var jsonObjects = result.Split('\n');
        foreach (string obj in jsonObjects)
        {
            if (string.IsNullOrEmpty(obj))
                continue;
            opticalFlowObjects.Add(JsonUtility.FromJson<OpticalFlowObject>(obj));
        }
    }
}
