using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class FixationController : MonoBehaviour
{
    [SerializeField]
    private Camera agentCamera;
    [SerializeField]
    private Camera auxiliaryAgentCamera;
    [SerializeField]
    private LayerMask scanLayerMask;
    [SerializeField]
    private InferenceClient inferenceClient;
    public int fixationIndex;
    public Vector3 currentFixationPoint;
    public double currentFixationTime;
    private byte[] fixationBytes;

    public bool IsFixating = false;
    private float fixationTimer;
    void Start()
    {
        fixationTimer = 0;
    }

    void Update()
    {
        fixationTimer -= Time.deltaTime;
        if (fixationTimer <= 0)
        {
            InferSaliencyMap();
            UpdateAuxiliaryCamera();
            if (fixationBytes!=null)
            {
                UpdateFixationIndex(fixationBytes);
                GetFixationPoint();
                fixationTimer = (float) currentFixationTime;
            }
            else
            {
                currentFixationPoint = Vector3.negativeInfinity;
            } 
        }
    }
    private void GetFixationPoint()
    {
        int matrix_i = fixationIndex % 256;
        int matrix_j = fixationIndex / 256;
        // Compensate size difference between camera and fixation map
        matrix_i = matrix_i*(agentCamera.targetTexture.width/256);
        matrix_j = matrix_j*(agentCamera.targetTexture.height/256);
        Ray ray = auxiliaryAgentCamera.ScreenPointToRay(new Vector3(matrix_i, matrix_j, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, scanLayerMask))
        {
            currentFixationPoint = hit.point;
        }     
    }

    public double GetCurrentFixationTime()
    {
        return currentFixationTime;     
    }

    public Vector3 GetCurrentFixationPoint()
    {
        return currentFixationPoint;
    }


    private void InferSaliencyMap()
    {
        fixationBytes = null;
        var input = GetCameraImage();
        RunSaliencyInference(input);
    }

    private byte[] GetCameraImage()
    {
        // Set render target to target texture
        var currentRT = RenderTexture.active;
        RenderTexture.active = agentCamera.targetTexture;

        // Create a new texture and read the active Render Texture into it.
        Texture2D image = new Texture2D(agentCamera.targetTexture.width, agentCamera.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, agentCamera.targetTexture.width, agentCamera.targetTexture.height), 0, 0);
        image.Apply();

        // Encode to PNG
        byte[] bytes = image.EncodeToPNG();

        // Set render texture back to default
        RenderTexture.active = currentRT;

        return bytes;
    }

    private void UpdateFixationIndex(byte[] rawData)
    {
        byte[] intBytes = new byte[4];
        Buffer.BlockCopy(rawData, 0, intBytes, 0, 4);
        fixationIndex = BitConverter.ToInt32(intBytes);
        Array.Reverse(rawData);
        currentFixationTime = BitConverter.ToDouble(rawData);
    }

    private void RunSaliencyInference(byte[] input)
    {
        inferenceClient.Infer(input, output =>
        {
            fixationBytes = output;
        }, error =>
        {
            //Debug.LogError(error.Message);
        });
    }

    private void UpdateAuxiliaryCamera()
    {
        auxiliaryAgentCamera.enabled = true;
        auxiliaryAgentCamera.transform.position = agentCamera.transform.position;
        auxiliaryAgentCamera.transform.rotation = agentCamera.transform.rotation;
        auxiliaryAgentCamera.transform.localScale = agentCamera.transform.localScale;
        auxiliaryAgentCamera.enabled = false;
    }
}
