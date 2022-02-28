/****************************************************
 * File: JointController.cs
   * Author: Eduardo Alvarado
   * Email: eduardo.alvarado-pinero@polytechnique.edu
   * Date: Created by LIX on 27/01/2021
   * Project: ** WORKING TITLE **
   * Last update: 18/02/2022
*****************************************************/

/* Status: STABLE */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JointController : MonoBehaviour
{

    #region Read-only & Static Fields

    // Normal PD Controller
    private readonly PDController _normalPDController = new PDController(1f, 0f, 0.1f);

    // Antagonistic PD Controller middle class array
    private readonly JointControllerImitation _antagonisticControllerXYZ = new JointControllerImitation(1f, 0.0f, 0.0f, 0.01f,
                                                                                                        1f, 0.0f, 0.0f, 0.01f,
                                                                                                        1f, 0.0f, 0.0f, 0.01f);

    // Others
    private ConfigurableJoint _jointAnt;
    private Transform _currentTransform;
    private Transform _kinematicTransform;
    private Rigidbody _objectRigidbody;

    // Orientations
    private Quaternion _initialLocalOrientation;
    private Quaternion _initialGlobalOrientation;
    private Quaternion _currentLocalOrientation;
    private Quaternion _currentGlobalOrientation;
    private Quaternion _kinematicLocalOrientation;
    private Quaternion _kinematicGlobalOrientation;

    // For Normal PD Controller
    private Quaternion newRotationLocal;
    private Quaternion newRotationGlobal;

    // Window Graph - Right Hand
    private WindowGraph _rightHandGraph;
    private RectTransform _rightHandGraphContainer;
    private GameObject _rightHandPointX;
    private GameObject _rightHandLineX;
    private GameObject _rightHandLineXCurrent;
    private GameObject _rightHandPointY;
    private GameObject _rightHandLineY;
    private GameObject _rightHandLineYCurrent;
    private GameObject _rightHandPointZ;
    private GameObject _rightHandLineZ;
    private GameObject _rightHandLineZCurrent;

    private Toggle rightHandToggleX;
    private Toggle rightHandToggleY;
    private Toggle rightHandToggleZ;

    // Window Graph - Right Fore Arm
    private WindowGraph _rightForeArmGraph;
    private RectTransform _rightForeArmGraphContainer;
    private GameObject _rightForeArmPointX;
    private GameObject _rightForeArmLineX;
    private GameObject _rightForeArmLineXCurrent;
    private GameObject _rightForeArmPointY;
    private GameObject _rightForeArmLineY;
    private GameObject _rightForeArmLineYCurrent;
    private GameObject _rightForeArmPointZ;
    private GameObject _rightForeArmLineZ;
    private GameObject _rightForeArmLineZCurrent;

    private Toggle rightForeArmToggleX;
    private Toggle rightForeArmToggleY;
    private Toggle rightForeArmToggleZ;

    // Window Graph - Right Arm
    private WindowGraph _rightArmGraph;
    private RectTransform _rightArmGraphContainer;
    private GameObject _rightArmPointX;
    private GameObject _rightArmLineX;
    private GameObject _rightArmLineXCurrent;
    private GameObject _rightArmPointY;
    private GameObject _rightArmLineY;
    private GameObject _rightArmLineYCurrent;
    private GameObject _rightArmPointZ;
    private GameObject _rightArmLineZ;
    private GameObject _rightArmLineZCurrent;

    private Toggle rightArmToggleX;
    private Toggle rightArmToggleY;
    private Toggle rightArmToggleZ;

    #endregion

    #region Instance Fields

    public float DELTATIME = 0.02f;

    public enum Controller
    {
        DefaultPDController, NormalPDController, AntagonisticController
    }

    [Header("General Settings")]
    public Controller controllerType;
    public Transform kinematicLimb;

    [Header("Ragdoll Limbs")]
    public GameObject hand;
    public GameObject foreArm;
    public GameObject arm;

    [Header("Default PD Controller - Settings")]
    public bool activateDefaultPD;
    public float spring;
    public float damper;

    [Header("Normal PD Controller - Settings")]
    public bool activateNormalPD;
    public bool applyNormalTorque;
    public Vector3 requiredTorque;
    public bool globalMode = true;
    public float Kp;
    public float Ki;
    public float Kd;
    public bool deployDesiredRotation;
    public bool debugModeNormal;

    [Header("Antagonistic Controller - Settings")]
    public bool activateAntagonisticPD;
    public bool applyAntTorque;
    public Vector3 requiredAntagonisticLocalTorque;
    public bool debugModeAntagonistic;
    public float multGrav = 1f;

    [Header("Antagonistic Controller - Settings - X")]
    public float pLX;
    public float pHX;
    public float iX;
    public float dX;
    public float slopeX; // Before, variable used for each controller separatelly - now just to retrieve from lower class
    public float interceptX;
    public float minSoftLimitX;
    public float maxSoftLimitX;
    public float minHardLimitX;
    public float maxHardLimitX;
    public bool drawLimitsX;
    public float slopeXCurrent;
    public float interceptXCurrent;
    //private bool applyAntTorqueX;
    //private float torqueAppliedX;

    [Header("Antagonistic Controller - Settings - Y")]
    public float pLY;
    public float pHY;
    public float iY;
    public float dY;
    public float slopeY; // Before, variable used for each controller separatelly - now just to retrieve from lower class
    public float interceptY;
    public float minSoftLimitY;
    public float maxSoftLimitY;
    public float minHardLimitY;
    public float maxHardLimitY;
    public bool drawLimitsY;
    public float slopeYCurrent;
    public float interceptYCurrent;
    //private bool applyAntTorqueY;
    //private float torqueAppliedY;

    [Header("Antagonistic Controller - Settings - Z")]
    public float pLZ;
    public float pHZ;
    public float iZ;
    public float dZ;
    public float slopeZ; // Before, variable used for each controller separatelly - now just to retrieve from lower class
    public float interceptZ;
    public float minSoftLimitZ;
    public float maxSoftLimitZ;
    public float minHardLimitZ;
    public float maxHardLimitZ;
    public bool drawLimitsZ;
    public float slopeZCurrent;
    public float interceptZCurrent;
    //private bool applyAntTorqueZ;
    //private float torqueAppliedZ;

    [Header("External Forces")]
    public Vector3 distance3D;
    public Vector3 gravityAcc;
    public Vector3 gravityTorqueVector;
    public Vector3 gravityTorqueVectorLocal;

    #endregion

    #region Instance Properties

    public Quaternion DesiredLocalRotation { get; set; }

    #endregion

    #region Unity Methods

    private void Awake()
    {
        this._currentTransform = transform;
        this._kinematicTransform = kinematicLimb.transform;
        this._objectRigidbody = GetComponent<Rigidbody>();
        this._jointAnt = GetComponent<ConfigurableJoint>();

        this._rightHandGraph = GameObject.Find("WindowGraphRightHand").GetComponent<WindowGraph>();
        this._rightHandGraphContainer = GameObject.Find("GraphContainerRightHand").GetComponent<RectTransform>();

        this._rightForeArmGraph = GameObject.Find("WindowGraphRightForeArm").GetComponent<WindowGraph>();
        this._rightForeArmGraphContainer = GameObject.Find("GraphContainerRightForeArm").GetComponent<RectTransform>();

        this._rightArmGraph = GameObject.Find("WindowGraphRightArm").GetComponent<WindowGraph>();
        this._rightArmGraphContainer = GameObject.Find("GraphContainerRightArm").GetComponent<RectTransform>();

        // Initial Orientations - They do not update!
        this._initialLocalOrientation = transform.localRotation;
        this._initialGlobalOrientation = transform.rotation;
    }

    private void Start()
    {
        #region UI

        // Get Toggle UI
        rightHandToggleX = GameObject.Find("RightHandToggleX").GetComponent<Toggle>();
        rightHandToggleY = GameObject.Find("RightHandToggleY").GetComponent<Toggle>();
        rightHandToggleZ = GameObject.Find("RightHandToggleZ").GetComponent<Toggle>();
        rightForeArmToggleX = GameObject.Find("RightForeArmToggleX").GetComponent<Toggle>();
        rightForeArmToggleY = GameObject.Find("RightForeArmToggleY").GetComponent<Toggle>();
        rightForeArmToggleZ = GameObject.Find("RightForeArmToggleZ").GetComponent<Toggle>();
        rightArmToggleX = GameObject.Find("RightArmToggleX").GetComponent<Toggle>();
        rightArmToggleY = GameObject.Find("RightArmToggleY").GetComponent<Toggle>();
        rightArmToggleZ = GameObject.Find("RightArmToggleZ").GetComponent<Toggle>();

        rightHandToggleX.onValueChanged.AddListener(delegate
        {
            RightHandToggleXValueChanged(rightHandToggleX);
        });

        rightHandToggleY.onValueChanged.AddListener(delegate
        {
            RightHandToggleYValueChanged(rightHandToggleY);
        });

        rightHandToggleZ.onValueChanged.AddListener(delegate
        {
            RightHandToggleZValueChanged(rightHandToggleZ);
        });

        rightForeArmToggleX.onValueChanged.AddListener(delegate
        {
            RightForeArmToggleXValueChanged(rightForeArmToggleX);
        });

        rightForeArmToggleY.onValueChanged.AddListener(delegate
        {
            RightForeArmToggleYValueChanged(rightForeArmToggleY);
        });

        rightForeArmToggleZ.onValueChanged.AddListener(delegate
        {
            RightForeArmToggleZValueChanged(rightForeArmToggleZ);
        });

        rightArmToggleX.onValueChanged.AddListener(delegate
        {
            RightArmToggleXValueChanged(rightArmToggleX);
        });

        rightArmToggleY.onValueChanged.AddListener(delegate
        {
            RightArmToggleYValueChanged(rightArmToggleY);
        });

        rightArmToggleZ.onValueChanged.AddListener(delegate
        {
            RightArmToggleZValueChanged(rightArmToggleZ);
        });

        // Window Graph - Right Hand
        if (this.gameObject.CompareTag("RightHand"))
        {
            _rightHandPointX = _rightHandGraph.CreateCircle(_rightHandGraphContainer, new Vector2(0, 0), Color.red, "rightHandPointX");
            _rightHandLineX = _rightHandGraph.CreateLine(_rightHandGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.red, "rightHandLineX");
            _rightHandLineXCurrent = _rightHandGraph.CreateLine(_rightHandGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.black, "rightHandLineXCurrent");
            _rightHandPointY = _rightHandGraph.CreateCircle(_rightHandGraphContainer, new Vector2(0, 0), Color.green, "rightHandPointY");
            _rightHandLineY = _rightHandGraph.CreateLine(_rightHandGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.green, "rightHandLineY");
            _rightHandLineYCurrent = _rightHandGraph.CreateLine(_rightHandGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.black, "rightHandLineYCurrent");
            _rightHandPointZ = _rightHandGraph.CreateCircle(_rightHandGraphContainer, new Vector2(0, 0), Color.blue, "rightHandPointZ");
            _rightHandLineZ = _rightHandGraph.CreateLine(_rightHandGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.blue, "rightHandLineZ");
            _rightHandLineZCurrent = _rightHandGraph.CreateLine(_rightHandGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.black, "rightHandLineZCurrent");
        }

        // Window Graph - Right Fore Arm
        if (this.gameObject.CompareTag("RightForeArm"))
        {
            _rightForeArmPointX = _rightForeArmGraph.CreateCircle(_rightForeArmGraphContainer, new Vector2(0, 0), Color.red, "rightForeArmPointX");
            _rightForeArmLineX = _rightForeArmGraph.CreateLine(_rightForeArmGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.red, "rightForeArmLineX");
            _rightForeArmLineXCurrent = _rightForeArmGraph.CreateLine(_rightForeArmGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.black, "rightForeArmLineXCurrent");
            _rightForeArmPointY = _rightForeArmGraph.CreateCircle(_rightForeArmGraphContainer, new Vector2(0, 0), Color.green, "rightForeArmPointY");
            _rightForeArmLineY = _rightForeArmGraph.CreateLine(_rightForeArmGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.green, "rightForeArmLineY");
            _rightForeArmLineYCurrent = _rightForeArmGraph.CreateLine(_rightForeArmGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.black, "rightForeArmLineYCurrent");
            _rightForeArmPointZ = _rightForeArmGraph.CreateCircle(_rightForeArmGraphContainer, new Vector2(0, 0), Color.blue, "rightForeArmPointZ");
            _rightForeArmLineZ = _rightForeArmGraph.CreateLine(_rightForeArmGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.blue, "rightForeArmLineZ");
            _rightForeArmLineZCurrent = _rightForeArmGraph.CreateLine(_rightForeArmGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.black, "rightForeArmLineZCurrent");
        }

        // Window Graph - Right Arm
        if (this.gameObject.CompareTag("RightArm"))
        {
            _rightArmPointX = _rightArmGraph.CreateCircle(_rightArmGraphContainer, new Vector2(0, 0), Color.red, "rightArmPointX");
            _rightArmLineX = _rightArmGraph.CreateLine(_rightArmGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.red, "rightArmLineX");
            _rightArmLineXCurrent = _rightArmGraph.CreateLine(_rightArmGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.black, "rightArmLineXCurrent");
            _rightArmPointY = _rightArmGraph.CreateCircle(_rightArmGraphContainer, new Vector2(0, 0), Color.green, "rightArmPointY");
            _rightArmLineY = _rightArmGraph.CreateLine(_rightArmGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.green, "rightArmLineY");
            _rightArmLineYCurrent = _rightArmGraph.CreateLine(_rightArmGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.black, "rightArmLineYCurrent");
            _rightArmPointZ = _rightArmGraph.CreateCircle(_rightArmGraphContainer, new Vector2(0, 0), Color.blue, "rightArmPointZ");
            _rightArmLineZ = _rightArmGraph.CreateLine(_rightArmGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.blue, "rightArmLineZ");
            _rightArmLineZCurrent = _rightArmGraph.CreateLine(_rightArmGraphContainer, new Vector2(0, 0), new Vector2(0, 0), Color.black, "rightArmLineZCurrent");
        }

        #endregion
    }

    private void Update()
    {
        //Debug.Log("[UPDATE] FixedDeltaTime: " + Time.fixedDeltaTime.ToString("F4"));
        //Debug.Log("[UPDATE] DeltaTime: " + Time.deltaTime.ToString("F4"));

        #region Setting Limits

        // Set hard limit to the limit in the joints
        var lowAngularXJoint = _jointAnt.lowAngularXLimit;
        lowAngularXJoint.limit = minHardLimitX;
        _jointAnt.lowAngularXLimit = lowAngularXJoint;

        var highAngularXJoint = _jointAnt.highAngularXLimit;
        highAngularXJoint.limit = maxHardLimitX;
        _jointAnt.highAngularXLimit = highAngularXJoint;

        var angularYJoint = _jointAnt.angularYLimit;
        angularYJoint.limit = maxHardLimitY;
        _jointAnt.angularYLimit = angularYJoint;

        var angularZJoint = _jointAnt.angularZLimit;
        angularZJoint.limit = maxHardLimitZ;
        _jointAnt.angularZLimit = angularZJoint;

        if(drawLimitsX)
        {
            // TODO - Should we multiply for transform.localRotation after the AngleAxis? Does the range varies actually if we move the hand?
            Debug.DrawRay(transform.position, Quaternion.AngleAxis(minHardLimitX, transform.right) * transform.parent.up * 0.8f, Color.black);
            Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxHardLimitX, transform.right) * transform.parent.up * 0.8f, Color.black);

            Debug.DrawRay(transform.position, Quaternion.AngleAxis(minSoftLimitX, transform.right) * transform.parent.up * 0.4f, Color.red);
            Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxSoftLimitX, transform.right) * transform.parent.up * 0.4f, Color.green);
        }

        if (drawLimitsY)
        {
            Debug.DrawRay(transform.position, Quaternion.AngleAxis(minHardLimitY, transform.up) * transform.parent.up * 0.8f, Color.black);
            Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxHardLimitY, transform.up) * transform.parent.up * 0.8f, Color.black);

            Debug.DrawRay(transform.position, Quaternion.AngleAxis(minSoftLimitY, transform.up) * transform.parent.up * 0.4f, Color.red);
            Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxSoftLimitY, transform.up) * transform.parent.up * 0.4f, Color.green);
        }

        if (drawLimitsZ)
        {
            Debug.DrawRay(transform.position, Quaternion.AngleAxis(minHardLimitZ, transform.forward) * transform.parent.up * 0.8f, Color.black);
            Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxHardLimitZ, transform.forward) * transform.parent.up * 0.8f, Color.black);

            Debug.DrawRay(transform.position, Quaternion.AngleAxis(minSoftLimitZ, transform.forward) * transform.parent.up * 0.4f, Color.red);
            Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxSoftLimitZ, transform.forward) * transform.parent.up * 0.4f, Color.green);
        }

        #endregion

        #region Update Gains and Isoline

        // Normal PD Controller
        this._normalPDController.KP = this.Kp;
        this._normalPDController.KI = this.Ki;
        this._normalPDController.KD = this.Kd;

        // Antagonistic PD Controller with Middle Array
        this._antagonisticControllerXYZ.KPLX = this.pLX;
        this._antagonisticControllerXYZ.KIX = this.iX;
        this._antagonisticControllerXYZ.KDX = this.dX;

        this._antagonisticControllerXYZ.KPLY = this.pLY;
        this._antagonisticControllerXYZ.KIY = this.iY;
        this._antagonisticControllerXYZ.KDY = this.dY;

        this._antagonisticControllerXYZ.KPLZ = this.pLZ;
        this._antagonisticControllerXYZ.KIZ = this.iZ;
        this._antagonisticControllerXYZ.KDZ = this.dZ;

        // To get them back once they are calculated in the lower class and display them in the inspector
        this.pHX = this._antagonisticControllerXYZ.KPHX;
        this.pHY = this._antagonisticControllerXYZ.KPHY;
        this.pHZ = this._antagonisticControllerXYZ.KPHZ;

        // Getting slopes and intercepts
        this.slopeX = this._antagonisticControllerXYZ.SlopeX;
        this.slopeY = this._antagonisticControllerXYZ.SlopeY;
        this.slopeZ = this._antagonisticControllerXYZ.SlopeZ;
        this.interceptX = this._antagonisticControllerXYZ.InterceptX;
        this.interceptY = this._antagonisticControllerXYZ.InterceptY;
        this.interceptZ = this._antagonisticControllerXYZ.InterceptZ;

        // Getting slopes and intercepts from current orientation
        this.slopeXCurrent = this._antagonisticControllerXYZ.SlopeXCurrent;
        this.slopeYCurrent = this._antagonisticControllerXYZ.SlopeYCurrent;
        this.slopeZCurrent = this._antagonisticControllerXYZ.SlopeZCurrent;
        this.interceptXCurrent = this._antagonisticControllerXYZ.InterceptXCurrent;
        this.interceptYCurrent = this._antagonisticControllerXYZ.InterceptYCurrent;
        this.interceptZCurrent = this._antagonisticControllerXYZ.InterceptZCurrent;

        #endregion

        #region Plot

        // Window Graph Update - Right Hand
        if (this.gameObject.CompareTag("RightHand"))
        {
            _rightHandGraph.MoveCircle(_rightHandPointX, new Vector2(pLX, pHX));
            if (slopeX > 1f)
            {
                _rightHandGraph.MoveLine(_rightHandLineX, Vector2.zero, new Vector2((_rightHandGraphContainer.sizeDelta.x / slopeX), _rightHandGraphContainer.sizeDelta.y));
            }
            else if (slopeX < 1f)
            {
                _rightHandGraph.MoveLine(_rightHandLineX, Vector2.zero, new Vector2(_rightHandGraphContainer.sizeDelta.x, _rightHandGraphContainer.sizeDelta.y * slopeX));
            }
            if (slopeXCurrent > 1f)
            {
                _rightHandGraph.MoveLine(_rightHandLineXCurrent, Vector2.zero, new Vector2((_rightHandGraphContainer.sizeDelta.x / slopeXCurrent), _rightHandGraphContainer.sizeDelta.y));
            }
            else if (slopeXCurrent < 1f)
            {
                _rightHandGraph.MoveLine(_rightHandLineXCurrent, Vector2.zero, new Vector2(_rightHandGraphContainer.sizeDelta.x, _rightHandGraphContainer.sizeDelta.y * slopeXCurrent));
            }

            _rightHandGraph.MoveCircle(_rightHandPointY, new Vector2(pLY, pHY));
            if (slopeY > 1f)
            {
                _rightHandGraph.MoveLine(_rightHandLineY, Vector2.zero, new Vector2((_rightHandGraphContainer.sizeDelta.x / slopeY), _rightHandGraphContainer.sizeDelta.y));
            }
            else if (slopeY < 1f)
            {
                _rightHandGraph.MoveLine(_rightHandLineY, Vector2.zero, new Vector2(_rightHandGraphContainer.sizeDelta.x, _rightHandGraphContainer.sizeDelta.y * slopeY));
            }
            if (slopeYCurrent > 1f)
            {
                _rightHandGraph.MoveLine(_rightHandLineYCurrent, Vector2.zero, new Vector2((_rightHandGraphContainer.sizeDelta.x / slopeYCurrent), _rightHandGraphContainer.sizeDelta.y));
            }
            else if (slopeYCurrent < 1f)
            {
                _rightHandGraph.MoveLine(_rightHandLineYCurrent, Vector2.zero, new Vector2(_rightHandGraphContainer.sizeDelta.x, _rightHandGraphContainer.sizeDelta.y * slopeYCurrent));
            }

            _rightHandGraph.MoveCircle(_rightHandPointZ, new Vector2(pLZ, pHZ));
            if (slopeZ > 1f)
            {
                _rightHandGraph.MoveLine(_rightHandLineZ, Vector2.zero, new Vector2((_rightHandGraphContainer.sizeDelta.x / slopeZ), _rightHandGraphContainer.sizeDelta.y));
            }
            else if (slopeZ < 1f)
            {
                _rightHandGraph.MoveLine(_rightHandLineZ, Vector2.zero, new Vector2(_rightHandGraphContainer.sizeDelta.x, _rightHandGraphContainer.sizeDelta.y * slopeZ));
            }
            if (slopeZCurrent > 1f)
            {
                _rightHandGraph.MoveLine(_rightHandLineZCurrent, Vector2.zero, new Vector2((_rightHandGraphContainer.sizeDelta.x / slopeZCurrent), _rightHandGraphContainer.sizeDelta.y));
            }
            else if (slopeZCurrent < 1f)
            {
                _rightHandGraph.MoveLine(_rightHandLineZCurrent, Vector2.zero, new Vector2(_rightHandGraphContainer.sizeDelta.x, _rightHandGraphContainer.sizeDelta.y * slopeZCurrent));
            }
        }

        // Window Graph Update - Right Fore Arm
        if (this.gameObject.CompareTag("RightForeArm"))
        {
            _rightForeArmGraph.MoveCircle(_rightForeArmPointX, new Vector2(pLX, pHX));
            if (slopeX > 1f)
            {
                _rightForeArmGraph.MoveLine(_rightForeArmLineX, Vector2.zero, new Vector2((_rightForeArmGraphContainer.sizeDelta.x / slopeX), _rightForeArmGraphContainer.sizeDelta.y));
            }
            else if (slopeX < 1f)
            {
                _rightForeArmGraph.MoveLine(_rightForeArmLineX, Vector2.zero, new Vector2(_rightForeArmGraphContainer.sizeDelta.x, _rightForeArmGraphContainer.sizeDelta.y * slopeX));
            }
            if (slopeXCurrent > 1f)
            {
                _rightForeArmGraph.MoveLine(_rightForeArmLineXCurrent, Vector2.zero, new Vector2((_rightForeArmGraphContainer.sizeDelta.x / slopeXCurrent), _rightForeArmGraphContainer.sizeDelta.y));
            }
            else if (slopeXCurrent < 1f)
            {
                _rightForeArmGraph.MoveLine(_rightForeArmLineXCurrent, Vector2.zero, new Vector2(_rightForeArmGraphContainer.sizeDelta.x, _rightForeArmGraphContainer.sizeDelta.y * slopeXCurrent));
            }

            _rightForeArmGraph.MoveCircle(_rightForeArmPointY, new Vector2(pLY, pHY));
            if (slopeY > 1f)
            {
                _rightForeArmGraph.MoveLine(_rightForeArmLineY, Vector2.zero, new Vector2((_rightForeArmGraphContainer.sizeDelta.x / slopeY), _rightForeArmGraphContainer.sizeDelta.y));
            }
            else if (slopeY < 1f)
            {
                _rightForeArmGraph.MoveLine(_rightForeArmLineY, Vector2.zero, new Vector2(_rightForeArmGraphContainer.sizeDelta.x, _rightForeArmGraphContainer.sizeDelta.y * slopeY));
            }
            if (slopeYCurrent > 1f)
            {
                _rightForeArmGraph.MoveLine(_rightForeArmLineYCurrent, Vector2.zero, new Vector2((_rightForeArmGraphContainer.sizeDelta.x / slopeYCurrent), _rightForeArmGraphContainer.sizeDelta.y));
            }
            else if (slopeYCurrent < 1f)
            {
                _rightForeArmGraph.MoveLine(_rightForeArmLineYCurrent, Vector2.zero, new Vector2(_rightForeArmGraphContainer.sizeDelta.x, _rightForeArmGraphContainer.sizeDelta.y * slopeYCurrent));
            }

            _rightForeArmGraph.MoveCircle(_rightForeArmPointZ, new Vector2(pLZ, pHZ));
            if (slopeZ > 1f)
            {
                _rightForeArmGraph.MoveLine(_rightForeArmLineZ, Vector2.zero, new Vector2((_rightForeArmGraphContainer.sizeDelta.x / slopeZ), _rightForeArmGraphContainer.sizeDelta.y));
            }
            else if (slopeZ < 1f)
            {
                _rightForeArmGraph.MoveLine(_rightForeArmLineZ, Vector2.zero, new Vector2(_rightForeArmGraphContainer.sizeDelta.x, _rightForeArmGraphContainer.sizeDelta.y * slopeZ));
            }
            if (slopeZCurrent > 1f)
            {
                _rightForeArmGraph.MoveLine(_rightForeArmLineZCurrent, Vector2.zero, new Vector2((_rightForeArmGraphContainer.sizeDelta.x / slopeZCurrent), _rightForeArmGraphContainer.sizeDelta.y));
            }
            else if (slopeZCurrent < 1f)
            {
                _rightForeArmGraph.MoveLine(_rightForeArmLineZCurrent, Vector2.zero, new Vector2(_rightForeArmGraphContainer.sizeDelta.x, _rightForeArmGraphContainer.sizeDelta.y * slopeZCurrent));
            }
        }

        // Window Graph Update - Right Arm
        if (this.gameObject.CompareTag("RightArm"))
        {
            _rightArmGraph.MoveCircle(_rightArmPointX, new Vector2(pLX, pHX));
            if (slopeX > 1f)
            {
                _rightArmGraph.MoveLine(_rightArmLineX, Vector2.zero, new Vector2((_rightArmGraphContainer.sizeDelta.x / slopeX), _rightArmGraphContainer.sizeDelta.y));
            }
            else if (slopeX < 1f)
            {
                _rightArmGraph.MoveLine(_rightArmLineX, Vector2.zero, new Vector2(_rightArmGraphContainer.sizeDelta.x, _rightArmGraphContainer.sizeDelta.y * slopeX));
            }
            if (slopeXCurrent > 1f)
            {
                _rightArmGraph.MoveLine(_rightArmLineXCurrent, Vector2.zero, new Vector2((_rightArmGraphContainer.sizeDelta.x / slopeXCurrent), _rightArmGraphContainer.sizeDelta.y));
            }
            else if (slopeXCurrent < 1f)
            {
                _rightArmGraph.MoveLine(_rightArmLineXCurrent, Vector2.zero, new Vector2(_rightArmGraphContainer.sizeDelta.x, _rightArmGraphContainer.sizeDelta.y * slopeXCurrent));
            }

            _rightArmGraph.MoveCircle(_rightArmPointY, new Vector2(pLY, pHY));
            if (slopeY > 1f)
            {
                _rightArmGraph.MoveLine(_rightArmLineY, Vector2.zero, new Vector2((_rightArmGraphContainer.sizeDelta.x / slopeY), _rightArmGraphContainer.sizeDelta.y));
            }
            else if (slopeY < 1f)
            {
                _rightArmGraph.MoveLine(_rightArmLineY, Vector2.zero, new Vector2(_rightArmGraphContainer.sizeDelta.x, _rightArmGraphContainer.sizeDelta.y * slopeY));
            }
            if (slopeYCurrent > 1f)
            {
                _rightArmGraph.MoveLine(_rightArmLineYCurrent, Vector2.zero, new Vector2((_rightArmGraphContainer.sizeDelta.x / slopeYCurrent), _rightArmGraphContainer.sizeDelta.y));
            }
            else if (slopeYCurrent < 1f)
            {
                _rightArmGraph.MoveLine(_rightArmLineYCurrent, Vector2.zero, new Vector2(_rightArmGraphContainer.sizeDelta.x, _rightArmGraphContainer.sizeDelta.y * slopeYCurrent));
            }

            _rightArmGraph.MoveCircle(_rightArmPointZ, new Vector2(pLZ, pHZ));
            if (slopeZ > 1f)
            {
                _rightArmGraph.MoveLine(_rightArmLineZ, Vector2.zero, new Vector2((_rightArmGraphContainer.sizeDelta.x / slopeZ), _rightArmGraphContainer.sizeDelta.y));
            }
            else if (slopeZ < 1f)
            {
                _rightArmGraph.MoveLine(_rightArmLineZ, Vector2.zero, new Vector2(_rightArmGraphContainer.sizeDelta.x, _rightArmGraphContainer.sizeDelta.y * slopeZ));
            }
            if (slopeZCurrent > 1f)
            {
                _rightArmGraph.MoveLine(_rightArmLineZCurrent, Vector2.zero, new Vector2((_rightArmGraphContainer.sizeDelta.x / slopeZCurrent), _rightArmGraphContainer.sizeDelta.y));
            }
            else if (slopeZCurrent < 1f)
            {
                _rightArmGraph.MoveLine(_rightArmLineZCurrent, Vector2.zero, new Vector2(_rightArmGraphContainer.sizeDelta.x, _rightArmGraphContainer.sizeDelta.y * slopeZCurrent));
            }
        }

        #endregion
    }

    private void FixedUpdate()
    {
        //Debug.Log("[FIXED UPDATE] FixedDeltaTime: " + Time.fixedDeltaTime.ToString("F4"));
        //Debug.Log("[FIXED UPDATE] DeltaTime: " + Time.deltaTime.ToString("F4"));

        if (DesiredLocalRotation == null || this._currentTransform == null || this._objectRigidbody == null || this.kinematicLimb == null)
        {
            return;
        }

        #region Getting Orientations

        // Get kinematic orientations to be followed (Kinematic Model)
        _kinematicLocalOrientation = kinematicLimb.transform.localRotation;
        _kinematicGlobalOrientation = kinematicLimb.transform.rotation;

        // Get current orientations to be moved (Ragdoll Model)
        _currentLocalOrientation = transform.localRotation;
        _currentGlobalOrientation = transform.rotation;

        #endregion

        #region External Forces

        // Calculate forces relative to the RB - Distance from root to the COM

        // 1. Gravity force and generated torque
        gravityAcc = Physics.gravity * multGrav;

        if (this.CompareTag("RightHand"))
        {
            distance3D = _objectRigidbody.worldCenterOfMass - transform.position;
            gravityTorqueVector = Vector3.Cross(distance3D, _objectRigidbody.mass * gravityAcc); // Remember: wrt. global coord. system
            gravityTorqueVectorLocal = transform.InverseTransformDirection(gravityTorqueVector); // Remember: wrt. local coord. system // TODO REVIEW

            //Debug.DrawRay(_objectRigidbody.worldCenterOfMass, Vector3.up, Color.red);
            //Debug.DrawRay(transform.position, Vector3.up, Color.blue);
            Debug.DrawRay(transform.position, distance3D, Color.red);
            Debug.DrawRay(transform.position, gravityTorqueVectorLocal, Color.yellow);
        }
        else if (this.CompareTag("RightForeArm"))
        {
            distance3D = ((_objectRigidbody.worldCenterOfMass - transform.position) + (hand.GetComponent<Rigidbody>().worldCenterOfMass - transform.position)) / 2;
            gravityTorqueVector = Vector3.Cross(distance3D, (_objectRigidbody.mass + hand.GetComponent<Rigidbody>().mass) * gravityAcc); // Remember: wrt. global coord. system
            gravityTorqueVectorLocal = transform.InverseTransformDirection(gravityTorqueVector); // Remember: wrt. local coord. system // TODO REVIEW

            //Debug.DrawRay(_objectRigidbody.worldCenterOfMass, Vector3.up, Color.red);
            //Debug.DrawRay(transform.position, Vector3.up, Color.blue);
            Debug.DrawRay(transform.position, distance3D, Color.red);
            Debug.DrawRay(transform.position, gravityTorqueVectorLocal, Color.yellow);
        }

        #endregion

        #region Default PD Controller

        /*
         * 1. The first method (DefaultPDController) already sets the target at the configurable joint. 
         * Otherwise, turn off DefaultPDController and set joint back to normal
         */

        if ((controllerType == Controller.DefaultPDController) && (activateDefaultPD))
        {
            // [Local]
            DesiredLocalRotation = ConfigurableJointExtensions.GetTargetRotationLocal(_jointAnt, _kinematicLocalOrientation, _initialLocalOrientation, _currentLocalOrientation, transform);
            ConfigurableJointExtensions.SetTargetRotationLocal(_jointAnt, _kinematicLocalOrientation, _initialLocalOrientation);

            _jointAnt.rotationDriveMode = RotationDriveMode.XYAndZ;
            var angularXDrive = _jointAnt.angularXDrive;
            angularXDrive.positionSpring = spring;
            angularXDrive.positionDamper = damper;
            angularXDrive.maximumForce = Mathf.Infinity;
            _jointAnt.angularXDrive = angularXDrive;
            var angularYZDrive = _jointAnt.angularYZDrive;
            angularYZDrive.positionSpring = spring;
            angularYZDrive.positionDamper = damper;
            angularYZDrive.maximumForce = Mathf.Infinity;
            _jointAnt.angularYZDrive = angularYZDrive;
        }
        else
        {
            // [Local]
            DesiredLocalRotation = ConfigurableJointExtensions.GetTargetRotationLocal(_jointAnt, _kinematicLocalOrientation, _initialLocalOrientation, _currentLocalOrientation, transform);

            _jointAnt.targetRotation = Quaternion.identity;
            _jointAnt.rotationDriveMode = RotationDriveMode.XYAndZ;
            var angularXDrive = _jointAnt.angularXDrive;
            angularXDrive.positionSpring = 0f;
            angularXDrive.positionDamper = 0f;
            angularXDrive.maximumForce = Mathf.Infinity;
            _jointAnt.angularXDrive = angularXDrive;
            var angularYZDrive = _jointAnt.angularYZDrive;
            angularYZDrive.positionSpring = 0f;
            angularYZDrive.positionDamper = 0f;
            angularYZDrive.maximumForce = Mathf.Infinity;
            _jointAnt.angularYZDrive = angularYZDrive;
        }

        #endregion

        #region Normal PD Controller

        /* 
         * 2. The second method (NormalPDController) uses my own implementation for a PD controller using Forward pass with Euler's integration, and using Axis-Angle representation.
         */

        if ((controllerType == Controller.NormalPDController) && (activateNormalPD))
        {
            requiredTorque = ComputeRequiredTorque(_currentLocalOrientation,
                                                   _currentGlobalOrientation,
                                                   _kinematicLocalOrientation,
                                                   _kinematicGlobalOrientation,
                                                   DesiredLocalRotation,
                                                   this._objectRigidbody.angularVelocity,
                                                   gravityTorqueVectorLocal,
                                                   DELTATIME);


            if (debugModeNormal)
            {
                Debug.Log("[INFO: " + this.gameObject.name + "] Normal PD Controller (Angle-axis) requiredTorque: " + requiredTorque);
            }

            if (applyNormalTorque)
            {
                if (globalMode)
                    this._objectRigidbody.AddTorque(requiredTorque, ForceMode.Force); // Option (B) for Improved Torque [Global]
                else
                    this._objectRigidbody.AddRelativeTorque(requiredTorque, ForceMode.Force); // Option (B) for Improved Torque [Local]

                //this._objectRigidbody.AddRelativeTorque(requiredAngularAcceleration, ForceMode.Acceleration); // Option (A) for Torque [Local]
            }
        }

        #endregion

        #region Antagonistic Controller

        /* 
         * 3. The third method (AntagonisticController) uses my own implementation for a Antagonistic Controllers using Axis-Angle representation.
         */

        if ((controllerType == Controller.AntagonisticController) && (activateAntagonisticPD))
        {
            requiredAntagonisticLocalTorque = _antagonisticControllerXYZ.ComputeRequiredAntagonisticTorque(minSoftLimitX, maxSoftLimitX,
                                                                                                              minSoftLimitY, maxSoftLimitY,
                                                                                                              minSoftLimitZ, maxSoftLimitZ,
                                                                                                              minHardLimitX, maxHardLimitX,
                                                                                                              minHardLimitY, maxHardLimitY,
                                                                                                              minHardLimitZ, maxHardLimitZ,
                                                                                                              _currentLocalOrientation,
                                                                                                              _currentGlobalOrientation,
                                                                                                              _kinematicLocalOrientation,
                                                                                                              _kinematicGlobalOrientation,
                                                                                                              DesiredLocalRotation,
                                                                                                              this._objectRigidbody.angularVelocity,
                                                                                                              gravityTorqueVectorLocal,
                                                                                                              DELTATIME,
                                                                                                              debugModeAntagonistic,
                                                                                                              _currentTransform, _kinematicTransform);


            // 1. Is this torque Local
            //Debug.Log("1. requiredAntagonisticTorque (LOCAL): " + requiredAntagonisticLocalTorque.ToString("F4"));

            // TEST 1 - Keep torque in Local Space

            // 2. We rotate the torque by the Inertia Tensor Rotation
            Vector3 torqueRotatedLocal = _objectRigidbody.inertiaTensorRotation * requiredAntagonisticLocalTorque;
            //Debug.Log("requiredAntagonisticLocalTorque rotated by inertiaTensorRotation (torqueRotatedLocal): " + torqueRotatedLocal.ToString("F4"));

            // 3. We mutiply by the Inertia Tensor
            torqueRotatedLocal.Scale(_objectRigidbody.inertiaTensor);
            //Debug.Log("torqueRotatedLocal scaled by inertiaTensor, which is the torque (Local): " + torqueRotatedLocal.ToString("F4"));

            // 4. Rotate back the Inertia Tensor Rotation
            Vector3 torqueRotatedBackLocal = Quaternion.Inverse(_objectRigidbody.inertiaTensorRotation) * torqueRotatedLocal; // This is the final torque to apply
            //Debug.Log("torqueRotatedLocal rotated back by inertiaTensorRotation (torqueRotatedBackLocal): " + torqueRotatedBackLocal.ToString("F4"));

            // TEST 2 - Transform to Global 

            /*
            // 2. Convert rotation of inertia tensor to global (instead of leaving it it local and transforming the torque to local instead)
            Quaternion inertiaTensorRotationGlobal = _objectRigidbody.inertiaTensorRotation * transform.rotation; // 1.way MOVING TO GLOBAL 2.way SAME
            Debug.Log("2. inertiaTensorRotationGlobal: " + inertiaTensorRotationGlobal.ToString("F4"));

            // 3. Take torque to global and rotate by inertiaTensorRotationGlobal
            Vector3 torqueRotatedGlobal = Quaternion.Inverse(inertiaTensorRotationGlobal) * transform.TransformDirection(requiredAntagonisticLocalTorque); // 1.way WITH INVERSE 2.way WITHOUT INVERSE
            Debug.Log("3. requiredAntagonisticGlobalTorque: " + transform.TransformDirection(requiredAntagonisticLocalTorque).ToString("F4"));
            Debug.Log("3. torqueRotatedGlobal: " + torqueRotatedGlobal.ToString("F4")); // 1.way BRINGS IT BACK TO LOCAL, when we make the INVERSE ABOVE!!! 2.way ??

            // 4. We mutiply by the Inertia Tensor
            torqueRotatedGlobal.Scale(_objectRigidbody.inertiaTensor);
            Debug.Log("4. torqueRotatedGlobal scaled by inertiaTensor, which is the torque (Global): " + torqueRotatedGlobal.ToString("F4"));

            // 5. Rotate back the Inertia Tensor Rotation
            Vector3 torqueRotatedBackGlobal = _objectRigidbody.inertiaTensorRotation * torqueRotatedGlobal;  // 1.way WITHOUT INVERSE WORKING // 2.way WITH INVERSE NOT WORKING
            Debug.Log("5. torqueRotatedGlobal rotated back by inertiaTensorRotation (torqueRotatedBackGlobal): " + torqueRotatedBackGlobal.ToString("F4"));

            // 6. Bring back torque to local
            Vector3 torqueRotatedBackLocal = transform.InverseTransformDirection(torqueRotatedBackGlobal); // 1.way NOT WORKING 2.way NOT WORKING
            Debug.Log("6. torqueRotatedBackLocal: " + torqueRotatedBackLocal.ToString("F4"));
            */

            if (debugModeAntagonistic)
            {
                Debug.Log("[INFO: " + this.gameObject.name + "] Antagonistic PD Controller (Angle-axis) requiredAntagonisticTorque: " + requiredAntagonisticLocalTorque);
            }

            if (applyAntTorque)
            {
                //this._objectRigidbody.AddRelativeTorque(requiredAntagonisticLocalTorque, ForceMode.Force); // Needs 49.9 angular drag

                this._objectRigidbody.AddRelativeTorque(torqueRotatedBackLocal, ForceMode.Force); // TEST 1 We stay in local space -> WORKS
                //this._objectRigidbody.AddTorque(transform.TransformDirection(torqueRotatedBackLocal), ForceMode.Force); // TEST 1 in Global Space -> Also WORKS

                //this._objectRigidbody.AddRelativeTorque(torqueRotatedBackGlobal, ForceMode.Force); // TEST 2 Alternative
            }
        }

        #endregion
    }

    #endregion

    #region Old Instance Methods

    /// <summary>
    /// Compute torque using Normal PD Controller.
    /// </summary>
    /// <param name="currentLocalOrientation"></param>
    /// <param name="currentGlobalOrientation"></param>
    /// <param name="kinematicLocalOrientation"></param>
    /// <param name="kinematicGlobalOrientation"></param>
    /// <param name="desiredLocalRotation"></param>
    /// <param name="angularVelocity"></param>
    /// <param name="gravityTorqueVectorLocal"></param>
    /// <param name="fixedDeltaTime"></param>
    /// <returns></returns>
    private Vector3 ComputeRequiredTorque(Quaternion currentLocalOrientation, Quaternion currentGlobalOrientation, 
                                          Quaternion kinematicLocalOrientation, Quaternion kinematicGlobalOrientation,
                                          Quaternion desiredLocalRotation, Vector3 angularVelocity, Vector3 gravityTorqueVectorLocal, 
                                          float fixedDeltaTime)
    {
        #region Orientations and Rotations

        /*

        // Current Local Orientation in Angle-axis
        float currentAngle = 0.0f;
        Vector3 currentAxis = Vector3.zero;
        currentLocalOrientation.ToAngleAxis(out currentAngle, out currentAxis);

        // Target Local Orientation in Angle-axis
        float targetAngle = 0.0f;
        Vector3 targetAxis = Vector3.zero;
        kinematicLocalOrientation.ToAngleAxis(out targetAngle, out targetAxis);

        // Target Rotation in Angle-axis -> Not used, the quaternion is wrt. joint coordinate system, valid for integrated PD in the conf. joint only.
        float rotationAngle = 0.0f;
        Vector3 rotationAxis = Vector3.zero;
        desiredLocalRotation.ToAngleAxis(out rotationAngle, out rotationAxis); // <--- Wrong Quaternion, is wrt. joint coordinate system!

        */

        #endregion

        #region Rotations

        // Create Rotation from Current Local Orientation to Kinematic Local Orientation and convert to Angle-Axis
        newRotationLocal = Quaternion.Inverse(currentLocalOrientation) * kinematicLocalOrientation; // Works
        //newRotationLocal = kinematicLocalOrientation * Quaternion.Inverse(currentLocalOrientation); // Creates jitter above limits

        // Q can be the-long-rotation-around-the-sphere eg. 350 degrees
        // We want the equivalant short rotation eg. -10 degrees
        // Check if rotation is greater than 190 degees == q.w is negative
        if (newRotationLocal.w < 0)
        {
            // Convert the quaterion to eqivalent "short way around" quaterion
            newRotationLocal.x = -newRotationLocal.x;
            newRotationLocal.y = -newRotationLocal.y;
            newRotationLocal.z = -newRotationLocal.z;
            newRotationLocal.w = -newRotationLocal.w;
        }
        float rotationNewAngleLocal = 0.0f;
        Vector3 rotationNewAxisLocal = Vector3.zero;
        newRotationLocal.ToAngleAxis(out rotationNewAngleLocal, out rotationNewAxisLocal);
        rotationNewAxisLocal.Normalize();

        // Create Rotation from Current Global Orientation to Kinematic Global Orientation and convert to Angle-Axis
        newRotationGlobal = kinematicGlobalOrientation * Quaternion.Inverse(currentGlobalOrientation); // Works

        // Q can be the-long-rotation-around-the-sphere eg. 350 degrees
        // We want the equivalant short rotation eg. -10 degrees
        // Check if rotation is greater than 190 degees == q.w is negative
        if (newRotationGlobal.w < 0)
        {
            // Convert the quaterion to eqivalent "short way around" quaterion
            newRotationGlobal.x = -newRotationGlobal.x;
            newRotationGlobal.y = -newRotationGlobal.y;
            newRotationGlobal.z = -newRotationGlobal.z;
            newRotationGlobal.w = -newRotationGlobal.w;
        }
        float rotationNewAngleGlobal = 0.0f;
        Vector3 rotationNewAxisGlobal = Vector3.zero;
        newRotationGlobal.ToAngleAxis(out rotationNewAngleGlobal, out rotationNewAxisGlobal);
        rotationNewAxisGlobal.Normalize();

        #endregion

        #region Deploy Rotations - Test

        if (deployDesiredRotation)
        {
            // 1. DesiredLocalRotation
            //transform.localRotation = desiredLocalRotation; // Wrong! -> The rotation is in the joint space, while local rotation in local space.

            // 2. Apply rotation quaternion [Local]
            //transform.localRotation = newRotationLocal * transform.localRotation; // Works perfectly - Still some jittering which I don't know where it comes from

            // 3. Apply rotation quaternion [Global]
            //transform.rotation = newRotationGlobal * transform.rotation; // Does not work entirely, are global rotations - Still some jittering which I don't know where it comes from
        }

        #endregion

        #region Controller Errors

        // Estimate Angle Error Local
        float newRotationErrorLocal = rotationNewAngleLocal;

        //Debug.Log("[INFO: " + this.gameObject.name + "] newRotationErrorLocal: " + newRotationErrorLocal);

        // Rotation Axis Local
        //Debug.Log("[INFO: " + this.gameObject.name + "] rotationNewAxisLocal: " + rotationNewAxisLocal);
        //Debug.DrawRay(this.transform.position, rotationNewAxisLocal, Color.blue);

        // Estimate Angle Error Global
        float newRotationErrorGlobal = rotationNewAngleGlobal;

        //Debug.Log("[INFO: " + this.gameObject.name + "] newRotationErrorGlobal: " + newRotationErrorGlobal);

        // Rotation Axis Global
        //Debug.Log("[INFO: " + this.gameObject.name + "] rotationNewAxisGlobal: " + rotationNewAxisGlobal);
        //Debug.DrawRay(this.transform.position, rotationNewAxisGlobal, Color.blue);

        #endregion

        #region Torque Estimation

        /*       1. Normal Torque Estimation (A)     */
        /* ========================================= */

        // Normal Torque [Local]
        float torqueLocal = _normalPDController.GetOutput(newRotationErrorLocal, angularVelocity.magnitude, fixedDeltaTime);
        //Debug.Log("[INFO] torqueLocal * rotationNewAxis: " + (torqueLocal * rotationNewAxisLocal));

        /*     2. Improved Torque Estimation (B)     */
        /* ========================================= */

        // Improved Torque [Global] and [Local]
        Vector3 torqueImprovedGlobal = _normalPDController.GetOutputAxisAngle(newRotationErrorGlobal, rotationNewAxisGlobal, angularVelocity, fixedDeltaTime);
        Vector3 torqueImprovedLocal = _normalPDController.GetOutputAxisAngle(newRotationErrorLocal, rotationNewAxisLocal, angularVelocity, fixedDeltaTime);
        //Debug.Log("[INFO] torqueImprovedGlobal: " + torqueImprovedGlobal);
        //Debug.Log("[INFO] torqueImprovedLocal: " + torqueImprovedLocal);

        #endregion

        #region Inertia

        // Global -> WORKS (If doing it in world, we need to bring the rotation inertia to world. Then, inverse->no-inverse and have global.

        // Convert rotation of inertia tensor to global
        Quaternion rotInertia2World = _objectRigidbody.inertiaTensorRotation * transform.rotation;

        torqueImprovedGlobal = Quaternion.Inverse(rotInertia2World) * torqueImprovedGlobal;
        torqueImprovedGlobal.Scale(_objectRigidbody.inertiaTensor);
        torqueImprovedGlobal = rotInertia2World * torqueImprovedGlobal;
        //Debug.Log("[INFO] torqueImprovedGlobal: " + torqueImprovedGlobal);

        // Local -> TODO - inertiaTensorRotation already in local. Then, no-inverse->inverse and have local.
        torqueImprovedLocal = _objectRigidbody.inertiaTensorRotation * torqueImprovedLocal;
        torqueImprovedLocal.Scale(_objectRigidbody.inertiaTensor);
        torqueImprovedLocal = Quaternion.Inverse(_objectRigidbody.inertiaTensorRotation) * torqueImprovedLocal;
        // Debug.Log("[INFO] torqueImprovedLocal: " + torqueImprovedLocal);

        #endregion

        /*       1. Normal Torque Estimation (A)     */
        /* ========================================= */

        //return torqueLocal * rotationNewAxisLocal; //

        /*     2. Improved Torque Estimation (B)     */
        /* ========================================= */

        if (globalMode)
            return torqueImprovedGlobal; // Working fine
        else
            return torqueImprovedLocal; // Not entirely working
    }

    #endregion

    #region Instance Methods

    // Swing-Twist Decomposition - TODO 
    private Quaternion getRotationComponentAboutAxis(Quaternion rotation, Vector3 direction)
    {
        Vector3 rotationAxis = new Vector3(rotation.x, rotation.y, rotation.z);

        float dotProd = Vector3.Dot(rotationAxis, direction);

        // Shortcut calculation of `projection` requires `direction` to be normalized
        Vector3 projection = new Vector3(direction.x * dotProd, direction.y * dotProd, direction.z * dotProd);

        Quaternion twist = new Quaternion(
                projection.x, projection.y, projection.z, rotation.w).normalized;
        if (dotProd < 0.0)
        {
            // Ensure `twist` points towards `direction`
            twist.x = -twist.x;
            twist.y = -twist.y;
            twist.z = -twist.z;
            twist.w = -twist.w;
            // Rotation angle `twist.angle()` is now reliable
        }
        return twist;
    }

    #endregion

    #region Listeners

    void RightHandToggleXValueChanged(Toggle toggle)
    {
        if(this.gameObject.CompareTag("RightHand"))
        {
            if (toggle.isOn)
            {
                _rightHandGraph.SetTransparencyLine(_rightHandLineX, Color.red, 0.5f);
                _rightHandGraph.SetTransparencyPoint(_rightHandPointX, Color.red, 1f);

                _rightHandGraph.SetTransparencyLine(_rightHandLineXCurrent, Color.black, 0.5f);
            }
            else
            {
                _rightHandGraph.SetTransparencyLine(_rightHandLineX, Color.red, 0f);
                _rightHandGraph.SetTransparencyPoint(_rightHandPointX, Color.red, 0f);

                _rightHandGraph.SetTransparencyLine(_rightHandLineXCurrent, Color.black, 0f);
            }
        }
    }

    void RightHandToggleYValueChanged(Toggle toggle)
    {
        if (this.gameObject.CompareTag("RightHand"))
        {
            if (toggle.isOn)
            {
                _rightHandGraph.SetTransparencyLine(_rightHandLineY, Color.green, 0.5f);
                _rightHandGraph.SetTransparencyPoint(_rightHandPointY, Color.green, 1f);

                _rightHandGraph.SetTransparencyLine(_rightHandLineYCurrent, Color.black, 0.5f);
            }
            else
            {
                _rightHandGraph.SetTransparencyLine(_rightHandLineY, Color.green, 0f);
                _rightHandGraph.SetTransparencyPoint(_rightHandPointY, Color.green, 0f);

                _rightHandGraph.SetTransparencyLine(_rightHandLineYCurrent, Color.black, 0f);
            } 
        }
    }

    void RightHandToggleZValueChanged(Toggle toggle)
    {
        if (this.gameObject.CompareTag("RightHand"))
        {
            if (toggle.isOn)
            {
                _rightHandGraph.SetTransparencyLine(_rightHandLineZ, Color.blue, 0.5f);
                _rightHandGraph.SetTransparencyPoint(_rightHandPointZ, Color.blue, 1f);

                _rightHandGraph.SetTransparencyLine(_rightHandLineZCurrent, Color.black, 0.5f);
            }
            else
            {
                _rightHandGraph.SetTransparencyLine(_rightHandLineZ, Color.blue, 0f);
                _rightHandGraph.SetTransparencyPoint(_rightHandPointZ, Color.blue, 0f);

                _rightHandGraph.SetTransparencyLine(_rightHandLineZCurrent, Color.black, 0f);
            } 
        }
    }

    void RightForeArmToggleXValueChanged(Toggle toggle)
    {
        if (this.gameObject.CompareTag("RightForeArm"))
        {
            if (toggle.isOn)
            {
                _rightForeArmGraph.SetTransparencyLine(_rightForeArmLineX, Color.red, 0.5f);
                _rightForeArmGraph.SetTransparencyPoint(_rightForeArmPointX, Color.red, 1f);

                _rightForeArmGraph.SetTransparencyLine(_rightForeArmLineXCurrent, Color.black, 0.5f);
            }
            else
            {
                _rightForeArmGraph.SetTransparencyLine(_rightForeArmLineX, Color.red, 0f);
                _rightForeArmGraph.SetTransparencyPoint(_rightForeArmPointX, Color.red, 0f);

                _rightForeArmGraph.SetTransparencyLine(_rightForeArmLineXCurrent, Color.black, 0f);
            } 
        }
    }

    void RightForeArmToggleYValueChanged(Toggle toggle)
    {
        if (this.gameObject.CompareTag("RightForeArm"))
        {
            if (toggle.isOn)
            {
                _rightForeArmGraph.SetTransparencyLine(_rightForeArmLineY, Color.green, 0.5f);
                _rightForeArmGraph.SetTransparencyPoint(_rightForeArmPointY, Color.green, 1f);

                _rightForeArmGraph.SetTransparencyLine(_rightForeArmLineYCurrent, Color.black, 0.5f);
            }
            else
            {
                _rightForeArmGraph.SetTransparencyLine(_rightForeArmLineY, Color.green, 0f);
                _rightForeArmGraph.SetTransparencyPoint(_rightForeArmPointY, Color.green, 0f);

                _rightForeArmGraph.SetTransparencyLine(_rightForeArmLineYCurrent, Color.black, 0f);
            } 
        }
    }

    void RightForeArmToggleZValueChanged(Toggle toggle)
    {
        if (this.gameObject.CompareTag("RightForeArm"))
        {
            if (toggle.isOn)
            {
                _rightForeArmGraph.SetTransparencyLine(_rightForeArmLineZ, Color.blue, 0.5f);
                _rightForeArmGraph.SetTransparencyPoint(_rightForeArmPointZ, Color.blue, 1f);

                _rightForeArmGraph.SetTransparencyLine(_rightForeArmLineZCurrent, Color.black, 0.5f);
            }
            else
            {
                _rightForeArmGraph.SetTransparencyLine(_rightForeArmLineZ, Color.blue, 0f);
                _rightForeArmGraph.SetTransparencyPoint(_rightForeArmPointZ, Color.blue, 0f);

                _rightForeArmGraph.SetTransparencyLine(_rightForeArmLineZCurrent, Color.black, 0f);
            } 
        }
    }

    void RightArmToggleXValueChanged(Toggle toggle)
    {
        if (this.gameObject.CompareTag("RightArm"))
        {
            if (toggle.isOn)
            {
                _rightArmGraph.SetTransparencyLine(_rightArmLineX, Color.red, 0.5f);
                _rightArmGraph.SetTransparencyPoint(_rightArmPointX, Color.red, 1f);

                _rightArmGraph.SetTransparencyLine(_rightArmLineXCurrent, Color.black, 0.5f);
            }
            else
            {
                _rightArmGraph.SetTransparencyLine(_rightArmLineX, Color.red, 0f);
                _rightArmGraph.SetTransparencyPoint(_rightArmPointX, Color.red, 0f);

                _rightArmGraph.SetTransparencyLine(_rightArmLineXCurrent, Color.black, 0f);
            }
        }
    }

    void RightArmToggleYValueChanged(Toggle toggle)
    {
        if (this.gameObject.CompareTag("RightArm"))
        {
            if (toggle.isOn)
            {
                _rightArmGraph.SetTransparencyLine(_rightArmLineY, Color.green, 0.5f);
                _rightArmGraph.SetTransparencyPoint(_rightArmPointY, Color.green, 1f);

                _rightArmGraph.SetTransparencyLine(_rightArmLineYCurrent, Color.black, 0.5f);
            }
            else
            {
                _rightArmGraph.SetTransparencyLine(_rightArmLineY, Color.green, 0f);
                _rightArmGraph.SetTransparencyPoint(_rightArmPointY, Color.green, 0f);

                _rightArmGraph.SetTransparencyLine(_rightArmLineYCurrent, Color.black, 0f);
            }
        }
    }

    void RightArmToggleZValueChanged(Toggle toggle)
    {
        if (this.gameObject.CompareTag("RightArm"))
        {
            if (toggle.isOn)
            {
                _rightArmGraph.SetTransparencyLine(_rightArmLineZ, Color.blue, 0.5f);
                _rightArmGraph.SetTransparencyPoint(_rightArmPointZ, Color.blue, 1f);

                _rightArmGraph.SetTransparencyLine(_rightArmLineZCurrent, Color.black, 0.5f);
            }
            else
            {
                _rightArmGraph.SetTransparencyLine(_rightArmLineZ, Color.blue, 0f);
                _rightArmGraph.SetTransparencyPoint(_rightArmPointZ, Color.blue, 0f);

                _rightArmGraph.SetTransparencyLine(_rightArmLineZCurrent, Color.black, 0f);
            }
        }
    }

    #endregion
}
