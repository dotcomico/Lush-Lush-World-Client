using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace LushWorld.World
{
    [ExecuteAlways]
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Time")]
        [Tooltip("0 = midnight  0.25 = sunrise  0.5 = noon  0.75 = sunset")]
        [Range(0f, 1f)] public float timeOfDay = 0.25f;
        [Min(0.1f)] public float dayDurationMinutes = 10f;
        public bool running = true;

        [Header("Sun")]
        public Light sun;
        [SerializeField] Gradient _sunColor;
        [SerializeField] AnimationCurve _sunIntensity;
        [Tooltip("Y-axis tilt of the sun's arc — 170 gives a natural Northern-Hemisphere path")]
        [Range(0f, 360f)] [SerializeField] float _sunOrbitYaw = 170f;

        [Header("Ambient")]
        [SerializeField] Gradient _ambientColor;

        [Header("Events")]
        public UnityEvent onDayStart;
        public UnityEvent onNightStart;

        public static event System.Action OnDayStarted;
        public static event System.Action OnNightStarted;

        public float TimeOfDay => timeOfDay;
        public bool IsNight    => timeOfDay < _sunriseThreshold || timeOfDay >= _nightThreshold;
        public bool IsDay      => !IsNight;

        [Header("Thresholds")]
        [Tooltip("timeOfDay value where sunrise color begins (default 0.14)")]
        [Range(0f, 0.5f)] [SerializeField] float _sunriseThreshold = 0.14f;
        [Tooltip("timeOfDay value where IsDay becomes true and OnDayStarted fires (default 0.19)")]
        [Range(0f, 0.5f)] [SerializeField] float _dayThreshold     = 0.19f;
        [Tooltip("timeOfDay value where IsNight becomes true and OnNightStarted fires (default 0.86)")]
        [Range(0.5f, 1f)] [SerializeField] float _nightThreshold   = 0.86f;

        bool _wasDay;
        bool _transitionInitialized;

        // Version-gated defaults: bump this whenever Build*Curve / Build*Gradient values change
        // so the existing serialized component picks up the new values on next Awake.
        const int DefaultsVersion = 3;
        [SerializeField, HideInInspector] int _defaultsVersion;

        void Awake()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            if (_defaultsVersion < DefaultsVersion)
                ApplyDefaults();
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

        void UpdateSun()
        {
            if (sun == null) return;
            sun.transform.rotation = Quaternion.Euler(timeOfDay * 360f - 90f, _sunOrbitYaw, 0f);
            sun.color              = _sunColor.Evaluate(timeOfDay);
            sun.intensity          = _sunIntensity.Evaluate(timeOfDay);
        }

        void UpdateAmbient()
        {
            RenderSettings.ambientLight = _ambientColor.Evaluate(timeOfDay);

            // Skybox reflections keep surfaces bright even when ambientLight is near-black.
            // Scale reflection intensity down with the sun so night actually feels dark.
            // A floor of 0.05 prevents perfectly-flat reflections on shiny surfaces.
            float sunVal = _sunIntensity.length > 0 ? _sunIntensity.Evaluate(timeOfDay) : 1f;
            RenderSettings.reflectionIntensity = Mathf.Lerp(0.05f, 1f, Mathf.Clamp01(sunVal));
        }

        void CheckTransition()
        {
            bool nowDay = timeOfDay >= _dayThreshold && timeOfDay < _nightThreshold;
            if (!_transitionInitialized) { _wasDay = nowDay; _transitionInitialized = true; return; }
            if (nowDay == _wasDay) return;
            _wasDay = nowDay;
            if (nowDay) { onDayStart?.Invoke(); OnDayStarted?.Invoke(); }
            else        { onNightStart?.Invoke(); OnNightStarted?.Invoke(); }
        }

        // Jumps time to a saved value and immediately updates lighting.
        public void LoadTimeOfDay(float time)
        {
            timeOfDay = Mathf.Clamp01(time);
            _transitionInitialized = false; // re-init _wasDay from loaded time; prevents spurious day/night event on first post-load Update
            UpdateSun();
            UpdateAmbient();
        }

        void Reset()
        {
            ApplyDefaults();
        }

        void ApplyDefaults()
        {
            _defaultsVersion = DefaultsVersion;
            _sunColor     = BuildSunColorGradient();
            _sunIntensity = BuildSunIntensityCurve();
            _ambientColor = BuildAmbientColorGradient();
        }

        static Gradient BuildSunColorGradient()
        {
            var g = new Gradient();
            g.SetKeys(new GradientColorKey[]
            {
                new(new Color(0.05f, 0.05f, 0.18f), 0.00f),
                new(new Color(1.00f, 0.30f, 0.03f), 0.14f),
                new(new Color(1.00f, 0.65f, 0.25f), 0.18f),
                new(new Color(1.00f, 0.95f, 0.85f), 0.45f),
                new(new Color(1.00f, 0.95f, 0.85f), 0.55f),
                new(new Color(1.00f, 0.65f, 0.25f), 0.82f),
                new(new Color(1.00f, 0.30f, 0.03f), 0.86f),
                new(new Color(0.05f, 0.05f, 0.18f), 1.00f),
            }, new GradientAlphaKey[] { new(1f, 0f), new(1f, 1f) });
            return g;
        }

        static AnimationCurve BuildSunIntensityCurve()
        {
            var c = new AnimationCurve();
            c.AddKey(new Keyframe(0.00f, 0.00f));
            c.AddKey(new Keyframe(0.14f, 0.00f));
            c.AddKey(new Keyframe(0.19f, 0.50f));
            c.AddKey(new Keyframe(0.50f, 1.20f));
            c.AddKey(new Keyframe(0.81f, 0.50f));
            c.AddKey(new Keyframe(0.86f, 0.00f));
            c.AddKey(new Keyframe(1.00f, 0.00f));
            for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
            return c;
        }

        static Gradient BuildAmbientColorGradient()
        {
            var g = new Gradient();
            g.SetKeys(new GradientColorKey[]
            {
                new(new Color(0.04f,  0.04f,  0.07f),  0.00f),  // deep night  — dark cool blue (visible)
                new(new Color(0.08f,  0.04f,  0.01f),  0.15f),  // pre-sunrise — dim ember
                new(new Color(0.38f,  0.26f,  0.10f),  0.19f),  // sunrise     — warm orange
                new(new Color(0.48f,  0.52f,  0.62f),  0.50f),  // noon        — cool sky blue
                new(new Color(0.38f,  0.26f,  0.10f),  0.81f),  // sunset      — warm orange
                new(new Color(0.08f,  0.04f,  0.01f),  0.85f),  // post-sunset — dim
                new(new Color(0.04f,  0.04f,  0.07f),  1.00f),  // deep night
            }, new GradientAlphaKey[] { new(1f, 0f), new(1f, 1f) });
            return g;
        }

    }
}
