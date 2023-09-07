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
    private RawImage saliencyMapOutput;
    [SerializeField]
    private InferenceClient inferenceClient;

    [Header("Saliency Map Settings")]
    [SerializeField]
    private float scanFrequency = 30f;
    [SerializeField]
    private LayerMask scanLayerMask;
    [SerializeField]
    public List<GameObject> salientObjects;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float saliencyValueThreshold = 0.5f;


    private float scanInterval; 
    private float scanTimer;

    private byte[] saliencyMapBytes;

    // [Header("Saliency Parameters Settings")]
    // [SerializeField]
    // private float distanceWeight = 0.33f;
    // [SerializeField]
    // private float velocityWeight = 0.33f;
    // [SerializeField]
    // private float sizeWeight = 0.33f;
    
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
        Color[] saliencyMapPixels = (saliencyMapOutput.texture as Texture2D).GetPixels();
        var saliencyPoints = new List<Vector3>();
        for (int i = 0; i < saliencyMapPixels.Length; i++)
        {
            float grayscaleValue = saliencyMapPixels[i].grayscale;
            if (grayscaleValue >= saliencyValueThreshold)
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
        // Get world coordinates from camera and raycast for objects
        var salientObjectsSet = new HashSet<GameObject>();
        foreach (var screenPoint in saliencyPoints)
        {
            Ray ray = agentCamera.ViewportPointToRay(screenPoint);
            //Debug.DrawRay(ray.origin, ray.direction*100f, Color.green, 2.5f);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, scanLayerMask);
            foreach (RaycastHit hit in hits)
            {
                Debug.DrawLine(gameObject.transform.position, hit.transform.position, Color.red, 2.5f);
                salientObjectsSet.Add(hit.collider.gameObject);
            }                      
        }
        salientObjects = new List<GameObject>(salientObjectsSet.ToList());
    }
    public List<GameObject> GetSalientObjects()
    {
        return salientObjects;
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
    
    // public Collider GetSalientObject()
    // {
    //     List<Collider> salientObjects = GetSalientObjects();
    //     //TODO: Here we have to implement all of the object selection logic (choose by speed, distance, tags, etc.)
    //     Collider salientObject = null;

    //     float minDistance = float.MaxValue;
    //     float maxDistance = float.MinValue;
    //     float minVelocity = float.MaxValue;
    //     float maxVelocity = float.MinValue;
    //     float minSize = float.MaxValue;
    //     float maxSize = float.MinValue;
    //     float maxSaliencyScore = float.MinValue;
    //     foreach (Collider obj in salientObjects)
    //     {
    //         string output = obj.name + ": \\";
    //         // Calculate distance
    //         var distance = Vector3.Distance(transform.position, obj.transform.position);
    //         if (distance < minDistance)
    //         {
    //             minDistance = distance;
    //             salientObject = obj;
    //         }

    //         if (distance < maxDistance)
    //         {
    //             maxDistance = distance;
    //         }
    //         float distanceFactor = NormalizeValue(distance, maxDistance, minDistance);
    //         output+="distance factor: "+distanceFactor+"\\";

    //         // Calculate velocity
    //         var rigidbody = salientObject.gameObject.GetComponent<Rigidbody>();
    //         var velocity = 0.0f;
    //         if (rigidbody != null)
    //         {
    //             velocity = rigidbody.velocity.magnitude;
    //             if (velocity < minVelocity)
    //             {
    //                 minVelocity = velocity;
    //             }

    //             if (velocity < maxVelocity)
    //             {
    //                 maxVelocity = velocity;
    //             }
    //         }
    //         float velocityFactor = NormalizeValue(velocity, maxVelocity, minVelocity);
    //         output+="velocity factor: "+velocityFactor+"\\";

    //         // Calculate size
    //         var size = salientObject.bounds.size.magnitude; // TODO: not sure if we want to calculate size like this
    //         if (size < minSize)
    //         {
    //             minSize = size;
    //         }

    //         if (size < maxSize)
    //         {
    //             maxSize = size;
    //         }
    //         float sizeFactor = NormalizeValue(size, maxSize, minSize);
    //         output+="size factor: "+sizeFactor+"\\";
            
    //         // Now calculate saliency score
    //         float saliencyScore = CalculateSaliencyScore(distanceFactor, velocityFactor, sizeFactor);
    //         if (saliencyScore > maxSaliencyScore)
    //         {
    //             maxSaliencyScore = saliencyScore;
    //             salientObject = obj;
    //         }
    //         output+="saliency score: "+saliencyScore;
    //         Debug.Log(output);           
    //     }
        
    //     return salientObject;
    // }

    // private float CalculateSaliencyScore(float distanceFactor, float velocityFactor, float sizeFactor)
    // {
    //     return distanceFactor * distanceWeight + velocityFactor * velocityWeight + sizeFactor * sizeWeight;
    // }

    // private float NormalizeValue(float value, float min, float max)
    // {
    //     return (value-min)/(max-min);
    // }
}
