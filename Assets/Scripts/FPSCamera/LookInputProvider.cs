using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static Cinemachine.AxisState;
using static Cinemachine.CinemachineCore;
 
namespace Runtime
{
    public class LookInputProvider : CinemachineExtension, IInputAxisProvider
    {
        [SerializeField] private Transform Player;
        [SerializeField] private InputActionReference XYAxis;
 
        private Vector2 _rotation;
 
        void Start()
        {
            XYAxis.action.Enable();
            transform.localEulerAngles = Player.localEulerAngles;
            _rotation = transform.localEulerAngles;
        }
 
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == Stage.Aim)
            {
                float y = Player.transform.localEulerAngles.y;
                Vector3 euler = state.RawOrientation.eulerAngles;
                state.RawOrientation = Quaternion.Euler(euler.x, y, euler.z);
            }
        }
        public float GetAxisValue(int axis)
        {
            if (axis == 1)
            {
                return XYAxis.action.ReadValue<Vector2>().y;
            }
 
            return 0;
        }
    }
}