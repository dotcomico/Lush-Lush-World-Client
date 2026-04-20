using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

namespace LushWorld.Player
{
    public enum SlideCameraMode { None, Medium, Cinematic }

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(FirstPersonController))]
    public class SlideController : MonoBehaviour
    {
        [Header("Slide Physics")]
        [SerializeField] private float _minSlideAngle = 5f;
        [SerializeField] private float _maxSlideSpeed = 15f;
        [SerializeField] private float _flatFriction = 8f;
        [SerializeField] private float _stopSpeedThreshold = 0.5f;
        [SerializeField] private float _slideGravity = -15f;

        [Header("Visual Flip")]
        [SerializeField] private Transform _visualModel;
        [SerializeField] private float _flipDuration = 0.25f;
        [SerializeField] private Vector3 _flipEuler = new Vector3(0f, 0f, 180f);
        [SerializeField] private float _spinSpeedMultiplier = 180f;

        [Header("Ground Detection")]
        [SerializeField] private float _groundRayDistance = 2f;
        [SerializeField] private LayerMask _groundLayers;

        [Header("Timing")]
        [SerializeField] private float _minSlideDuration = 0.5f;

        [Header("Camera Effect")]
        [SerializeField] private SlideCameraMode _cameraMode = SlideCameraMode.Medium;
        [SerializeField] private float _mediumRollAngle = 15f;
        [SerializeField] private float _cameraRollSpeed = 5f;

        public bool IsSliding { get; private set; }

        private CharacterController _controller;
        private FirstPersonController _fpc;
        private InputAction _slideAction;
        private Vector3 _slideVelocity;
        private float _verticalVelocity;
        private float _slideEntryTime;
        private float _targetCameraRoll;
        private Coroutine _flipCoroutine;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _fpc = GetComponent<FirstPersonController>();
            _slideAction = GetComponent<PlayerInput>().actions["Player/Crouch"];
        }

        private void OnEnable()
        {
            _slideAction.performed += OnSlideInputPerformed;
        }

        private void OnDisable()
        {
            _slideAction.performed -= OnSlideInputPerformed;
        }

        private void OnSlideInputPerformed(InputAction.CallbackContext _)
        {
            if (IsSliding)
                ExitSlide();
            else
                EnterSlide();
        }

        private void Update()
        {
            UpdateCameraRoll();
            if (!IsSliding) return;
            ApplySlidePhysics();
        }

        private void UpdateCameraRoll()
        {
            if (_cameraMode == SlideCameraMode.None) return;
            _fpc.CameraRollOffset = Mathf.LerpAngle(_fpc.CameraRollOffset, _targetCameraRoll, Time.deltaTime * _cameraRollSpeed);
        }

        private void EnterSlide()
        {
            IsSliding = true;
            _slideEntryTime = Time.time;
            _fpc.DisableHorizontalMovement = true;
            _targetCameraRoll = _cameraMode switch
            {
                SlideCameraMode.Medium   => _mediumRollAngle,
                SlideCameraMode.Cinematic => 180f,
                _                        => 0f
            };

            // Inherit current horizontal momentum so the slide feels continuous
            Vector3 currentVel = _controller.velocity;
            _slideVelocity = new Vector3(currentVel.x, 0f, currentVel.z);
            _verticalVelocity = currentVel.y;

            StartFlip(true);
        }

        private void ExitSlide()
        {
            IsSliding = false;
            _fpc.DisableHorizontalMovement = false;
            _slideVelocity = Vector3.zero;
            _targetCameraRoll = 0f;
            StartFlip(false);
        }

        private void ApplySlidePhysics()
        {
            // Vertical — same gravity model as FirstPersonController
            if (_fpc.Grounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += _slideGravity * Time.deltaTime;
            _verticalVelocity = Mathf.Max(_verticalVelocity, -53f);

            // Horizontal — slope acceleration or flat deceleration
            if (TryGetSlopeInfo(out float angle, out Vector3 downhill) && angle > _minSlideAngle)
            {
                float slopeAcceleration = Mathf.Abs(_slideGravity) * Mathf.Sin(angle * Mathf.Deg2Rad);
                _slideVelocity += downhill * slopeAcceleration * Time.deltaTime;
                _slideVelocity = Vector3.ClampMagnitude(_slideVelocity, _maxSlideSpeed);
            }
            else
            {
                _slideVelocity = Vector3.MoveTowards(_slideVelocity, Vector3.zero, _flatFriction * Time.deltaTime);

                // Auto-exit when nearly stopped on flat ground — but never before minSlideDuration
                bool pastMinDuration = Time.time - _slideEntryTime > _minSlideDuration;
                if (_fpc.Grounded && pastMinDuration && _slideVelocity.sqrMagnitude < _stopSpeedThreshold * _stopSpeedThreshold)
                {
                    ExitSlide();
                    return;
                }
            }

            _controller.Move((_slideVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime);

            // Spin the snail on its shell based on current slide speed
            if (_visualModel != null)
            {
                float spinSpeed = _slideVelocity.magnitude * _spinSpeedMultiplier;
                _visualModel.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
            }

            // Exit slide if player leaves ground (e.g. slid off a ledge)
            if (!_fpc.Grounded && _verticalVelocity < -5f)
                ExitSlide();
        }

        private bool TryGetSlopeInfo(out float angle, out Vector3 slideDirection)
        {
            angle = 0f;
            slideDirection = Vector3.zero;

            // Fall back to default layers if nothing is assigned in Inspector
            LayerMask mask = _groundLayers.value != 0 ? _groundLayers : Physics.DefaultRaycastLayers;
            if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, _groundRayDistance, mask))
                return false;

            angle = Vector3.Angle(hit.normal, Vector3.up);
            // Downhill direction: project gravity onto the slope plane
            Vector3 slopeRight = Vector3.Cross(hit.normal, Vector3.down);
            slideDirection = Vector3.Cross(slopeRight, hit.normal).normalized;
            return true;
        }

        private void StartFlip(bool toSlide)
        {
            if (_flipCoroutine != null) StopCoroutine(_flipCoroutine);
            _flipCoroutine = StartCoroutine(AnimateFlip(toSlide));
        }

        private IEnumerator AnimateFlip(bool toSlide)
        {
            if (_visualModel == null) yield break;

            Quaternion from = _visualModel.localRotation;
            Quaternion to = toSlide ? Quaternion.Euler(_flipEuler) : Quaternion.identity;

            for (float t = 0f; t < _flipDuration; t += Time.deltaTime)
            {
                _visualModel.localRotation = Quaternion.Slerp(from, to, t / _flipDuration);
                yield return null;
            }

            _visualModel.localRotation = to;
            _flipCoroutine = null;
        }
    }
}
