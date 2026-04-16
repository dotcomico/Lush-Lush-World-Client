using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

namespace LushWorld.Camera
{
    /// <summary>
    /// Rotates a pivot Transform (child of the player) based on look input.
    /// The TP virtual camera's Follow target is set to this pivot so the camera
    /// orbits around the player instead of locking to the character's facing direction.
    /// Enabled/disabled by CameraViewController when entering/leaving TP mode.
    /// </summary>
    public class ThirdPersonOrbitController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Empty GO child of PlayerCapsule — TP camera orbits around this")]
        public Transform OrbitPivot;
        [Tooltip("The PlayerCapsule GameObject — must have StarterAssetsInputs and PlayerInput")]
        public GameObject Player;

        [Header("Orbit Settings")]
        public float HorizontalSensitivity = 1.0f;
        public float VerticalSensitivity   = 1.0f;
        [Range(-80f,  0f)] public float MinPitch = -20f;
        [Range(  0f, 80f)] public float MaxPitch =  60f;

        [Header("Smoothing")]
        [Tooltip("Lower = snappier, higher = smoother. 0.05–0.12 is a good range.")]
        [Range(0f, 0.3f)] public float RotationSmoothTime = 0.08f;

        private StarterAssetsInputs _input;
        private PlayerInput _playerInput;

        // Target angles (updated from raw input each frame)
        private float _targetYaw;
        private float _targetPitch = 15f;

        // Smoothed angles (fed to the pivot rotation)
        private float _smoothYaw;
        private float _smoothPitch;
        private float _yawVelocity;
        private float _pitchVelocity;

        private void Awake()
        {
            if (Player != null)
            {
                _input       = Player.GetComponent<StarterAssetsInputs>();
                _playerInput = Player.GetComponent<PlayerInput>();
            }
        }

        private void OnEnable()
        {
            // Sync yaw with the player's current facing so the camera doesn't snap on switch.
            if (OrbitPivot != null && OrbitPivot.parent != null)
            {
                _targetYaw = _smoothYaw = OrbitPivot.parent.eulerAngles.y;
                _targetPitch = _smoothPitch = 15f;
            }
        }

        private void LateUpdate()
        {
            if (OrbitPivot == null || _input == null) return;

            var look = _input.look;
            if (look.sqrMagnitude >= 0.01f)
            {
                bool isMouse = _playerInput != null &&
                               _playerInput.currentControlScheme == "KeyboardMouse";
                float multiplier = isMouse ? 1f : Time.deltaTime;

                _targetYaw   += look.x * HorizontalSensitivity * multiplier;
                _targetPitch += look.y * VerticalSensitivity   * multiplier;
                _targetPitch  = Mathf.Clamp(_targetPitch, MinPitch, MaxPitch);
            }

            // Smooth the angles so mouse micro-movements don't cause shake.
            _smoothYaw = Mathf.SmoothDampAngle(
                _smoothYaw, _targetYaw, ref _yawVelocity, RotationSmoothTime);
            _smoothPitch = Mathf.SmoothDampAngle(
                _smoothPitch, _targetPitch, ref _pitchVelocity, RotationSmoothTime);

            // Set world rotation — independent of player body facing direction.
            OrbitPivot.rotation = Quaternion.Euler(_smoothPitch, _smoothYaw, 0f);
        }
    }
}
