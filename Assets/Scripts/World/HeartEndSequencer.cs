using System.Collections;
using UnityEngine;
using LushWorld.Camera;

namespace LushWorld.World
{
    // Listens for HeartStone.OnAllHeartsPlaced and drives the end-game sequence:
    // 1. Wait _shakeStartDelay seconds → camera shake begins (earth-moving feel)
    // 2. Wait remaining time until _statuesDelay → all SnailStatues rise over _riseDuration seconds
    // 3. Shake stops when statues finish rising
    // 4. Wait _portalDelayAfterStatues seconds → Portal activates
    public class HeartEndSequencer : MonoBehaviour
    {
        [Header("Statues")]
        [SerializeField] private Transform[] _snailStatues;
        [SerializeField] private float _statuesDelay = 5f;
        [SerializeField] private float _riseDistance = 2.5f;
        [SerializeField] private float _riseDuration = 2.5f;

        [Header("Portal")]
        [SerializeField] private GameObject _portal;
        [SerializeField] private float _portalDelayAfterStatues = 3f;

        [Header("Camera Shake")]
        [SerializeField] private float _shakeStartDelay = 3f;
        [SerializeField] private float _shakeAmplitude  = 2f;

        private float[] _targetYs;

        private void Awake()
        {
            _targetYs = new float[_snailStatues.Length];
            for (int i = 0; i < _snailStatues.Length; i++)
            {
                if (_snailStatues[i] == null) continue;
                _targetYs[i] = _snailStatues[i].position.y;
                Vector3 pos = _snailStatues[i].position;
                pos.y -= _riseDistance;
                _snailStatues[i].position = pos;
            }

            if (_portal != null)
                _portal.SetActive(false);
        }

        private void OnEnable()  => HeartStone.OnAllHeartsPlaced += OnAllHeartsPlaced;
        private void OnDisable() => HeartStone.OnAllHeartsPlaced -= OnAllHeartsPlaced;

        private void OnAllHeartsPlaced() => StartCoroutine(RunSequence());

        private IEnumerator RunSequence()
        {
            // Shake starts _shakeStartDelay seconds after hearts placed and lasts
            // until the statues finish rising — covers the full "earth moving" window
            yield return new WaitForSeconds(_shakeStartDelay);

            float shakeDuration = (_statuesDelay - _shakeStartDelay) + _riseDuration;
            CameraShaker.Instance?.Shake(shakeDuration, _shakeAmplitude);

            yield return new WaitForSeconds(_statuesDelay - _shakeStartDelay);
            yield return RiseAllStatues();
            yield return new WaitForSeconds(_portalDelayAfterStatues);

            if (_portal != null)
                _portal.SetActive(true);
        }

        private IEnumerator RiseAllStatues()
        {
            float[] startYs = new float[_snailStatues.Length];
            for (int i = 0; i < _snailStatues.Length; i++)
            {
                if (_snailStatues[i] != null)
                    startYs[i] = _snailStatues[i].position.y;
            }

            float elapsed = 0f;
            while (elapsed < _riseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _riseDuration));

                for (int i = 0; i < _snailStatues.Length; i++)
                {
                    if (_snailStatues[i] == null) continue;
                    Vector3 pos = _snailStatues[i].position;
                    pos.y = Mathf.Lerp(startYs[i], _targetYs[i], t);
                    _snailStatues[i].position = pos;
                }

                yield return null;
            }

            // Snap to exact final positions
            for (int i = 0; i < _snailStatues.Length; i++)
            {
                if (_snailStatues[i] == null) continue;
                Vector3 pos = _snailStatues[i].position;
                pos.y = _targetYs[i];
                _snailStatues[i].position = pos;
            }
        }
    }
}
