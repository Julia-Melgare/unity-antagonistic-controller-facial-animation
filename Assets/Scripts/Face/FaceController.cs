using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FaceController : MonoBehaviour
{
    [SerializeField]
    private AttentionController attentionController;
    [SerializeField]
    private Animator faceAnimator;
    [SerializeField]
    private SkinnedMeshRenderer faceMeshRenderer;

    [Header("Eyes Settings")]
    [SerializeField]
    private Transform leftEyeTransform;
    [SerializeField]
    private Transform rightEyeTransform;
    [SerializeField]
    private float eyeXRotationLimit = 25f;
    [SerializeField]
    private float eyeYRotationLimit = 25f;
    [SerializeField]
    private float eyeZRotationLimit = 25f;
    [SerializeField]
    private float eyeMovementSpeed = 2f;

    private Vector3 initialLeftEyeForward;
    private Vector3 initialRightEyeForward;

    #region Blendshapes
    private const int BrowOuterUpLeftBlendShapeIndex = 0;
    private const int BrowOuterUpRightBlendShapeIndex = 1;
    private const int EyeSquintLeftBlendShapeIndex = 2;
    private const int EyeSquintRightBlendShapeIndex = 3;
    private const int EyeLookInLeftBlendShapeIndex = 4;
    private const int EyeLookOutLeftBlendShapeIndex = 5;
    private const int EyeLookInRightBlendShapeIndex = 6;
    private const int EyeLookOutRightBlendShapeIndex = 7;
    private const int EyeLookUpLeftBlendShapeIndex = 8;
    private const int EyeLookUpRightBlendShapeIndex = 9;
    private const int EyeLookDownLeftBlendShapeIndex = 10;
    private const int EyeLookDownRightBlendShapeIndex = 11;
    private const int CheekPuffBlendShapeIndex = 12;
    private const int CheekSquintLeftBlendShapeIndex = 13;
    private const int CheekSquintRightBlendShapeIndex = 14;
    private const int NoseSneerLeftBlendShapeIndex = 15;
    private const int NoseSneerRightBlendShapeIndex = 16;
    private const int MouthLeftBlendShapeIndex = 17;
    private const int MouthRightBlendShapeIndex = 18;
    private const int MouthPuckerBlendShapeIndex = 19;
    private const int MouthFunnelBlendShapeIndex = 20;
    private const int MouthSmileLeftBlendShapeIndex = 21;
    private const int MouthSmileRightBlendShapeIndex = 22;
    private const int MouthFrownLeftBlendShapeIndex = 23;
    private const int MouthFrownRightBlendShapeIndex = 24;
    private const int MouthDimpleLeftBlendShapeIndex = 25;
    private const int MouthDimpleRightBlendShapeIndex = 26;
    private const int MouthPressLeftBlendShapeIndex = 27;
    private const int MouthPressRightBlendShapeIndex = 28;
    private const int MouthShrugLowerBlendShapeIndex = 29;
    private const int MouthShrugUpperBlendShapeIndex = 30;
    private const int MouthStretchLeftBlendShapeIndex = 31;
    private const int MouthStretchRightBlendShapeIndex = 32;
    private const int MouthUpperUpLeftBlendShapeIndex = 33;
    private const int MouthUpperUpRightBlendShapeIndex = 34;
    private const int MouthLowerDownLeftBlendShapeIndex = 35;
    private const int MouthLowerDownRightBlendShapeIndex = 36;
    private const int MouthRollUpperBlendShapeIndex = 37;
    private const int MouthRollLowerBlendShapeIndex = 38;
    private const int MouthClosedBlendShapeIndex = 39;
    private const int JawForwardBlendShapeIndex = 40;
    private const int JawOpenBlendShapeIndex = 41;
    private const int JawLeftBlendShapeIndex = 42;
    private const int JawRightBlendShapeIndex = 43;
    private const int BrowInnerUpBlendShapeIndex = 44;
    private const int EyeBlinkingRightBlendShapeIndex = 45;
    private const int EyeBlinkingLeftBlendShapeIndex = 46;
    private const int BrowDownLeftBlendShapeIndex = 47;
    private const int BrowDownRightBlendShapeIndex = 48;
    private const int EyeWideRightBlendShapeIndex = 49;
    private const int EyeWideLeftBlendShapeIndex = 50;
    private const int TongueJawOpenBlendShapeIndex = 51;
    private const int TongueJawForwardBlendShapeIndex = 52;
    private const int TongueJawLeftBlendShapeIndex = 53;
    private const int TongueJawRightBlendShapeIndex = 54;
    private const int TongueOutBlendShapeIndex = 55;

    #endregion

    [SerializeField]
    private float blinkIntervalMin = 0.1f,  blinkIntervalMax = 1f;

    [Header("Neck Settings")]
    [SerializeField]
    private Transform neckTransform;
    [SerializeField]
    private float neckXRotationLimit = 70f;
    [SerializeField]
    private float neckYRotationLimit = 70f;
    [SerializeField]
    private float neckZRotationLimit = 30f;
    [SerializeField]
    private float neckMovementSpeed = 1.5f;

    [Header("Safety Regions Settings")]
    [SerializeField]
    private FaceSafetyRegion faceSafetyRegionLeft;
    [SerializeField]
    private FaceSafetyRegion faceSafetyRegionRight;
    [SerializeField]
    private float minEyeDistance = 0.3f; // Minimum distance to eye needed to start animating squint blendshape
    private Vector3 initialNeckForward;

    private void Start()
    {
        StartCoroutine(Blink());
        initialNeckForward = neckTransform.forward;
        initialLeftEyeForward = leftEyeTransform.forward;
        initialRightEyeForward = rightEyeTransform.forward;
    }

    private void Update()
    {
        GameObject objectOfInterest = attentionController.GetCurrentFocus();

        // Rotate eyes towards the target
        SetRotation(leftEyeTransform, objectOfInterest, initialLeftEyeForward, eyeMovementSpeed);
        SetRotation(rightEyeTransform, objectOfInterest, initialRightEyeForward, eyeMovementSpeed);

        // Clamp eye rotations
        ClampRotation(leftEyeTransform, eyeXRotationLimit, eyeYRotationLimit, eyeZRotationLimit);
        ClampRotation(rightEyeTransform, eyeXRotationLimit, eyeYRotationLimit, eyeZRotationLimit);

        // Rotate neck towards eyes middle point
        Vector3 middlePoint = (leftEyeTransform.forward + rightEyeTransform.forward).normalized;
        SetRotation(neckTransform, objectOfInterest, initialNeckForward, middlePoint, neckMovementSpeed);

        // Clamp neck rotation
        ClampRotation(neckTransform, neckXRotationLimit, neckYRotationLimit, neckZRotationLimit);

        // Animate eye blendhsapes according to gaze direction
        AnimateEyeBlendShapes();

        AnimateSquintBlendShapes();
    }

    private void SetRotation(Transform objectTransform, GameObject objectOfInterest, Vector3 initialForward, float movementSpeed)
    {
        Vector3 targetDirection = transform.forward + new Vector3(0, initialForward.y, 0);

        if (objectOfInterest != null) targetDirection = objectOfInterest.transform.position - objectTransform.position;
        float singleStep = movementSpeed * Time.deltaTime;
        Vector3 newDirection = Vector3.RotateTowards(objectTransform.forward, targetDirection, singleStep, 0.0f);
        objectTransform.rotation = Quaternion.LookRotation(newDirection);
        Debug.DrawRay(objectTransform.position, newDirection, Color.red);
    }

    private void SetRotation(Transform objectTransform, GameObject objectOfInterest, Vector3 initialForward, Vector3 targetRotation, float movementSpeed)
    {
        Vector3 targetDirection = transform.forward + new Vector3(0, initialForward.y, 0);

        if (objectOfInterest != null) targetDirection = targetRotation;
        float singleStep = movementSpeed * Time.deltaTime;
        Vector3 newDirection = Vector3.RotateTowards(objectTransform.forward, targetDirection, singleStep, 0.0f);
        objectTransform.rotation = Quaternion.LookRotation(newDirection);
        Debug.DrawRay(objectTransform.position, newDirection, Color.red);
    }

    private void ClampRotation(Transform objectTransform, float xRotationLimit, float yRotationLimit, float zRotationLimit) 
    {
        Vector3 localRotation = objectTransform.localEulerAngles;
        float xRotation = localRotation.x > 180 ? localRotation.x - 360 : localRotation.x;
        float yRotation = localRotation.y > 180 ? localRotation.y - 360 : localRotation.y;
        float zRotation = localRotation.z > 180 ? localRotation.z - 360 : localRotation.z;
        objectTransform.localEulerAngles = new Vector3
            (
                Mathf.Clamp(xRotation, -xRotationLimit, xRotationLimit),
                Mathf.Clamp(yRotation, -yRotationLimit, yRotationLimit),
                Mathf.Clamp(zRotation, -zRotationLimit, zRotationLimit)
            );

        /*if(xRotation < -xRotationLimit || xRotation > xRotationLimit || yRotation < -yRotationLimit || yRotation > yRotationLimit || zRotation < -zRotationLimit || zRotation > zRotationLimit)
            return true;    
        return false;*/
    }

    private void AnimateEyeBlendShapes()
    {
        Vector3 localLeftEyeRotation = leftEyeTransform.localEulerAngles;
        float xLeftEyeRotation = localLeftEyeRotation.x > 180 ? localLeftEyeRotation.x - 360 : localLeftEyeRotation.x;
        float yLeftEyeRotation = localLeftEyeRotation.y > 180 ? localLeftEyeRotation.y - 360 : localLeftEyeRotation.y;
        int xLeftEyeBlendShapeIndex = Mathf.Sign(xLeftEyeRotation) < 0 ? EyeLookUpLeftBlendShapeIndex : EyeLookDownLeftBlendShapeIndex;
        int yLeftEyeBlendShapeIndex = Mathf.Sign(yLeftEyeRotation) < 0 ? EyeLookOutLeftBlendShapeIndex : EyeLookInLeftBlendShapeIndex;

        Vector3 localRightEyeRotation = rightEyeTransform.localEulerAngles;
        float xRightEyeRotation = localRightEyeRotation.x > 180 ? localRightEyeRotation.x - 360 : localRightEyeRotation.x;
        float yRightEyeRotation = localRightEyeRotation.y > 180 ? localRightEyeRotation.y - 360 : localRightEyeRotation.y;
        int xRightEyeBlendShapeIndex = Mathf.Sign(xRightEyeRotation) < 0 ? EyeLookUpRightBlendShapeIndex : EyeLookDownRightBlendShapeIndex;
        int yRightEyeBlendShapeIndex = Mathf.Sign(yRightEyeRotation) < 0 ? EyeLookOutRightBlendShapeIndex : EyeLookInRightBlendShapeIndex;

        faceMeshRenderer.SetBlendShapeWeight(xLeftEyeBlendShapeIndex, NormalizeBlendshapeValue(xLeftEyeRotation, eyeXRotationLimit));
        faceMeshRenderer.SetBlendShapeWeight(yLeftEyeBlendShapeIndex, NormalizeBlendshapeValue(yLeftEyeRotation, eyeYRotationLimit));
        faceMeshRenderer.SetBlendShapeWeight(xRightEyeBlendShapeIndex, NormalizeBlendshapeValue(xRightEyeRotation, eyeXRotationLimit));
        faceMeshRenderer.SetBlendShapeWeight(yRightEyeBlendShapeIndex, NormalizeBlendshapeValue(yRightEyeRotation, eyeYRotationLimit));
    }

    private void AnimateSquintBlendShapes()
    {
        if (faceSafetyRegionLeft.closestObstacle == null)
        {
            faceMeshRenderer.SetBlendShapeWeight(EyeSquintLeftBlendShapeIndex, 0f);
        }
        if (faceSafetyRegionRight.closestObstacle == null)
        {
            faceMeshRenderer.SetBlendShapeWeight(EyeSquintRightBlendShapeIndex, 0f);
        }
        if (faceSafetyRegionLeft.closestDistanceToEye < minEyeDistance)
        {
            faceMeshRenderer.SetBlendShapeWeight(EyeSquintLeftBlendShapeIndex, 100f - NormalizeBlendshapeValue(faceSafetyRegionLeft.closestDistanceToEye, minEyeDistance));
        }
        if (faceSafetyRegionRight.closestDistanceToEye < minEyeDistance)
        {
            faceMeshRenderer.SetBlendShapeWeight(EyeSquintRightBlendShapeIndex, 100f - NormalizeBlendshapeValue(faceSafetyRegionRight.closestDistanceToEye, minEyeDistance));
        }
    }

    private float NormalizeBlendshapeValue(float value, float max, float min=0)
    {
        return 100 * Mathf.Abs(value - min)/(max - min);
    }

    private IEnumerator Blink()
    {
        float blinkInterval = UnityEngine.Random.Range(blinkIntervalMin, blinkIntervalMax);
        //Debug.Log("Blinking: " + blinkInterval);
        yield return new WaitForSeconds(blinkInterval);
        faceAnimator.Play("Blinking");
        yield return Blink();
        yield return null;
    }

    // private string GetBlendshapeNames()
    // {
    //     string blendshapeNames = "";
    //     for (int i = 0; i < faceMeshRenderer.sharedMesh.blendShapeCount; i++)
    //     {
    //         string blendShapeName = faceMeshRenderer.sharedMesh.GetBlendShapeName(i);
    //         string blendShapeDirection = "";
    //         string[] blendShapeNameDir = blendShapeName.Split('_');
    //         if (blendShapeNameDir.Length > 1)
    //         {
    //             blendShapeDirection = blendShapeNameDir[1];
    //             if(blendShapeDirection.Contains("R"))
    //             {
    //                 blendShapeDirection = "Right";
    //             }
    //             else
    //             {
    //                 blendShapeDirection = "Left";
    //             }
    //         }
    //         blendShapeName = char.ToUpper(blendShapeNameDir[0][0]) + blendShapeNameDir[0].Substring(1);
    //         blendshapeNames += String.Format("private const int {0}{1}BlendShapeIndex = {2};\n", blendShapeName, blendShapeDirection, i);
    //     }
    //     return blendshapeNames;
    // }
}
