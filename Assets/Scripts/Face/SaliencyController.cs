using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaliencyController : MonoBehaviour
{
    [SerializeField]
    private Camera agentCamera;
    [SerializeField]
    private RawImage saliencyMapOutput;
    [SerializeField]
    private InferenceClient inferenceClient;

    [Header("Saliency Map Settings")]
    [SerializeField]
    private float scanFrequency = 30f;
    [SerializeField]
    private LayerMask scanLayerMask;

    private float scanInterval; 
    private float scanTimer;

    private byte[] saliencyMapBytes;
    
    void Start()
    {
        scanInterval = 1.0f / scanFrequency;
    }
    void Update()
    {
        scanTimer -= Time.deltaTime;

        if (scanTimer < 0)
        {
            scanTimer += scanInterval;
            InferSaliencyMap();
            if (saliencyMapBytes!=null) UpdateSaliencyMap(saliencyMapBytes);
        }
    }

    private List<Collider> GetSalientObjects()
    {
        // Find index of highest value in map
        Color[] saliencyMapPixels = (saliencyMapOutput.texture as Texture2D).GetPixels();
        var saliencyPoints = new List<Vector3>();
        for (int i = 0; i < saliencyMapPixels.Length; i++)
        {
            float grayscaleValue = saliencyMapPixels[i].grayscale;
            if (grayscaleValue >= .95f)
            {
                // Convert array index to matrix indexes
                int width = agentCamera.targetTexture.width;
                int height = agentCamera.targetTexture.height;
                int matrix_i = i / width;
                int matrix_j = i % width;
                // Convert matrix indexes to camera coordinates
                float screenPointX = (float)matrix_i/width;
                float screenPointY = (float)matrix_j/height;
                saliencyPoints.Add(new Vector3(screenPointX, screenPointY, 0));
            } 
        }        
        // Get world coordinates from camera
        var objects = new List<string>();
        var obstacles = new List<Collider>();
        foreach (var screenPoint in saliencyPoints)
        {
            Ray ray = agentCamera.ViewportPointToRay(screenPoint);
            Debug.DrawRay(ray.origin, ray.direction, Color.green, 0.25f);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, float.MaxValue, scanLayerMask))
            {
                objects.Add(hit.transform.name);
                obstacles.Add(hit.collider);
            }            
            objects.Add("null");
        }
        //Print list
        string output = "[ ";
        foreach (string obj in objects) output+= obj + " ";
        output+= "]";
        //Debug.Log(output);        
        return obstacles;
    }
    
    public Collider GetSaliencyPoint()
    {
        List<Collider> salientObjects = GetSalientObjects();
        //TODO: Here we have to implement all of the object selection logic (choose by speed, distance, tags, etc.)
        // For now: focuses on the nearest object
        Collider salientObject = null;

        float minDistance = float.MaxValue;
        float maxDistance = float.MinValue;
        foreach (Collider obj in salientObjects)
        {
            var distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                salientObject = obj;
            }

            if (distance < maxDistance)
            {
                maxDistance = distance;
            }
            Debug.Log("Distance factor: "+NormalizeValue(distance, maxDistance, minDistance));
        }
        
        return salientObject;
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

    private void UpdateSaliencyMap(byte[] rawData)
    {
        Texture2D saliencyMapTexture = new Texture2D(2, 2);
        ImageConversion.LoadImage(saliencyMapTexture, rawData);
        saliencyMapOutput.texture = saliencyMapTexture;
    }

    private float NormalizeValue(float value, float min, float max)
    {
        return (value-min)/(max-min);
    }
}
