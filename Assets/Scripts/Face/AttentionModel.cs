using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttentionModel : MonoBehaviour
{
    [SerializeField]
    private Camera agentCamera;
    [SerializeField]
    private InferenceClient inferenceClient;

    [Header("Saliency Map Settings")]
    [SerializeField]
    private float scanFrequency = 30f;

    private float scanInterval; 
    private float scanTimer;
    
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
        }
    }

    private void InferSaliencyMap()
    {
        var input = GetCameraImage();
        inferenceClient.Infer(input, output =>
        {
            Debug.Log(output.Length);
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
}
