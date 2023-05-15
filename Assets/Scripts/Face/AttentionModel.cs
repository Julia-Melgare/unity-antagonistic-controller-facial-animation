using UnityEngine;
using UnityEngine.UI;

public class AttentionModel : MonoBehaviour
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
            if (saliencyMapBytes!=null) updateSaliencyMap(saliencyMapBytes);
        }
    }

    public Collider GetSaliencyPoint()
    {
        // Find index of highest value in map
        Color[] saliencyMapPixels = (saliencyMapOutput.texture as Texture2D).GetPixels();
        float maxValue = 0;
        int maxIndex = -1;
        for (int i = 0; i < saliencyMapPixels.Length; i++)
        {
            float grayscaleValue = saliencyMapPixels[i].grayscale;
            if (grayscaleValue > maxValue)
            {
                maxValue = grayscaleValue;
                maxIndex = i;
            }
        }
        // Convert array index to matrix indexes
        int width = agentCamera.targetTexture.width;
        int height = agentCamera.targetTexture.height;
        int matrix_i = maxIndex / width;
        int matrix_j = maxIndex % width;
        // Convert matrix indexes to camera coordinates
        float screenPointX = (float)matrix_i/width;
        float screenPointY = (float)matrix_j/height;
        // Get world coordinates from camera
        Ray ray = agentCamera.ViewportPointToRay(new Vector3(screenPointX, screenPointY, 0));
        Debug.DrawRay(ray.origin, ray.direction, Color.green, 0.25f);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("I'm looking at " + hit.transform.name);
            return hit.collider;
        }            
        Debug.Log("I'm looking at nothing!");
        return null;
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

    private void updateSaliencyMap(byte[] rawData)
    {
        Texture2D saliencyMapTexture = new Texture2D(2, 2);
        ImageConversion.LoadImage(saliencyMapTexture, rawData);
        saliencyMapOutput.texture = saliencyMapTexture;
    }
}