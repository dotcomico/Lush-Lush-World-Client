using UnityEngine;
using UCamera = UnityEngine.Camera;

namespace LushWorld.World
{
    [ExecuteAlways]
    public class NightSkyboxController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] DayNightCycle _cycle;
        [SerializeField] Material _daySkyboxMaterial;
        [SerializeField] Material _nightSkyboxMaterial;
        [SerializeField] ParticleSystem _stars;

        [Header("Blend Timing  (0 = midnight · 0.5 = noon)")]
        [Tooltip("timeOfDay where night sky starts fading in (just after sunset).")]
        [Range(0f, 1f)] [SerializeField] float _fadeInStart  = 0.68f;
        [Tooltip("timeOfDay where night sky is fully visible.")]
        [Range(0f, 1f)] [SerializeField] float _fadeInFull   = 0.98f;
        [Tooltip("timeOfDay (just past midnight) where night sky starts fading out.")]
        [Range(0f, 1f)] [SerializeField] float _fadeOutStart = 0.02f;
        [Tooltip("timeOfDay where day sky is fully restored.")]
        [Range(0f, 1f)] [SerializeField] float _fadeOutFull  = 0.32f;

        [Header("Stars")]
        [Range(0f, 1f)] [SerializeField] float _maxStarBrightness = 1f;

        Material _dayInstance;
        Material _nightInstance;
        MaterialPropertyBlock _starBlock;
        ParticleSystemRenderer _starsRenderer;
        bool _nightActive;

        // ── Lifecycle ─────────────────────────────────────────────

        void OnEnable()
        {
            if (_cycle == null)
                _cycle = GetComponent<DayNightCycle>();

            if (_daySkyboxMaterial != null)
                _dayInstance = new Material(_daySkyboxMaterial);
            if (_nightSkyboxMaterial != null)
                _nightInstance = new Material(_nightSkyboxMaterial);

            if (_stars != null)
            {
                _starsRenderer = _stars.GetComponent<ParticleSystemRenderer>();
                _starBlock = new MaterialPropertyBlock();
            }

            Refresh();
        }

        void OnDisable()
        {
            if (_daySkyboxMaterial != null)
                RenderSettings.skybox = _daySkyboxMaterial;
            CleanupInstances();
        }

        void OnDestroy() => CleanupInstances();

        void Update()
        {
            if (_cycle == null) return;
            // Stars are NOT repositioned here — they emit once in world space and stay fixed.
            Refresh();
        }

        // ── Core ──────────────────────────────────────────────────

        void Refresh()
        {
            float t     = _cycle != null ? _cycle.TimeOfDay : 0f;
            float blend = ComputeNightBlend(t);
            ApplySkybox(blend);
            ApplyStars(blend);
        }

        float ComputeNightBlend(float t)
        {
            if (t >= _fadeInStart)
                return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_fadeInStart, _fadeInFull, t));

            if (t <= _fadeOutStart)
                return 1f;

            if (t <= _fadeOutFull)
                return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_fadeOutFull, _fadeOutStart, t));

            return 0f;
        }

        void ApplySkybox(float blend)
        {
            const float kMinExp    = 0.02f;
            const float kDimStart  = 0.20f;
            const float kSwap      = 0.50f;
            const float kBrightEnd = 0.80f;

            if (blend <= kDimStart)
            {
                SwitchToDay();
                SetExposure(_dayInstance, 1f);
            }
            else if (blend < kSwap)
            {
                SwitchToDay();
                SetExposure(_dayInstance, Mathf.Lerp(1f, kMinExp,
                    Mathf.InverseLerp(kDimStart, kSwap, blend)));
            }
            else if (blend < kBrightEnd)
            {
                SwitchToNight();
                SetExposure(_nightInstance, Mathf.Lerp(kMinExp, 1f,
                    Mathf.InverseLerp(kSwap, kBrightEnd, blend)));
            }
            else
            {
                SwitchToNight();
                SetExposure(_nightInstance, 1f);
            }
        }

        void ApplyStars(float blend)
        {
            if (_stars == null || _starsRenderer == null || _starBlock == null) return;

            bool shouldShow = blend > 0.01f;

            if (_stars.gameObject.activeSelf != shouldShow)
            {
                if (shouldShow)
                {
                    // Snap the Stars object to camera position ONCE before the burst fires.
                    // After this, particles live in world space and never move again,
                    // so camera rotation and movement cannot affect them.
                    SnapStarsToCamera();
                    _stars.gameObject.SetActive(true);
                    _stars.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    _stars.Play();
                }
                else
                {
                    _stars.gameObject.SetActive(false);
                }
            }

            if (!shouldShow) return;

            var col = new Color(1f, 1f, 1f, blend * _maxStarBrightness);
            _starBlock.SetColor("_TintColor", col);
            _starBlock.SetColor("_Color",     col);
            _starsRenderer.SetPropertyBlock(_starBlock);
        }

        // Called once per night, just before the burst. Places the emission sphere
        // around the player's current position. World-space particles then never move.
        void SnapStarsToCamera()
        {
            UCamera cam = UCamera.main;
            if (cam == null || _stars == null) return;
            _stars.transform.position = cam.transform.position;
            _stars.transform.rotation = Quaternion.identity;
        }

        // ── Helpers ───────────────────────────────────────────────

        void SwitchToDay()
        {
            if (_nightActive && _dayInstance != null)
            {
                RenderSettings.skybox = _dayInstance;
                _nightActive = false;
            }
        }

        void SwitchToNight()
        {
            if (!_nightActive && _nightInstance != null)
            {
                RenderSettings.skybox = _nightInstance;
                _nightActive = true;
            }
        }

        static void SetExposure(Material mat, float exposure)
        {
            if (mat != null && mat.HasProperty("_Exposure"))
                mat.SetFloat("_Exposure", Mathf.Clamp01(exposure));
        }

        void CleanupInstances()
        {
            if (_dayInstance   != null) { DestroyImmediate(_dayInstance);   _dayInstance   = null; }
            if (_nightInstance != null) { DestroyImmediate(_nightInstance); _nightInstance = null; }
        }
    }
}
