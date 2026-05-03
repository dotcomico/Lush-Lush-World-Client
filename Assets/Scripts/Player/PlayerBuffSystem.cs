using System;
using System.Collections;
using StarterAssets;
using UnityEngine;

namespace LushWorld.Player
{
    // Manages temporary stat buffs applied to the local player.
    // Phase 2 upgrade: wrap ApplyBuff as [ServerRpc] and replicate active-buff state.
    public class PlayerBuffSystem : MonoBehaviour
    {
        private const string GummyRushId = "gummy_rush";

        [Header("Gummy Rush Tuning")]
        [SerializeField] private float _buffDuration    = 30f;
        [SerializeField] private float _speedMultiplier = 2f;
        [SerializeField] private float _jumpMultiplier  = 2f;

        public static PlayerBuffSystem LocalPlayer { get; private set; }

        public static event Action<string, float> OnBuffStarted;  // buffId, duration
        public static event Action<string, float> OnBuffTick;     // buffId, remainingSeconds
        public static event Action<string> OnBuffEnded;

        [SerializeField] private FirstPersonController _fpc;

        private Coroutine _activeCoroutine;
        private string _activeBuffId;

        private float _originalMoveSpeed;
        private float _originalSprintSpeed;
        private float _originalJumpHeight;

        private void Awake()
        {
            LocalPlayer = this;
        }

        private void OnDestroy()
        {
            if (LocalPlayer == this) LocalPlayer = null;
        }

        private void OnEnable()
        {
            PlayerStats.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            PlayerStats.OnPlayerDied -= HandlePlayerDied;
        }

        public void ApplyBuff(string buffId)
        {
            if (buffId != GummyRushId) return;

            if (_activeCoroutine != null)
            {
                // Refresh timer — restore first so we re-cache the unmodified values
                StopCoroutine(_activeCoroutine);
                RestoreGummyRushStats();
            }

            _activeBuffId = buffId;
            _activeCoroutine = StartCoroutine(GummyRushCoroutine());
        }

        private IEnumerator GummyRushCoroutine()
        {
            CacheAndApplyGummyRush();
            OnBuffStarted?.Invoke(GummyRushId, _buffDuration);

            float remaining = _buffDuration;
            while (remaining > 0f)
            {
                yield return Utilities.CoroutineUtils.Wait1;
                remaining -= 1f;
                OnBuffTick?.Invoke(GummyRushId, Mathf.Max(remaining, 0f));
            }

            EndBuff();
        }

        private void CacheAndApplyGummyRush()
        {
            _originalMoveSpeed = _fpc.MoveSpeed;
            _originalSprintSpeed = _fpc.SprintSpeed;
            _originalJumpHeight = _fpc.JumpHeight;

            _fpc.MoveSpeed   *= _speedMultiplier;
            _fpc.SprintSpeed *= _speedMultiplier;
            _fpc.JumpHeight  *= _jumpMultiplier;
        }

        private void RestoreGummyRushStats()
        {
            _fpc.MoveSpeed = _originalMoveSpeed;
            _fpc.SprintSpeed = _originalSprintSpeed;
            _fpc.JumpHeight = _originalJumpHeight;
        }

        private void EndBuff()
        {
            RestoreGummyRushStats();
            string ended = _activeBuffId;
            _activeBuffId = null;
            _activeCoroutine = null;
            OnBuffEnded?.Invoke(ended);
        }

        private void HandlePlayerDied()
        {
            if (_activeCoroutine == null) return;
            StopCoroutine(_activeCoroutine);
            EndBuff();
        }
    }
}
