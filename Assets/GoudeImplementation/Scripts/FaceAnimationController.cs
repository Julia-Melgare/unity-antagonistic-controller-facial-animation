using System.Collections;
using UnityEngine;
using Voxus.Random;

public class FaceAnimationController : MonoBehaviour
{
    [SerializeField]
    private FixationController fixationController;
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
    private float eyeXRotationLimit = 50f;
    [SerializeField]
    private float eyeYRotationLimit = 50f;
    [SerializeField]
    private float eyeZRotationLimit = 50f;
    [SerializeField]
    private float eyeMovementSpeed = 1.74533f; //100 degrees in radians;

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

    [Header("Neck Settings")]
    [SerializeField]
    private Transform headTransform;
    [SerializeField]
    private Transform neckTransform;
    [SerializeField]
    private float neckXRotationLimit = 60f;
    [SerializeField]
    private float neckYRotationLimit = 80f;
    [SerializeField]
    private float neckZRotationLimit = 30f;
    [SerializeField]
    private float neckMovementSpeed = 0.698132f; // 40 degrees in radians

    private float headAngle;
    private RandomGaussian timeFixationDelayDist;
    private RandomGaussian headAccAngleDist;
    private RandomGaussian headDeaccAngleDist;

    private float blinkProbability;
    private float timeSinceLastBlink;

    private float fixationDelayTimer;
    private float fixationDelayTime;

    [SerializeField]
    private float headLiftTime = 2f;
    private float headLiftTimer;

    private Vector3 initialNeckForward;
    private Vector3 initialLeftEyeForward;
    private Vector3 initialRightEyeForward;

    private Vector3 lastFixation;
    private Vector3 currentFixation;

    private void Start()
    {
        timeFixationDelayDist = new RandomGaussian(0.15f, 0.1f);
        headAccAngleDist = new RandomGaussian(60, 10);
        headDeaccAngleDist = new RandomGaussian(15, 2);
        fixationDelayTime = timeFixationDelayDist.Get();
        fixationDelayTimer = fixationDelayTime;
        headLiftTimer = headLiftTime;
        timeSinceLastBlink = 0f;
        initialNeckForward = neckTransform.forward;
        StartCoroutine(Blink());
    }

    private void Update()
    {
        Vector3 fixationPoint = currentFixation = fixationController.GetCurrentFixationPoint();
        Vector3 middlePoint = (leftEyeTransform.forward + rightEyeTransform.forward).normalized;

        if (fixationPoint == Vector3.negativeInfinity)
        {
            SetRotation(neckTransform, initialNeckForward, neckMovementSpeed);
            ClampRotation(neckTransform, neckXRotationLimit, neckYRotationLimit, neckZRotationLimit);
        }

        //CheckBlink();

        Vector3 leftEyeTargetPosition = fixationPoint - leftEyeTransform.position;
        Vector3 rightEyeTargetPosition = fixationPoint - rightEyeTransform.position;

        // Rotate eyes towards the target
        SetRotation(leftEyeTransform, leftEyeTargetPosition, eyeMovementSpeed);
        SetRotation(rightEyeTransform, rightEyeTargetPosition, eyeMovementSpeed);

        // Clamp eye rotations
        ClampRotation(leftEyeTransform, eyeXRotationLimit, eyeYRotationLimit, eyeZRotationLimit);
        ClampRotation(rightEyeTransform, eyeXRotationLimit, eyeYRotationLimit, eyeZRotationLimit);       

        middlePoint = (leftEyeTransform.forward + rightEyeTransform.forward).normalized;
        headAngle = Vector3.Angle(neckTransform.forward, middlePoint);
        float headAngleThreshold = headAccAngleDist.Get();
        headAngleThreshold = Mathf.Clamp(headAngleThreshold, headAngleThreshold, 60);
        if (headAngle > headAngleThreshold)
        {
            // Rotate neck towards eyes middle point
            SetRotation(neckTransform, middlePoint, neckMovementSpeed);
            // Clamp neck rotation
            ClampRotation(neckTransform, neckXRotationLimit, neckYRotationLimit, neckZRotationLimit);
        }

        // Animate eye blendhsapes according to gaze direction
        AnimateGazeBlendShapes();
        lastFixation = fixationPoint;
    }

    private void SetRotation(Transform objectTransform, Vector3 targetRotation, float movementSpeed)
    {
        float singleStep = movementSpeed * Time.deltaTime;
        Vector3 newDirection = Vector3.RotateTowards(objectTransform.forward, targetRotation, singleStep, 0.0f);
        objectTransform.rotation = Quaternion.LookRotation(newDirection);
        //Debug.DrawRay(objectTransform.position, newDirection, Color.red);
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

    private void CheckBlink()
    {
        if (faceAnimator.GetCurrentAnimatorStateInfo(0).IsName("Blinking"))
            return;
        timeSinceLastBlink += Time.deltaTime;
        if (currentFixation != lastFixation)
        {
            blinkProbability = 0.05f;
        }
        else
        {
            if (timeSinceLastBlink < 2)
            {
                blinkProbability = timeSinceLastBlink/2 * 0.0009f + 0.0001f;
            }
            else if (timeSinceLastBlink < 3)
            {
                blinkProbability = (timeSinceLastBlink - 2) * 0.0009f + 0.0001f;
            }
            else
            {
                blinkProbability = 0.01f;
            }
        }
        if (blinkProbability > Random.Range(0, 1))
        {
            float blinkDuration = Random.Range(0.25f, 1);
            faceAnimator.SetFloat("speedMultiplier", blinkDuration);
            faceAnimator.Play("Blinking");
            timeSinceLastBlink = 0;
        }
    }

    private IEnumerator Blink()
    {
        float blinkInterval = UnityEngine.Random.Range(0.1f, 1);
        //Debug.Log("Blinking: " + blinkInterval);
        yield return new WaitForSeconds(blinkInterval);
        faceAnimator.Play("Blinking");
        yield return Blink();
        yield return null;
    }

    private void AnimateGazeBlendShapes()
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

    private float NormalizeBlendshapeValue(float value, float max, float min=0)
    {
        return 100 * Mathf.Abs(value - min)/(max - min);
    }
}
