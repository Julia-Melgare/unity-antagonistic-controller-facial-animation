using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntagonisticArmComplexV2 : MonoBehaviour
{
    [Header("Antagonistic - X")]
    public bool applyTorqueX;
    public float torqueAppliedX;
    public float pLX;
    public float pHX;
    public float iX;
    public float dX;
    public float minAngleX;
    public float maxAngleX;
    public float slopeX;
    public float interceptX;
    public float eqAngleX;
    private AntagonisticController _AntPIDX;

    [Header("Antagonistic - Y")]
    public bool applyTorqueY;
    public float torqueAppliedY;
    public float pLY;
    public float pHY;
    public float iY;
    public float dY;
    public float minAngleY;
    public float maxAngleY;
    public float slopeY;
    public float interceptY;
    public float eqAngleY;
    private AntagonisticController _AntPIDY;

    [Header("Antagonistic - Z")]
    public bool applyTorqueZ;
    public float torqueAppliedZ;
    public float pLZ;
    public float pHZ;
    public float iZ;
    public float dZ;
    public float minAngleZ;
    public float maxAngleZ;
    public float slopeZ;
    public float interceptZ;
    public float eqAngleZ;
    private AntagonisticController _AntPIDZ;

    [Header("Debug")]
    public bool toogleRays = false;
    public bool printTorqueX = false;
    public bool printTorqueY = false;
    public bool printTorqueZ = false;
    public Vector3 currentAngle;
    public Vector3 currentAngle2;
    public Vector3 currentAngle3;
    public Rigidbody _rbAnt;
    public ConfigurableJoint _jointAnt;
    public Transform sphereAnt;

    [Header("Experimental")]
    public Vector3 distance3D;
    public bool applyExternalForce;
    public Vector3 externalForce;
    public float externalTorqueMagnitudeX;
    public Vector3 externalTorqueVector;
    public Vector3 externalTorqueVectorLocal;
    public bool applyGravity;
    public Vector3 gravityAcc;
    public float gravityTorqueMagnitudeX;
    public Vector3 gravityTorqueVector;
    public Vector3 gravityTorqueVectorLocal;

    // Start is called before the first frame update
    void Start()
    {
        _AntPIDX = new AntagonisticController(pLX, pHX, iX, dX);
        _AntPIDY = new AntagonisticController(pLY, pHY, iY, dY);
        _AntPIDZ = new AntagonisticController(pLZ, pHZ, iZ, dZ);
    }

    // Update is called once per frame
    void Update()
    {
        if (toogleRays)
        {
            // Draw equilibrium angle (GREEN)
            Debug.DrawRay(transform.position,
                          Quaternion.AngleAxis(eqAngleZ, transform.parent.forward) * Quaternion.AngleAxis(eqAngleX, transform.parent.right) * transform.parent.up * 10f,
                          Color.green);

            // Draw up-axis of body limb (BLUE)
            Debug.DrawRay(transform.position, transform.up * 10f, Color.blue);

            // Draw limits of the antagonistic controller
            if (applyTorqueX)
            {
                Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxAngleX, transform.parent.right) * transform.parent.up * 10f, Color.red);
                Debug.DrawRay(transform.position, Quaternion.AngleAxis(minAngleX, transform.parent.right) * transform.parent.up * 10f, Color.red);
            }

            //if (applyTorqueY)
            //{
            //    Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxAngleY, transform.parent.forward) * transform.parent.up * 10f, Color.red);
            //    Debug.DrawRay(transform.position, Quaternion.AngleAxis(minAngleY, transform.parent.forward) * transform.parent.up * 10f, Color.red);
            //}

            if (applyTorqueZ)
            {
                Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxAngleZ, transform.parent.forward) * transform.parent.up * 10f, Color.red);
                Debug.DrawRay(transform.position, Quaternion.AngleAxis(minAngleZ, transform.parent.forward) * transform.parent.up * 10f, Color.red);
            }

            // Draw line conecting root with COM
            Debug.DrawLine(transform.position, _rbAnt.worldCenterOfMass, Color.yellow);

            // Draw external force
            //Debug.DrawRay(_rbAnt.worldCenterOfMass, _rbAnt.mass * gravityAcc, Color.yellow);
            //Debug.DrawRay(_rbAnt.worldCenterOfMass, externalForce, Color.magenta);

            // Draw torque produced by external
            //Debug.DrawRay(transform.position, gravityTorqueVector, Color.yellow);
            //Debug.DrawRay(transform.position, externalTorqueVector, Color.magenta);
        }
    }

    public static Vector3 GetSignedEulerAngles(Vector3 angles)
    {
        Vector3 signedAngles = Vector3.zero;
        for (int i = 0; i < 3; i++)
        {
            signedAngles[i] = (angles[i] + 180f) % 360f - 180f;
        }
        return -signedAngles;
    }

    public float CalcEulerSafeX(float x)
    {
        if (x >= -90 && x <= 90)
            return x;
        x = x % 180;
        if (x > 0)
            x -= 180;
        else
            x += 180;
        return x;
    }

    public Vector3 EulerSafeX(Vector3 eulerAngles)
    {
        eulerAngles.x = CalcEulerSafeX(eulerAngles.x);
        return eulerAngles;
    }

    private void FixedUpdate()
    {
        // Controller Gains
        _AntPIDX.KI = iX;
        _AntPIDX.KD = dX;
        _AntPIDY.KI = iY;
        _AntPIDY.KD = dY;
        _AntPIDZ.KI = iZ;
        _AntPIDZ.KD = dZ;

        // Enable/Unable Gravity
        _rbAnt.useGravity = applyGravity;

        // Get current joint angle
        currentAngle = jointRotation(_jointAnt);
        //currentAngle2 = GetSignedEulerAngles(transform.rotation.eulerAngles);
        //currentAngle3 = EulerSafeX(currentAngle);
        Debug.Log("currentAngle: " + currentAngle);
        //Debug.Log("currentAngle2: " + currentAngle2);
        //Debug.Log("currentAngle3: " + currentAngle3);


        // Distance from root to the RB COM
        distance3D = _rbAnt.worldCenterOfMass - transform.position;

        // 1. Gravity force and generated torque
        gravityAcc = Physics.gravity;
        gravityTorqueMagnitudeX = Vector3.Magnitude(_rbAnt.mass * gravityAcc) * Vector3.Distance(transform.parent.position, _rbAnt.worldCenterOfMass) * Mathf.Sin((90 - currentAngle.x) * Mathf.Deg2Rad); // Debug
        gravityTorqueVector = Vector3.Cross(distance3D, _rbAnt.mass * gravityAcc); // Remember: wrt. global coord. system
        gravityTorqueVectorLocal = transform.InverseTransformDirection(gravityTorqueVector); // Remember: wrt. local coord. system

        // 2. External Forces and generated torque
        externalTorqueMagnitudeX = Vector3.Magnitude(externalForce) * Vector3.Distance(sphereAnt.position, _rbAnt.worldCenterOfMass) * Mathf.Sin((90 - currentAngle.x) * Mathf.Deg2Rad); // Debug - TODO: Angle could be wrong
        externalTorqueVector = Vector3.Cross(distance3D, externalForce); // Remember: wrt. global coord. system
        externalTorqueVectorLocal = transform.InverseTransformDirection(externalTorqueVector); // Remember: wrt. local coord. system

        // Applying forces: Gravity is applied by Unity in the RB. For the external forces, we add them to the RB.
        if (applyExternalForce)
            _rbAnt.AddForce(externalForce, ForceMode.Force);

        //------//

        // Now, we create a controller for each axis. Each will contain an intercept and slope.
        // Here, we define the torques that we expect the controller to counteract.

        if (applyTorqueX)
        {
            // With "-" because Unity uses left-hand rule. I leave the debugs lines with left-hand.
            interceptX = (-gravityTorqueVectorLocal.x - externalTorqueVectorLocal.x) / (maxAngleX - eqAngleX);
            slopeX = (minAngleX - eqAngleX) / (eqAngleX - maxAngleX);

            // Isoline
            pHX = pLX * slopeX + interceptX;
            _AntPIDX.KPH = pHX;
            _AntPIDX.KPL = pLX;

            float angleLowErrorX = minAngleX - currentAngle.x;
            float angleHighErrorX = maxAngleX - currentAngle.x;
            if (printTorqueX)
            {
                Debug.Log("currentAngle.x: " + currentAngle.x);
                Debug.Log("angleLowErrorX: " + (angleLowErrorX));
                Debug.Log("angleHighErrorX: " + (angleHighErrorX));
            }

            torqueAppliedX = _AntPIDX.GetOutput(angleLowErrorX, angleHighErrorX, Time.fixedDeltaTime);
            if (printTorqueX)
            {
                Debug.Log("torqueAppliedX in Ant: " + torqueAppliedX);
                Debug.Log("gravityTorqueVectorLocal.x generated by gravity: " + gravityTorqueVectorLocal.x);
                Debug.Log("externalTorqueVectorLocal.x generated by extF: " + externalTorqueVectorLocal.x);
                Debug.Log("Torque difference: " + (gravityTorqueVectorLocal.x + externalTorqueVectorLocal.x + torqueAppliedX));
                Debug.Log("---------------");
            }
        }

        if (applyTorqueY)
        {
            interceptY = (-gravityTorqueVectorLocal.y - externalTorqueVectorLocal.y) / (maxAngleY - eqAngleY);
            slopeY = (minAngleY - eqAngleY) / (eqAngleY - maxAngleY);

            // Isoline
            pHY = pLY * slopeY + interceptY;
            _AntPIDY.KPH = pHY;
            _AntPIDY.KPL = pLY;

            float angleLowErrorY = minAngleY - currentAngle.y;
            float angleHighErrorY = maxAngleY - currentAngle.y;
            if (printTorqueY)
            {
                Debug.Log("currentAngle.y: " + currentAngle.y);
                Debug.Log("angleLowErrorY: " + angleLowErrorY);
                Debug.Log("angleHighErrorY: " + angleHighErrorY);
            }

            // CAUTION -> Had to switch High and Low errors...
            torqueAppliedY = _AntPIDY.GetOutput(angleHighErrorY, angleLowErrorY, Time.fixedDeltaTime);
            if (printTorqueY)
            {
                Debug.Log("torqueAppliedY in Ant: " + torqueAppliedY);
                Debug.Log("gravityTorqueVectorLocal.y generated by gravity: " + gravityTorqueVectorLocal.y);
                Debug.Log("externalTorqueVectorLocal.y generated by extF: " + externalTorqueVectorLocal.y);
                Debug.Log("Torque difference: " + (gravityTorqueVectorLocal.y + externalTorqueVectorLocal.y + torqueAppliedY));
                Debug.Log("---------------");
            }
        }

        if (applyTorqueZ)
        {
            interceptZ = (-gravityTorqueVectorLocal.z - externalTorqueVectorLocal.z) / (maxAngleZ - eqAngleZ);
            slopeZ = (minAngleZ - eqAngleZ) / (eqAngleZ - maxAngleZ);

            // Isoline
            pHZ = pLZ * slopeZ + interceptZ;
            _AntPIDZ.KPH = pHZ;
            _AntPIDZ.KPL = pLZ;

            float angleLowErrorZ = minAngleZ - currentAngle.z;
            float angleHighErrorZ = maxAngleZ - currentAngle.z;
            if (printTorqueZ)
            {
                Debug.Log("currentAngle.z: " + currentAngle.z);
                Debug.Log("angleLowErrorZ: " + angleLowErrorZ);
                Debug.Log("angleHighErrorZ: " + angleHighErrorZ);
            }

            torqueAppliedZ = _AntPIDZ.GetOutput(angleLowErrorZ, angleHighErrorZ, Time.fixedDeltaTime);
            if (printTorqueZ)
            {
                Debug.Log("torqueAppliedZ in Ant: " + torqueAppliedZ);
                Debug.Log("gravityTorqueVectorLocal.z generated by gravity: " + gravityTorqueVectorLocal.z);
                Debug.Log("externalTorqueVectorLocal.z generated by extF: " + externalTorqueVectorLocal.z);
                Debug.Log("Torque difference: " + (gravityTorqueVectorLocal.z + externalTorqueVectorLocal.z + torqueAppliedZ));
                Debug.Log("---------------");
            }

        }

        // Applying the relative torques
        if (applyTorqueX)
            _rbAnt.AddRelativeTorque(torqueAppliedX * Vector3.right);

        //if (applyTorqueY)
        //    _rbAnt.AddRelativeTorque(torqueAppliedY * Vector3.forward);

        //if (applyTorqueZ)
        //    _rbAnt.AddRelativeTorque(torqueAppliedZ * Vector3.forward);
    }

    public Quaternion getJointRotation(ConfigurableJoint joint)
    {
        //Debug.Log("joint.axis: " + joint.axis);
        //Debug.Log("joint.connectedBody.transform.rotation.eulerAngles: " + joint.connectedBody.transform.rotation.eulerAngles);
        return (Quaternion.FromToRotation(joint.axis, joint.connectedBody.transform.rotation.eulerAngles));
    }

    public float to180(float v)
    {
        if (v > 180)
        {
            v = v - 360;
        }
        return v;
    }
    Vector3 jointRotation(ConfigurableJoint joint)
    {
        Quaternion localRotation = Quaternion.Inverse(this.transform.parent.rotation) * this.transform.rotation;
        Debug.Log("localRotation: " + localRotation.eulerAngles);

        //Quaternion jointBasis = Quaternion.LookRotation(joint.secondaryAxis, Vector3.Cross(joint.axis, joint.secondaryAxis));
        //Debug.Log("jointBasis: " + jointBasis.eulerAngles);
        //Quaternion jointBasisInverse = Quaternion.Inverse(jointBasis);
        //var rotation = (jointBasisInverse * Quaternion.Inverse(joint.connectedBody.rotation) * joint.GetComponent<Rigidbody>().transform.rotation * jointBasis).eulerAngles;
        //Debug.Log("rotation: " + rotation);

        //return new Vector3(to180(rotation.x), to180(rotation.z), to180(rotation.y));
        return new Vector3(to180(localRotation.eulerAngles.x), to180(localRotation.eulerAngles.y), to180(localRotation.eulerAngles.z));
        //return new Vector3(localRotation.eulerAngles.x, localRotation.eulerAngles.y, localRotation.eulerAngles.z);
    }
}
