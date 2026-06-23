using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
    public List<FixationObject> imageSalientObjects;
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
    private bool awatingResponse = false;
    
    void Start()
    {
        scanInterval = 1.0f / scanFrequency;
        auxiliaryAgentCamera.enabled = false;
        saliencyMapOutput = new Texture2D(saliencyMapSize, saliencyMapSize);
        currentVisionFrame = new Texture2D(360, 360);
        imageSalientObjects = new List<FixationObject>();
    }
    void Update()
    {
        scanTimer -= Time.deltaTime;

        if (scanTimer < 0 && !awatingResponse)
        {
            scanTimer += scanInterval;
            previousVisionFrame = currentVisionFrame;
            InferSaliencyMap();
            UpdateAuxiliaryCamera(); 
        }
    }

    private void UpdateAuxiliaryCamera()
    {
        auxiliaryAgentCamera.enabled = true;
        auxiliaryAgentCamera.transform.position = agentCamera.transform.position;
        auxiliaryAgentCamera.transform.rotation = agentCamera.transform.rotation;
        auxiliaryAgentCamera.transform.localScale = agentCamera.transform.localScale;
        auxiliaryAgentCamera.enabled = false;
    }

    private void InferSaliencyMap()
    {
        var input = GetCameraImage();
        inferenceClient.Infer(input, output =>
        {
            saliencyMapBytes = output;
        }, error =>
        {
            Debug.LogError(error.Message);
        });
        awatingResponse = true;
        StartCoroutine(WaitForResponse());
    }

    private byte[] GetCameraImage()
    {
        // Set render target to target texture
        var currentRT = RenderTexture.active;
        RenderTexture.active = agentCamera.targetTexture;

        // Read the active Render Texture into our current vision frame.
        Debug.Log(currentVisionFrame);
        currentVisionFrame.ReadPixels(new Rect(0, 0, currentVisionFrame.width, currentVisionFrame.height), 0, 0);
        currentVisionFrame.Apply(false);

        // Set render texture back to default
        RenderTexture.active = currentRT;

        return currentVisionFrame.EncodeToJPG();
    }

    private IEnumerator WaitForResponse()
    {
        while (saliencyMapBytes == null)
        {
            //Debug.Log("Awating response...");
            yield return null;
        }
        UpdateSaliencyMap(saliencyMapBytes);
        saliencyMapBytes = null;
        awatingResponse = false;
    }

    private void UpdateSaliencyMap(byte[] rawData)
    {
        Texture2D temp = new Texture2D(2, 2);
        ImageConversion.LoadImage(temp, rawData);
        saliencyMapOutput.SetPixels(temp.GetPixels());
        saliencyMapOutput.Apply();
        Destroy(temp);
        if (saliencyMapImage != null) saliencyMapImage.texture = saliencyMapOutput;
        if (visionFrameImage != null) visionFrameImage.texture = previousVisionFrame;
        ScanSaliencyMap();
    }

    private void ScanSaliencyMap()
    {
        imageSalientObjects.Clear();
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
        foreach (var screenPoint in saliencyPoints)
        {
            Ray ray = auxiliaryAgentCamera.ScreenPointToRay(screenPoint.Key);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, scanLayerMask);
            FixationObject fixationObject;
            if (hits.Length > 0)
            {
                var hit = hits[0];
                GameObject raycastObj = hit.collider.gameObject;

                if (hit.collider.GetType() == typeof(TerrainCollider))
                {
                    //We need to look at that specific point on the terrain instead
                    GameObject terrainPoint = new GameObject("TerrainPoint", typeof(SelfDestruct));
                    terrainPoint.transform.position = hit.point;
                    raycastObj = terrainPoint;
                }

                Vector3 hitLocalPos = raycastObj.transform.InverseTransformPoint(hit.point);
                fixationObject = new FixationObject(raycastObj, hitLocalPos, screenPoint.Value);                
            }
            else
            {
                // create fixation from the raycast direction
                GameObject rayPoint = new GameObject("RayPoint", typeof(SelfDestruct));
                rayPoint.transform.position = ray.GetPoint(100f);
                fixationObject = new FixationObject(rayPoint, Vector3.zero, screenPoint.Value);
                
            }
            imageSalientObjects.Add(fixationObject);                                      
        }
        imageSalientObjects.Sort((x, y) => x.imageSaliencyScore.CompareTo(y.imageSaliencyScore));
        if (debugSaliencyRaycast)
        {
            Texture2D newTexture = new Texture2D(saliencyMapOutput.width, saliencyMapOutput.height);
            newTexture.SetPixels(saliencyMapPixels);
            newTexture.Apply();
            saliencyMapImage.texture = newTexture;
            Destroy(newTexture);
        }
    }
    public List<FixationObject> GetSalientObjects()
    {
        return imageSalientObjects ?? new List<FixationObject>();
    }

    private void OnDestroy()
    {
        if (currentVisionFrame != null) Destroy(currentVisionFrame);
        if (previousVisionFrame != null) Destroy(previousVisionFrame);
        if (saliencyMapOutput != null) Destroy(saliencyMapOutput);
    }
}
