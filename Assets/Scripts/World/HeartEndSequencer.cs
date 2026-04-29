using System.Collections;
using UnityEngine;

namespace LushWorld.World
{
    // Listens for HeartStone.OnAllHeartsPlaced and drives the end-game sequence:
    // 1. Wait _statuesDelay seconds
    // 2. All SnailStatues rise from underground (SmoothStep eased) over _riseDuration seconds
    // 3. Wait _portalDelayAfterStatues seconds
    // 4. Portal GameObject is activated
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
            yield return new WaitForSeconds(_statuesDelay);
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
