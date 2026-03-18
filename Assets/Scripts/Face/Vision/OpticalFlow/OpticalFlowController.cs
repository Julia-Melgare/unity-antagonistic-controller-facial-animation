using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
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
    [SerializeField]
    private Camera peripheralViewCamera;
    [SerializeField]
    private LayerMask scanLayerMask;

    [Header("Compute Shader Inputs")]
    [SerializeField]
    private RenderTexture opticalFlowTexture;
    [SerializeField]
    private int bufferSize = 12;

    [Header("Debug/Visualization")]
    [SerializeField]
    private RawImage accumOpticalFlowImage;
    [SerializeField]
    private List<OpticalFlowObject> opticalFlowObjects;
    private Dictionary<int, GameObject> opticalFlowToGameObject;

    [Header("Output")]
    public List<GameObject> motionSalientObjects; //FixationObject

    private int width = 256;
    private int height = 256;

    private int kernel;
    private RenderTexture accumOpticalFlowTexture;
    private Queue<RenderTexture> opticalFlowBuffer;

    private RenderTexture accumExportTexture;
    private Texture2D captureTexture;
    private bool awaitingResponse = false;
    private byte[] inferenceResultBytes;

    private Dictionary<FixationObject, float> motionSalientObjectsDict;

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
        opticalFlowToGameObject = new Dictionary<int, GameObject>();
        motionSalientObjects = new List<GameObject>();//new List<FixationObject>();
        motionSalientObjectsDict = new Dictionary<FixationObject, float>();
        captureTexture = new Texture2D(width, height);
    }

    void Update()
    {
        AccumulateOpticalFlow();

        if (awaitingResponse)
            return;
        InferOpticalFlow();
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
            blankTexture.Release();
            Destroy(blankTexture);
        }
        else
        {
            var oldFlow = opticalFlowBuffer.Dequeue();
            opticalFlowBuffer.Enqueue(opticalFlowFrame);
            DispatchComputeShader(opticalFlowFrame, oldFlow, accumOpticalFlowTexture, opticalFlowBuffer.Count);
            oldFlow.Release();
            Destroy(oldFlow);
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
        captureTexture.ReadPixels(new Rect(0, 0, accumExportTexture.width, accumExportTexture.height), 0, 0);
        captureTexture.Apply(false);

        // Set render texture back to default
        RenderTexture.active = currentRT;
        return captureTexture.EncodeToJPG();
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
        inferenceResultBytes = null;

        ProcessInferenceResult(response);
    }

    private void ProcessInferenceResult(string result)
    {
        if (string.IsNullOrEmpty(result))
            return;

        var jsonObjects = result.Split('\n');
        opticalFlowObjects.Clear();
        foreach (string obj in jsonObjects)
        {
            if (string.IsNullOrEmpty(obj))
                continue;
            opticalFlowObjects.Add(JsonUtility.FromJson<OpticalFlowObject>(obj));
        }

        ScanOpticalFlowObjects();
    }

    void ScanOpticalFlowObjects()
    {
        motionSalientObjectsDict.Clear();
        // remove lost objects from dictionary
        var objIDs = from obj in opticalFlowObjects select obj.id;
        foreach (int key in opticalFlowToGameObject.Keys.ToArray())
        {
            if (!objIDs.Contains(key))
            {
                if (opticalFlowToGameObject[key].name.Equals("OpticalFlowPoint")) Destroy(opticalFlowToGameObject[key]);
                opticalFlowToGameObject.Remove(key);
            }
        }

        // assign/update game object for each ID
        foreach (var obj in opticalFlowObjects)
        {
            if (opticalFlowToGameObject.ContainsKey(obj.id) && !opticalFlowToGameObject[obj.id].name.Equals("OpticalFlowPoint"))
            {
                // we already found a proper game object for this ID, continue
                continue;
            }
            // raycast for new IDs and update IDs that dont have a proper game object
            Ray ray = peripheralViewCamera.ScreenPointToRay(new Vector3(obj.centroid[1], obj.centroid[0], 0));
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, scanLayerMask);
            FixationObject fixationObject = null;
            GameObject raycastObj;
            if (hits.Length > 0)
            {
                var hit = hits[0];
                raycastObj = hit.collider.gameObject;
                Vector3 hitLocalPos = raycastObj.transform.InverseTransformPoint(hit.point);
                fixationObject = new FixationObject(raycastObj, hitLocalPos);
            }
            else
            {
                // create fixation from the raycast direction OR update its position
                raycastObj = opticalFlowToGameObject.ContainsKey(obj.id) ? opticalFlowToGameObject[obj.id] : new GameObject("OpticalFlowPoint");
                raycastObj.transform.position = ray.GetPoint(25f);
                fixationObject = new FixationObject(raycastObj, Vector3.zero);
            }
            opticalFlowToGameObject[obj.id] = raycastObj;               
            motionSalientObjectsDict.TryAdd(fixationObject, obj.score);
        }
        motionSalientObjects = new List<GameObject>(opticalFlowToGameObject.Values);//new List<FixationObject>(motionSalientObjectsDict.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value).Keys);
    }

    private void OnDestroy()
    {
        foreach (var rt in opticalFlowBuffer)
        {
            rt.Release();
            Destroy(rt);
        }

        if (accumOpticalFlowTexture != null)
        {
            accumOpticalFlowTexture.Release();
            Destroy(accumOpticalFlowTexture);
        }

        if (accumExportTexture != null)
        {
            accumExportTexture.Release();
            Destroy(accumExportTexture);
        }

        if (captureTexture != null)
            Destroy(captureTexture);
    }
}
