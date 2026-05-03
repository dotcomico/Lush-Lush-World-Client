using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LushWorld.Player
{
    // Drives a URP post-processing Volume to produce a candy-world effect during Gummy Rush.
    // Attach to any persistent scene GO. Assign _buffVolume in Inspector.
    public class GummyCandyEffect : MonoBehaviour
    {
        private const float FadeInDuration = 0.5f;
        private const float FadeOutDuration = 0.8f;
        private const float HueOscillateSpeed = 1.2f;
        private const float MaxHueShift = 25f;

        [SerializeField] private Volume _buffVolume;

        private ColorAdjustments _colorAdj;
        private bool _buffActive;
        private Coroutine _fadeCoroutine;

        private void Start()
        {
            if (_buffVolume == null) return;

            // .profile creates a runtime instance — does NOT modify the asset on disk
            var profile = _buffVolume.profile;
            profile.TryGet(out _colorAdj);
            _buffVolume.weight = 0f;
        }

        private void OnEnable()
        {
            PlayerBuffSystem.OnBuffStarted += HandleBuffStarted;
            PlayerBuffSystem.OnBuffEnded += HandleBuffEnded;
        }

        private void OnDisable()
        {
            PlayerBuffSystem.OnBuffStarted -= HandleBuffStarted;
            PlayerBuffSystem.OnBuffEnded -= HandleBuffEnded;
        }

        private void Update()
        {
            if (!_buffActive || _colorAdj == null) return;
            _colorAdj.hueShift.Override(Mathf.Sin(Time.time * HueOscillateSpeed) * MaxHueShift);
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

            if (_buffVolume == null) return;
            if (_colorAdj != null) _colorAdj.hueShift.Override(0f);

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeVolume(_buffVolume.weight, 0f, FadeOutDuration));
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
