using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [SerializeField]
    private OpticalFlowShader opticalFlowShader;

    [Header("Object Scan Settings")]
    [SerializeField]
    private LayerMask scanLayerMask;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float movementValueThreshold = 0.6f;
    [SerializeField]
    public List<GameObject> objectsLeft;
    [SerializeField]
    public List<GameObject> objectsRight;

    [Header("Debug/Visualization")]
    public bool debug = true;
    public bool debugMovementRaycast = false;
    [SerializeField]
    private RawImage opticalFlowLeftImg;
    [SerializeField]
    private RawImage opticalFlowRightImg;

    [SerializeField]
    private RawImage previousLeftImg;
    [SerializeField]
    private RawImage previousRightImg;

    private Texture2D prevLeftImg;
    private Texture2D prevRightImg;

    private Texture2D opticalFlowLeft;
    private Texture2D opticalFlowRight;

    private Dictionary<GameObject, float> objectsDictLeft;
    private Dictionary<GameObject, float> objectsDictRight;

    private float iFrame = 0;

    void Start()
    {
        objectsLeft = new List<GameObject>();
        objectsRight = new List<GameObject>();
        objectsDictLeft = new Dictionary<GameObject, float>();
        objectsDictRight = new Dictionary<GameObject, float>();
        prevLeftImg = new Texture2D(peripheralCamLeft.targetTexture.width, peripheralCamLeft.targetTexture.height);
        prevRightImg = new Texture2D(peripheralCamRight.targetTexture.width, peripheralCamRight.targetTexture.height);
        opticalFlowLeft = new Texture2D(opticalFlowShader.opticalFlowWidth, opticalFlowShader.opticalFlowHeight);
        opticalFlowRight = new Texture2D(opticalFlowShader.opticalFlowWidth, opticalFlowShader.opticalFlowHeight);
        UpdatePrevImages();
    }

    void Update()
    {
        // Compute left and right optical flows
        //ComputeOpticalFlows();
        // Scan optical flows
        //objectsLeft = ScanOpticalFlow(opticalFlowLeft, peripheralCamLeftAux, objectsDictLeft);
        //objectsRight = ScanOpticalFlow(opticalFlowRight, peripheralCamRightAux, objectsDictRight);
        // Update current camera images
        UpdatePrevImages();
        // Update Aux Camera Transforms
        UpdateAuxiliaryCameras();
        SaveCameraTexture(peripheralCamLeft, "opticalflowl" + iFrame);
        iFrame++;
        if (debug)
        {
            opticalFlowLeftImg.texture = opticalFlowLeft;
            opticalFlowRightImg.texture = opticalFlowRight;

            previousLeftImg.texture = prevLeftImg;
            previousRightImg.texture = prevRightImg;
        }
    }

    private List<GameObject> ScanOpticalFlow(Texture2D opticalFlow, Camera camera, Dictionary<GameObject, float> objDict)
    {
        // Scan left optical flow
        Color[] opticalFlowPixels = opticalFlow.GetPixels();
        var movementPoints = new Dictionary<Vector3, float>();
        for (int i = 0; i < opticalFlowPixels.Length; i++)
        {
            float grayscaleValue = opticalFlowPixels[i].grayscale;
            if (grayscaleValue >= movementValueThreshold)
            {
                // Convert array index to matrix indexes
                int width = opticalFlow.width;
                int height = opticalFlow.height;
                int matrix_i = i / width;
                int matrix_j = i % width;
                // Compensate size difference between camera and optical flow
                matrix_i = matrix_i*(camera.targetTexture.width/opticalFlow.width);
                matrix_j = matrix_j*(camera.targetTexture.height/opticalFlow.height);

                movementPoints.Add(new Vector3(matrix_j, matrix_i, 0), grayscaleValue);
                if(debugMovementRaycast)
                    opticalFlowPixels[i] = Color.magenta; // Highlight pixel for visualization purposes
            } 
        }       
        // Get world coordinates from camera and raycast for objects
        movementPoints = movementPoints.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        objDict = new Dictionary<GameObject, float>();
        foreach (var screenPoint in movementPoints)
        {
            Ray ray = camera.ScreenPointToRay(screenPoint.Key);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, scanLayerMask);
            foreach (RaycastHit hit in hits)
                objDict.TryAdd(hit.collider.gameObject, screenPoint.Value);
        }
        if (debugMovementRaycast)
        {
            opticalFlow.SetPixels(opticalFlowPixels);
            opticalFlow.Apply();
        }
        return new List<GameObject>(objDict.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value).Keys);
    }

    private void UpdateAuxiliaryCameras()
    {
        peripheralCamLeftAux.transform.position = peripheralCamLeft.transform.position;
        peripheralCamRightAux.transform.position = peripheralCamRight.transform.position;
        peripheralCamLeftAux.transform.rotation = peripheralCamLeft.transform.rotation;
        peripheralCamRightAux.transform.rotation = peripheralCamRight.transform.rotation;
    }
    
    private void ComputeOpticalFlows()
    {
        var currentRT = RenderTexture.active;

        // Compute Optical Flow left
        RenderTexture.active = opticalFlowShader.ComputeOpticalFlow(prevLeftImg, peripheralCamLeftAux.targetTexture);
        opticalFlowLeft.ReadPixels(new Rect(0, 0, opticalFlowShader.opticalFlowWidth, opticalFlowShader.opticalFlowHeight), 0, 0);
        opticalFlowLeft.Apply();

        // Compute Optical Flow Right
        RenderTexture.active = opticalFlowShader.ComputeOpticalFlow(prevRightImg, peripheralCamRightAux.targetTexture);
        opticalFlowRight.ReadPixels(new Rect(0, 0, opticalFlowShader.opticalFlowWidth, opticalFlowShader.opticalFlowHeight), 0, 0);
        opticalFlowRight.Apply();

        RenderTexture.active = currentRT;
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

    public float GetObjectSpeed(GameObject obj)
    {
        if (objectsDictLeft.TryGetValue(obj, out float speed))
        {
            return speed;
        }
        else
        {
            return objectsDictRight.GetValueOrDefault(obj, .95f);
        }
    }

    public float GetObjectMovementRight(GameObject obj)
    {
        return objectsDictRight.GetValueOrDefault(obj, .95f);
    }

    // Functions to help debug
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
