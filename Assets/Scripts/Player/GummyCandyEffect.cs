using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LushWorld.Player
{
    // Drives a URP post-processing Volume + camera sway to produce a drugged/candy-rush
    // effect during Gummy Rush. Attach to any persistent scene GO.
    // Inspector: assign _buffVolume and _cameraRoot (PlayerCameraRoot).
    public class GummyCandyEffect : MonoBehaviour
    {
        private const float FadeInDuration  = 0.5f;
        private const float FadeOutDuration = 0.8f;

        // Color hue cycling
        private const float HueOscillateSpeed = 1.2f;
        private const float MaxHueShift       = 50f;

        // Vignette — slow tunnel-vision breathing
        private const float VignetteSpeed    = 0.6f;
        private const float VignetteBase     = 0.25f;
        private const float VignettePulse    = 0.18f;

        // Lens distortion — drunk barrel warp
        private const float DistortionSpeed  = 1.3f;
        private const float MaxDistortion    = 0.35f;

        // Camera roll sway — head-tilt "dazed" feeling
        private const float SwaySpeed        = 0.7f;
        private const float MaxSwayAngle     = 4f;

        [SerializeField] private Volume    _buffVolume;
        [SerializeField] private Transform _cameraRoot; // PlayerCameraRoot

        private ColorAdjustments _colorAdj;
        private Vignette         _vignette;
        private LensDistortion   _lensDistortion;

        private bool      _buffActive;
        private Coroutine _fadeCoroutine;

        private void Start()
        {
            if (_buffVolume == null) return;

            // .profile creates a runtime instance — does NOT modify the asset on disk
            var profile = _buffVolume.profile;
            profile.TryGet(out _colorAdj);
            profile.TryGet(out _vignette);
            profile.TryGet(out _lensDistortion);
            _buffVolume.weight = 0f;
        }

        private void OnEnable()
        {
            PlayerBuffSystem.OnBuffStarted += HandleBuffStarted;
            PlayerBuffSystem.OnBuffEnded   += HandleBuffEnded;
        }

        private void OnDisable()
        {
            PlayerBuffSystem.OnBuffStarted -= HandleBuffStarted;
            PlayerBuffSystem.OnBuffEnded   -= HandleBuffEnded;
        }

        private void Update()
        {
            if (!_buffActive) return;

            float t = Time.time;

            if (_colorAdj != null)
                _colorAdj.hueShift.Override(Mathf.Sin(t * HueOscillateSpeed) * MaxHueShift);

            if (_vignette != null)
                _vignette.intensity.Override(VignetteBase + VignettePulse * Mathf.Sin(t * VignetteSpeed));

            if (_lensDistortion != null)
                _lensDistortion.intensity.Override(Mathf.Sin(t * DistortionSpeed) * MaxDistortion);
        }

        // Runs in LateUpdate so the sway is applied after FirstPersonController sets pitch.
        private void LateUpdate()
        {
            if (_cameraRoot == null || !_buffActive) return;

            float sway = Mathf.Sin(Time.time * SwaySpeed) * MaxSwayAngle;
            Vector3 euler = _cameraRoot.localEulerAngles;
            euler.z = sway;
            _cameraRoot.localEulerAngles = euler;
        }

        private void HandleBuffStarted(string buffId, float duration)
        {
            if (_buffVolume == null) return;
            _buffActive = true;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeVolume(_buffVolume.weight, 1f, FadeInDuration));
        }

        private void HandleBuffEnded(string buffId)
        {
            _buffActive = false;
            ResetEffects();

            if (_buffVolume == null) return;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeVolume(_buffVolume.weight, 0f, FadeOutDuration));
        }

        private void ResetEffects()
        {
            if (_colorAdj != null)     _colorAdj.hueShift.Override(0f);
            if (_vignette != null)     _vignette.intensity.Override(0f);
            if (_lensDistortion != null) _lensDistortion.intensity.Override(0f);

            if (_cameraRoot != null)
            {
                Vector3 euler = _cameraRoot.localEulerAngles;
                euler.z = 0f;
                _cameraRoot.localEulerAngles = euler;
            }
        }

        private IEnumerator FadeVolume(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _buffVolume.weight = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _buffVolume.weight = to;
            _fadeCoroutine = null;
        }
    }
}
