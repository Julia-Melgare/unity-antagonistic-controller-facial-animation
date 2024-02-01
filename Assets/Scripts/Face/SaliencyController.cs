using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class SaliencyController : MonoBehaviour
{
    [SerializeField]
    private Camera agentCamera;
    [SerializeField]
    private Camera auxiliaryAgentCamera;
    [SerializeField]
    private InferenceClient inferenceClient;
    private Texture2D saliencyMapOutput;

    [Header("Saliency Map Settings")]
    [SerializeField]
    private int saliencyMapSize = 16;
    [SerializeField]
    private float scanFrequency = 30f;
    [SerializeField]
    private LayerMask scanLayerMask;
    [SerializeField]
    public List<GameObject> salientObjects;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float saliencyValueThreshold = 0.5f;

    [Header("Debug/Visualization")]
    [SerializeField]
    private RawImage saliencyMapImage;
    [SerializeField]
    private RawImage visionFrameImage;
    [SerializeField]
    private bool debugSaliencyRaycast = false;
    private Texture2D previousVisionFrame;
    private Texture2D currentVisionFrame;


    private float scanInterval; 
    private float scanTimer;
    private byte[] saliencyMapBytes;

    private Dictionary<GameObject, float> salientObjectsDict;
    
    void Start()
    {
        scanInterval = 1.0f / scanFrequency;
        auxiliaryAgentCamera.enabled = false;
        salientObjectsDict = new Dictionary<GameObject, float>();
        saliencyMapOutput = new Texture2D(16, 16);
    }
    void Update()
    {
        scanTimer -= Time.deltaTime;

        if (scanTimer < 0)
        {
            scanTimer += scanInterval;
            previousVisionFrame = currentVisionFrame;
            InferSaliencyMap();
            UpdateAuxiliaryCamera();
            if (saliencyMapBytes!=null)
            {
                UpdateSaliencyMap(saliencyMapBytes);
                ScanSaliencyMap();
            } 
        }
    }

    private void ScanSaliencyMap()
    {
        // Find index of highest value in map
        Color[] saliencyMapPixels = saliencyMapOutput.GetPixels();
        var saliencyPoints = new Dictionary<Vector3, float>();
        for (int i = 0; i < saliencyMapPixels.Length; i++)
        {
            float grayscaleValue = saliencyMapPixels[i].grayscale;
            if (grayscaleValue >= saliencyValueThreshold)
            {
                // Convert array index to matrix indexes
                int width = saliencyMapOutput.width;
                int height = saliencyMapOutput.height;
                int matrix_i = i / width;
                int matrix_j = i % width;
                // Compensate size difference between camera and saliency map
                matrix_i = matrix_i*(agentCamera.targetTexture.width/saliencyMapOutput.width);
                matrix_j = matrix_j*(agentCamera.targetTexture.height/saliencyMapOutput.height);
                saliencyPoints.Add(new Vector3(matrix_j, matrix_i, 0), grayscaleValue);
                if(debugSaliencyRaycast)
                    saliencyMapPixels[i] = Color.red; // Highlight pixel for visualization purposes
            } 
        }        
        // Get world coordinates from camera and raycast for objects
        salientObjectsDict = new Dictionary<GameObject, float>();
        foreach (var screenPoint in saliencyPoints)
        {
            Ray ray = auxiliaryAgentCamera.ScreenPointToRay(screenPoint.Key);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, scanLayerMask);
            foreach (RaycastHit hit in hits)
            {
                GameObject raycastObj = hit.collider.gameObject;

                if (hit.collider.GetType() == typeof(TerrainCollider))
                {
                    //We need to look at that specific point on the terrain instead
                    GameObject terrainPoint = new GameObject("TerrainPoint", typeof(SelfDestruct));
                    terrainPoint.transform.position = hit.point;
                    raycastObj = terrainPoint;
                }

                salientObjectsDict.TryAdd(raycastObj, screenPoint.Value);
            }                          
        }
        salientObjects = new List<GameObject>(salientObjectsDict.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value).Keys);
        if (debugSaliencyRaycast)
        {
            Texture2D newTexture = new Texture2D(16, 16);
            newTexture.SetPixels(saliencyMapPixels);
            newTexture.Apply();
            saliencyMapImage.texture = newTexture;
        }
    }
    public List<GameObject> GetSalientObjects()
    {
        return salientObjects;
    }

    public float GetObjectSaliency(GameObject obj)
    {
        if (salientObjectsDict == null) return .95f;
        return salientObjectsDict.GetValueOrDefault(obj, .95f);
    }

    private void InferSaliencyMap()
    {
        var input = GetCameraImage();
        inferenceClient.Infer(input, output =>
        {
            saliencyMapBytes = output;
        }, error =>
        {
            //Debug.LogError(error.Message);
        });
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

        currentVisionFrame = image;

        // Encode to PNG
        byte[] bytes = image.EncodeToJPG();

        // Set render texture back to default
        RenderTexture.active = currentRT;

        return bytes;
    }

    private void UpdateSaliencyMap(byte[] rawData)
    {
        Texture2D temp = new Texture2D(2, 2);
        ImageConversion.LoadImage(temp, rawData);
        saliencyMapOutput.SetPixels(temp.GetPixels());
        saliencyMapOutput.Apply();
        if (saliencyMapImage != null) saliencyMapImage.texture = saliencyMapOutput;
        if (visionFrameImage != null) visionFrameImage.texture = previousVisionFrame;
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
