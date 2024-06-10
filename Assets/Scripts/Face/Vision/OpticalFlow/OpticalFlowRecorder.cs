using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpticalFlowRecorder : MonoBehaviour
{
    [SerializeField]
    private Camera opticalFlowCamera;
    [SerializeField]
    private Camera fullViewCamera;
    private float iFrame = 0;

    void Update()
    {
        SaveCameraTexture(opticalFlowCamera, "opticalflow_" + iFrame);
        SaveCameraTexture(fullViewCamera, "fullview_" + iFrame);
        iFrame++;
    }

    private void SaveCameraTexture(Camera camera, string filename)
    {
        var currentRT = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;
        Texture2D image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
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
