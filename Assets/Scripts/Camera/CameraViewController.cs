using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
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

        // ── Third Person Orbit ────────────────────────────────────────────────
        [Header("Third Person Orbit")]
        [Tooltip("Controller that orbits the TP camera around the player via look input")]
        public ThirdPersonOrbitController OrbitController;

        // ── UI ────────────────────────────────────────────────────────────────
        [Header("UI")]
        [Tooltip("Button that cycles through camera modes (mobile & PC)")]
        public Button CycleCameraButton;

        // ─────────────────────────────────────────────────────────────────────
        private CameraMode _currentMode = CameraMode.FirstPerson;
        private static readonly string[] _modeLabels = { "FP", "3P", "ISO" };
        private Coroutine _hideMeshCoroutine;

        // ─────────────────────────────────────────────────────────────────────
        private void Start()
        {
            if (CycleCameraButton != null)
                CycleCameraButton.onClick.AddListener(CycleCamera);

            // Rewire TP VCam Follow to the orbit pivot so the camera orbits
            // around the player instead of locking to the character's facing direction.
            if (ThirdPersonCameraGO != null && OrbitController != null && OrbitController.OrbitPivot != null)
            {
                var vcam = ThirdPersonCameraGO.GetComponent<CinemachineVirtualCamera>();
                if (vcam != null)
                    vcam.Follow = OrbitController.OrbitPivot;
            }

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

            // Orbit controller only runs in TP mode.
            if (OrbitController != null)
                OrbitController.enabled = (mode == CameraMode.ThirdPerson);

            bool isFP = mode == CameraMode.FirstPerson;

            // Tell the player controller how to behave in this mode.
            if (PlayerController != null)
            {
                // In FP: the look joystick rotates the camera target (existing behaviour).
                // In TP/ISO: disable that so the character faces movement direction instead.
                PlayerController.EnableCameraRotation = isFP;

                // In TP/ISO: reset FP camera target pitch to zero so the CinemachineComposer
                // isn't chasing a tilted look-at point inherited from FP mode.
                if (!isFP && PlayerController.CinemachineCameraTarget != null)
                    PlayerController.CinemachineCameraTarget.transform.localRotation = Quaternion.identity;

                // In TP/ISO: pass the main camera so movement is camera-relative.
                // In FP: null means movement is player-relative (existing behaviour).
                PlayerController.MovementCameraTransform = isFP ? null : UnityEngine.Camera.main.transform;
            }

            // Show player mesh in TP/ISO immediately so it's visible during the blend.
            // When entering FP, wait for the Cinemachine blend to finish before hiding —
            // otherwise the snail vanishes mid-transition.
            if (isFP)
            {
                // Cancel any in-flight hide (e.g. double-tap) before starting a new one.
                if (_hideMeshCoroutine != null) StopCoroutine(_hideMeshCoroutine);
                _hideMeshCoroutine = StartCoroutine(HideMeshAfterBlend());
            }
            else
            {
                // Leaving FP: cancel any pending hide and show the mesh immediately.
                if (_hideMeshCoroutine != null) { StopCoroutine(_hideMeshCoroutine); _hideMeshCoroutine = null; }
                SetMeshVisibility(true);
            }

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

        private void SetMeshVisibility(bool visible)
        {
            foreach (var r in PlayerMeshRenderers)
                if (r != null)
                    r.enabled = visible;
        }

        /// <summary>
        /// Waits until the Cinemachine Brain is done blending, then hides the player mesh.
        /// Prevents the snail from disappearing mid-transition when entering FP mode.
        /// </summary>
        private IEnumerator HideMeshAfterBlend()
        {
            // Yield one frame FIRST. Cinemachine activates new vcams and starts blending
            // in LateUpdate — if we check IsBlending on the same frame SetActive was called,
            // the blend hasn't registered yet and IsBlending is still false, so the
            // WaitUntil fires immediately and the mesh is hidden too early.
            yield return null;

            var brain = UnityEngine.Camera.main?.GetComponent<CinemachineBrain>();
            if (brain != null)
                yield return new WaitUntil(() => !brain.IsBlending);

            // Only hide if we're still in FP mode — user may have switched away
            // before the blend finished.
            if (_currentMode == CameraMode.FirstPerson)
            {
                SetMeshVisibility(false);
                _hideMeshCoroutine = null;
            }
        }
    }

    /// <summary>Available camera perspective modes.</summary>
    public enum CameraMode { FirstPerson, ThirdPerson, Isometric }
}
