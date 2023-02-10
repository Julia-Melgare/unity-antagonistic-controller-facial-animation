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
    private Animator faceAnimator;
    [SerializeField]
    private Transform neckTransform;


    [SerializeField]
    private float blinkIntervalMin = 0.1f,  blinkIntervalMax = 1f;
    [SerializeField]
    private float neckXRotationLimit = 70f;
    [SerializeField]
    private float neckYRotationLimit = 70f;
    [SerializeField]
    private float neckZRotationLimit = 30f;

    [SerializeField]
    private float neckMovementSpeed = 1.5f;

    private Vector3 initialNeckForward;

    private void Start()
    {
        StartCoroutine(Blink());
        initialNeckForward = neckTransform.forward;
    }

    private void Update()
    {
        // Find nearest obstacle
        Collider nearestObstacle = null;
        if (safetyRegionLeft.targetObstacle.obstacle != null && safetyRegionRight.targetObstacle.obstacle != null)
           nearestObstacle = safetyRegionLeft.targetObstacle.distance < safetyRegionRight.targetObstacle.distance ? safetyRegionLeft.targetObstacle.obstacle : safetyRegionRight.targetObstacle.obstacle;
        else
           nearestObstacle = safetyRegionLeft.targetObstacle.obstacle != null ? safetyRegionLeft.targetObstacle.obstacle : safetyRegionRight.targetObstacle.obstacle;

        // Set neck rotation
        Vector3 targetDirection = transform.forward + new Vector3(0, initialNeckForward.y, 0);

        if (nearestObstacle != null) targetDirection = nearestObstacle.transform.position - neckTransform.position;
        float singleNeckStep = neckMovementSpeed * Time.deltaTime;
        Vector3 newNeckDirection = Vector3.RotateTowards(neckTransform.forward, targetDirection, singleNeckStep, 0.0f);
        Debug.DrawRay(neckTransform.position, newNeckDirection, Color.red);
        neckTransform.rotation = Quaternion.LookRotation(newNeckDirection);

        //Clamp neck rotation
        Vector3 localNeckRotation = neckTransform.localEulerAngles;
        float xRotation = localNeckRotation.x > 180 ? localNeckRotation.x - 360 : localNeckRotation.x; 
        float yRotation = localNeckRotation.y > 180 ? localNeckRotation.y - 360 : localNeckRotation.y; 
        float zRotation = localNeckRotation.z > 180 ? localNeckRotation.z - 360 : localNeckRotation.z; 
        neckTransform.localEulerAngles = new Vector3
            (
                Mathf.Clamp(xRotation, -neckXRotationLimit, neckXRotationLimit),
                Mathf.Clamp(yRotation, -neckYRotationLimit, neckYRotationLimit),
                Mathf.Clamp(zRotation, -neckZRotationLimit, neckZRotationLimit)
            );
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
