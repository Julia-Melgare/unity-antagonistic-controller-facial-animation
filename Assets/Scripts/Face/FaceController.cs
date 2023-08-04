using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceController : MonoBehaviour
{
    [SerializeField]
    private SafetyRegionLeft safetyRegionLeft;
    [SerializeField]
    private SafetyRegionRight safetyRegionRight;

    [SerializeField]
    private SaliencyController saliencyController;

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

    private const int EyeLookInLeftBlendShapeIndex = 4;
    private const int EyeLookInRightBlendShapeIndex = 5;
    private const int EyeLookOutLeftBlendShapeIndex = 6;
    private const int EyeLookOutRightBlendShapeIndex = 7;
    private const int EyeLookUpLeftBlendShapeIndex = 8;
    private const int EyeLookUpRightBlendShapeIndex = 9;
    private const int EyeLookDownLeftBlendShapeIndex = 10;
    private const int EyeLookDownRightBlendShapeIndex = 11;

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

    private Vector3 initialNeckForward;

    [Header("Attention Settings")]
    [SerializeField]
    private float focusTime = 2f;    

    private Collider currentFocus = null;
    private float focusTimer;

    private void Start()
    {
        StartCoroutine(Blink());
        initialNeckForward = neckTransform.forward;
        initialLeftEyeForward = leftEyeTransform.forward;
        initialRightEyeForward = rightEyeTransform.forward;
        focusTimer = 0f;
    }

    private void Update()
    {
        // Find nearest obstacle
        Collider nearestObstacle;
        if (safetyRegionLeft.targetObstacle.obstacle != null && safetyRegionRight.targetObstacle.obstacle != null)
           nearestObstacle = safetyRegionLeft.targetObstacle.distance < safetyRegionRight.targetObstacle.distance ? safetyRegionLeft.targetObstacle.obstacle : safetyRegionRight.targetObstacle.obstacle;
        else
           nearestObstacle = safetyRegionLeft.targetObstacle.obstacle != null ? safetyRegionLeft.targetObstacle.obstacle : safetyRegionRight.targetObstacle.obstacle;
        if (nearestObstacle == null)
        {
            nearestObstacle = saliencyController.GetSaliencyPoint();
        }

        // Rotate neck and eyes towards the target
        SetRotation(neckTransform, nearestObstacle, initialNeckForward, neckMovementSpeed);
        SetRotation(leftEyeTransform, nearestObstacle, initialLeftEyeForward, eyeMovementSpeed);
        SetRotation(rightEyeTransform, nearestObstacle, initialRightEyeForward, eyeMovementSpeed);

        //Clamp rotations
        ClampRotation(neckTransform, neckXRotationLimit, neckYRotationLimit, neckZRotationLimit);
        ClampRotation(leftEyeTransform, eyeXRotationLimit, eyeYRotationLimit, eyeZRotationLimit);
        ClampRotation(rightEyeTransform, eyeXRotationLimit, eyeYRotationLimit, eyeZRotationLimit);

        //Animate eye blendhsapes according to gaze direction
        AnimateEyeBlendShapes();

        //Update current focus
        if (nearestObstacle!=null) currentFocus = nearestObstacle;
        Debug.Log("Currently focusing on: "+currentFocus.gameObject.name);
    }

    private void SetRotation(Transform objectTransform, Collider nearestObstacle, Vector3 initialForward, float movementSpeed)
    {
        Vector3 targetDirection = transform.forward + new Vector3(0, initialForward.y, 0);

        if (nearestObstacle != null) targetDirection = nearestObstacle.transform.position - objectTransform.position;
        float singleStep = movementSpeed * Time.deltaTime;
        Vector3 newDirection = Vector3.RotateTowards(objectTransform.forward, targetDirection, singleStep, 0.0f);
        objectTransform.rotation = Quaternion.LookRotation(newDirection);
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

    private float NormalizeBlendshapeValue(float value, float max)
    {
        return 100 * Mathf.Abs(value)/max;
    }

    private IEnumerator Blink()
    {
        float blinkInterval = Random.Range(blinkIntervalMin, blinkIntervalMax);
        Debug.Log("Blinking: " + blinkInterval);
        yield return new WaitForSeconds(blinkInterval);
        faceAnimator.Play("Blinking");
        yield return Blink();
        yield return null;
    }
}
