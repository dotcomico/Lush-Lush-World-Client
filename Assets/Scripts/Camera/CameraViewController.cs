using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using StarterAssets;

namespace LushWorld.Camera
{
    /// <summary>
    /// Manages switching between First-Person, Third-Person, and Isometric camera modes.
    /// Attach this to a CameraViewManager GameObject in the scene.
    /// Wire up the three virtual camera GameObjects, the player controller, mesh renderers,
    /// and the mobile UI button in the Inspector.
    /// </summary>
    public class CameraViewController : MonoBehaviour
    {
        // ── Camera GameObjects ────────────────────────────────────────────────
        [Header("Virtual Camera GameObjects")]
        [Tooltip("The existing first-person CinemachineVirtualCamera GameObject")]
        public GameObject FirstPersonCameraGO;
        [Tooltip("Third-person CinemachineVirtualCamera GameObject")]
        public GameObject ThirdPersonCameraGO;
        [Tooltip("Isometric CinemachineVirtualCamera GameObject")]
        public GameObject IsometricCameraGO;

        // ── Player references ─────────────────────────────────────────────────
        [Header("Player")]
        [Tooltip("The FirstPersonController on the PlayerCapsule")]
        public FirstPersonController PlayerController;
        [Tooltip("All mesh renderers that should be hidden in first-person view")]
        public Renderer[] PlayerMeshRenderers;

        // ── UI ────────────────────────────────────────────────────────────────
        [Header("UI")]
        [Tooltip("Button that cycles through camera modes (mobile & PC)")]
        public Button CycleCameraButton;

        // ─────────────────────────────────────────────────────────────────────
        private CameraMode _currentMode = CameraMode.FirstPerson;
        private static readonly string[] _modeLabels = { "FP", "3P", "ISO" };

        // ─────────────────────────────────────────────────────────────────────
        private void Start()
        {
            if (CycleCameraButton != null)
                CycleCameraButton.onClick.AddListener(CycleCamera);

            Debug.Log($"[CameraView] Keyboard.current = {Keyboard.current}");
            ApplyMode(_currentMode);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[Key.V].wasPressedThisFrame)
                CycleCamera();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Advances to the next camera mode in the cycle FP → TP → ISO → FP.</summary>
        public void CycleCamera() =>
            SetMode((CameraMode)(((int)_currentMode + 1) % 3));

        /// <summary>Switches directly to a specific camera mode.</summary>
        public void SetMode(CameraMode mode)
        {
            _currentMode = mode;
            ApplyMode(mode);
        }

        public CameraMode CurrentMode => _currentMode;

        // ── Private ───────────────────────────────────────────────────────────

        private void ApplyMode(CameraMode mode)
        {
            // Activate only the relevant virtual camera; Brain picks it up automatically.
            SetActive(FirstPersonCameraGO, mode == CameraMode.FirstPerson);
            SetActive(ThirdPersonCameraGO, mode == CameraMode.ThirdPerson);
            SetActive(IsometricCameraGO, mode == CameraMode.Isometric);

            bool isFP = mode == CameraMode.FirstPerson;

            // Tell the player controller how to behave in this mode.
            if (PlayerController != null)
            {
                // In FP: the look joystick rotates the camera target (existing behaviour).
                // In TP/ISO: disable that so the character faces movement direction instead.
                PlayerController.EnableCameraRotation = isFP;

                // In TP/ISO: pass the main camera so movement is camera-relative.
                // In FP: null means movement is player-relative (existing behaviour).
                PlayerController.MovementCameraTransform = isFP ? null : UnityEngine.Camera.main.transform;
            }

            // Show player mesh in TP/ISO, hide it in FP (you're inside the character).
            foreach (var r in PlayerMeshRenderers)
                if (r != null)
                    r.enabled = !isFP;

            // Update button label so the player knows the current mode.
            if (CycleCameraButton != null)
            {
                var label = CycleCameraButton.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = _modeLabels[(int)mode];
            }

            Debug.Log($"[CameraView] Switched to {mode}");
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }
    }

    /// <summary>Available camera perspective modes.</summary>
    public enum CameraMode { FirstPerson, ThirdPerson, Isometric }
}
