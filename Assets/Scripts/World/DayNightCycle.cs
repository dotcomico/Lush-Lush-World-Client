using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace LushWorld.World
{
    /// Rotates the sun (Directional Light) through a full day and fires
    /// OnDayStarted / OnNightStarted events for other systems (e.g. fireflies) to subscribe.
    [ExecuteAlways]
    public class DayNightCycle : MonoBehaviour
    {
        // ── Time ─────────────────────────────────────────────────────────────
        [Header("Time")]
        [Tooltip("0 = midnight  0.25 = sunrise  0.5 = noon  0.75 = sunset")]
        [Range(0f, 1f)] public float timeOfDay = 0.25f;
        [Min(0.1f)] public float dayDurationMinutes = 10f;
        public bool running = true;

        // ── Sun ──────────────────────────────────────────────────────────────
        [Header("Sun")]
        public Light sun;
        [SerializeField] Gradient _sunColor;
        [SerializeField] AnimationCurve _sunIntensity;
        [Tooltip("Y-axis tilt of the sun's arc — 170 gives a natural Northern-Hemisphere path")]
        [Range(0f, 360f)] [SerializeField] float _sunOrbitYaw = 170f;

        // ── Ambient ──────────────────────────────────────────────────────────
        [Header("Ambient")]
        [SerializeField] Gradient _ambientColor;

        // ── Events ───────────────────────────────────────────────────────────
        [Header("Events")]
        public UnityEvent onDayStart;
        public UnityEvent onNightStart;

        // Static events so other systems (fireflies, audio) subscribe without a scene reference
        public static event System.Action OnDayStarted;
        public static event System.Action OnNightStarted;

        // ── Public API ───────────────────────────────────────────────────────
        public float TimeOfDay => timeOfDay;
        public bool IsNight  => timeOfDay < SunriseThreshold || timeOfDay >= NightThreshold;
        public bool IsDay    => !IsNight;

        // ── Thresholds ───────────────────────────────────────────────────────
        const float SunriseThreshold = 0.22f;  // sun starts appearing
        const float DayThreshold     = 0.28f;  // full day
        const float SunsetThreshold  = 0.72f;  // sun starts setting
        const float NightThreshold   = 0.78f;  // full night

        bool _wasDay;
        bool _transitionInitialized;

        // ── Lifecycle ────────────────────────────────────────────────────────

        void Awake()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
        }

        void Update()
        {
            if (running && Application.isPlaying)
                timeOfDay = (timeOfDay + Time.deltaTime / (dayDurationMinutes * 60f)) % 1f;

            UpdateSun();
            UpdateAmbient();

            if (Application.isPlaying)
                CheckTransition();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        void UpdateSun()
        {
            if (sun == null) return;

            // X = -90 at midnight → 0 at sunrise → 90 at noon → 180 at sunset
            sun.transform.rotation = Quaternion.Euler(timeOfDay * 360f - 90f, _sunOrbitYaw, 0f);
            sun.color              = _sunColor.Evaluate(timeOfDay);
            sun.intensity          = _sunIntensity.Evaluate(timeOfDay);
        }

        void UpdateAmbient()
        {
            RenderSettings.ambientLight = _ambientColor.Evaluate(timeOfDay);
        }

        void CheckTransition()
        {
            bool nowDay = timeOfDay >= DayThreshold && timeOfDay < NightThreshold;

            if (!_transitionInitialized)
            {
                _wasDay = nowDay;
                _transitionInitialized = true;
                return;
            }

            if (nowDay == _wasDay) return;

            _wasDay = nowDay;

            if (nowDay)
            {
                onDayStart?.Invoke();
                OnDayStarted?.Invoke();
            }
            else
            {
                onNightStart?.Invoke();
                OnNightStarted?.Invoke();
            }
        }

        // ── Editor defaults (called once when component is added) ────────────

        void Reset()
        {
            _sunColor     = BuildSunColorGradient();
            _sunIntensity = BuildSunIntensityCurve();
            _ambientColor = BuildAmbientColorGradient();
        }

        static Gradient BuildSunColorGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new(new Color(0.05f, 0.05f, 0.18f), 0.00f),  // midnight – deep blue
                    new(new Color(1.00f, 0.30f, 0.03f), 0.22f),  // pre-sunrise – deep orange
                    new(new Color(1.00f, 0.65f, 0.25f), 0.27f),  // sunrise – golden orange
                    new(new Color(1.00f, 0.95f, 0.85f), 0.45f),  // late morning – warm white
                    new(new Color(1.00f, 0.95f, 0.85f), 0.55f),  // afternoon  – warm white
                    new(new Color(1.00f, 0.65f, 0.25f), 0.73f),  // sunset – golden orange
                    new(new Color(1.00f, 0.30f, 0.03f), 0.78f),  // post-sunset – deep orange
                    new(new Color(0.05f, 0.05f, 0.18f), 1.00f),  // midnight – deep blue
                },
                new GradientAlphaKey[] { new(1f, 0f), new(1f, 1f) }
            );
            return g;
        }

        static AnimationCurve BuildSunIntensityCurve()
        {
            var c = new AnimationCurve();
            c.AddKey(new Keyframe(0.00f, 0.00f));  // midnight
            c.AddKey(new Keyframe(0.22f, 0.00f));  // pre-sunrise
            c.AddKey(new Keyframe(0.28f, 0.50f));  // just-risen
            c.AddKey(new Keyframe(0.50f, 1.20f));  // noon – slight HDR boost for bloom
            c.AddKey(new Keyframe(0.72f, 0.50f));  // about to set
            c.AddKey(new Keyframe(0.78f, 0.00f));  // just-set
            c.AddKey(new Keyframe(1.00f, 0.00f));  // midnight
            // Smooth all tangents
            for (int i = 0; i < c.length; i++)
                c.SmoothTangents(i, 0f);
            return c;
        }

        static Gradient BuildAmbientColorGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new(new Color(0.02f, 0.02f, 0.08f), 0.00f),  // midnight – near black blue
                    new(new Color(0.12f, 0.06f, 0.02f), 0.23f),  // pre-sunrise – warm dark
                    new(new Color(0.45f, 0.30f, 0.15f), 0.28f),  // sunrise – muted warm
                    new(new Color(0.55f, 0.60f, 0.70f), 0.50f),  // noon – cool sky-tinted
                    new(new Color(0.45f, 0.30f, 0.15f), 0.72f),  // sunset – muted warm
                    new(new Color(0.12f, 0.06f, 0.02f), 0.77f),  // post-sunset
                    new(new Color(0.02f, 0.02f, 0.08f), 1.00f),  // midnight
                },
                new GradientAlphaKey[] { new(1f, 0f), new(1f, 1f) }
            );
            return g;
        }
    }
}
