using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpticalFlowController : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField]
    private Camera peripheralCamLeft;
    [SerializeField]
    private Camera peripheralCamLeftAux;
    [SerializeField]
    private Camera peripheralCamRight;
    [SerializeField]
    private Camera peripheralCamRightAux;

    [Header("Debug/Visualization")]
    public bool debug = true;
    [SerializeField]
    private RawImage opticalFlowLeftImg;
    [SerializeField]
    private RawImage opticalFlowRightImg;

    [SerializeField]
    private RawImage previousLeftImg;
    [SerializeField]
    private RawImage previousRightImg;

    [SerializeField]
    private OpticalFlowShader opticalFlowShader;

    private Texture2D prevLeftImg;
    private Texture2D prevRightImg;

    private Texture2D opticalFlowLeft;
    private Texture2D opticalFlowRight;

    void Start()
    {
        prevLeftImg = new Texture2D(peripheralCamLeft.targetTexture.width, peripheralCamLeft.targetTexture.height);
        prevRightImg = new Texture2D(peripheralCamRight.targetTexture.width, peripheralCamRight.targetTexture.height);
        opticalFlowLeft = new Texture2D(opticalFlowShader.opticalFlowWidth, opticalFlowShader.opticalFlowHeight);
        opticalFlowRight = new Texture2D(opticalFlowShader.opticalFlowWidth, opticalFlowShader.opticalFlowHeight);
        UpdatePrevImages();
    }

    void Update()
    {
        // Compute Optical Flow left
        var currentRT = RenderTexture.active;
        RenderTexture.active = opticalFlowShader.ComputeOpticalFlow(prevLeftImg, peripheralCamLeftAux.targetTexture);
        opticalFlowLeft.ReadPixels(new Rect(0, 0, opticalFlowShader.opticalFlowWidth, opticalFlowShader.opticalFlowHeight), 0, 0);
        opticalFlowLeft.Apply();

        // Compute Optical Flow Right
        RenderTexture.active = opticalFlowShader.ComputeOpticalFlow(prevRightImg, peripheralCamRightAux.targetTexture);
        opticalFlowRight.ReadPixels(new Rect(0, 0, opticalFlowShader.opticalFlowWidth, opticalFlowShader.opticalFlowHeight), 0, 0);
        opticalFlowRight.Apply();

        RenderTexture.active = currentRT;

        // Update current camera images
        UpdatePrevImages();
        // Update Aux Camera Transforms
        peripheralCamLeftAux.transform.position = peripheralCamLeft.transform.position;
        peripheralCamRightAux.transform.position = peripheralCamRight.transform.position;
        peripheralCamLeftAux.transform.rotation = peripheralCamLeft.transform.rotation;
        peripheralCamRightAux.transform.rotation = peripheralCamRight.transform.rotation;

        if (debug)
        {
            opticalFlowLeftImg.texture = opticalFlowLeft;
            opticalFlowRightImg.texture = opticalFlowRight;

            previousLeftImg.texture = prevLeftImg;
            previousRightImg.texture = prevRightImg;
        }
    }

    private void UpdatePrevImages()
    {
        // Save current active render texture
        var currentRT = RenderTexture.active;

        // Set left previous image
        RenderTexture.active = peripheralCamLeft.targetTexture;
        prevLeftImg.ReadPixels(new Rect(0, 0, peripheralCamLeft.targetTexture.width, peripheralCamLeft.targetTexture.height), 0, 0);
        prevLeftImg.Apply();

        // Set right previous image
        RenderTexture.active = peripheralCamRight.targetTexture;
        prevRightImg.ReadPixels(new Rect(0, 0, peripheralCamRight.targetTexture.width, peripheralCamRight.targetTexture.height), 0, 0);
        prevRightImg.Apply();

        // Set render texture back to default
        RenderTexture.active = currentRT;
    }

    private void SaveTexture(Texture2D texture, string filename)
    {
        byte[] bytes = texture.EncodeToPNG();
        var dirPath = Application.dataPath + "/../SaveImages/";
        if(!System.IO.Directory.Exists(dirPath)) {
            System.IO.Directory.CreateDirectory(dirPath);
        }
        System.IO.File.WriteAllBytes(dirPath + filename + ".png", bytes);
    }

    private void SaveCameraTexture(Camera camera, string filename)
    {
        var currentRT = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;
        Texture2D image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, peripheralCamLeft.targetTexture.width, peripheralCamLeft.targetTexture.height), 0, 0);
        image.Apply();
        byte[] bytes = image.EncodeToPNG();
        var dirPath = Application.dataPath + "/../SaveImages/";
        if(!System.IO.Directory.Exists(dirPath)) {
            System.IO.Directory.CreateDirectory(dirPath);
        }
        System.IO.File.WriteAllBytes(dirPath + filename + ".png", bytes);
        RenderTexture.active = currentRT;
    }
}
