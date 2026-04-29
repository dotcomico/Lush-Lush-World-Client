using UnityEngine;

namespace LushWorld.Camera
{
    // Call CameraShaker.Instance.Shake(duration, amplitude) from any system.
    // CameraShakeExtension (added to all vcams by CameraViewController) reads
    // IsActive + CurrentAmplitude and applies the offset in Cinemachine's own
    // Finalize stage — no LateUpdate timing race with the Brain.
    public class CameraShaker : MonoBehaviour
    {
        public static CameraShaker Instance { get; private set; }

        public bool  IsActive        => _active;
        public float CurrentAmplitude => _shakeAmplitude;

        private float _shakeAmplitude;
        private float _shakeDuration;
        private float _elapsed;
        private bool  _active;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // duration  — seconds to shake
        // amplitude — max positional offset in world units (0.03–0.06 for subtle earth-shake)
        public void Shake(float duration, float amplitude)
        {
            _shakeDuration  = duration;
            _shakeAmplitude = amplitude;
            _elapsed        = 0f;
            _active         = true;
        }

        private void Update()
        {
            if (!_active) return;
            _elapsed += Time.deltaTime;
            if (_elapsed >= _shakeDuration)
                _active = false;
        }
    }
}
