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
            GetSaliencyPoint();
        }
    }

    Vector3 GetSaliencyPoint()
    {
        // find index of highest value in map
        int max = 0;
        int maxIndex = -1;
        for (int i = 0; i < saliencyMapBytes.Length; i++)
        {
            if (saliencyMapBytes[i] > max)
            {
                max = saliencyMapBytes[i];
                maxIndex = i;
            }
        }
        // convert array index to matrix indexes
        int width = agentCamera.targetTexture.width;
        int height = agentCamera.targetTexture.height;
        int matrix_i = maxIndex / width;
        int matrix_j = maxIndex % width;
        // get world coordinates from matrix indexes
        Ray ray = agentCamera.ViewportPointToRay(new Vector3(matrix_i, matrix_j, 0));
        Debug.DrawRay(ray.origin, ray.direction, Color.green, 0.5f);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
            print("I'm looking at " + hit.transform.name);
        else
            print("I'm looking at nothing!");
        return Vector3.zero;
    }

    private void InferSaliencyMap()
    {
        var input = GetCameraImage();
        inferenceClient.Infer(input, output =>
        {
            Debug.Log(output.Length);
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
