using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttentionModel : MonoBehaviour
{
    [SerializeField]
    private Camera agentCamera;
    [SerializeField]
    private InferenceClient inferenceClient;
    void Start()
    {
        
    }
    void Update()
    {
        
    }

    private void InferSaliencyMap()
    {
        var input = GetCameraImage();
        inferenceClient.Infer(input, output =>
        {
            // TODO: receive saliency map bytes and process them as an image
        }, error =>
        {
            // TODO: catch error
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
