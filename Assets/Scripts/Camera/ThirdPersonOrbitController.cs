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
        public float VerticalSensitivity = 1.0f;
        [Range(-80f, 0f)] public float MinPitch = -20f;
        [Range(0f,  80f)] public float MaxPitch =  60f;

        private StarterAssetsInputs _input;
        private PlayerInput _playerInput;
        private float _yaw;
        private float _pitch = 15f;

        private void Awake()
        {
            if (Player != null)
            {
                _input = Player.GetComponent<StarterAssetsInputs>();
                _playerInput = Player.GetComponent<PlayerInput>();
            }
        }

        private void OnEnable()
        {
            // Sync yaw with the player's current facing so the camera doesn't snap on switch.
            if (OrbitPivot != null && OrbitPivot.parent != null)
                _yaw = OrbitPivot.parent.eulerAngles.y;
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

                _yaw   += look.x * HorizontalSensitivity * multiplier;
                _pitch += look.y * VerticalSensitivity   * multiplier;
                _pitch  = Mathf.Clamp(_pitch, MinPitch, MaxPitch);
            }

            // Set world rotation — independent of player body facing direction.
            OrbitPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}
