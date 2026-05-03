using System;
using UnityEngine;
using LushWorld.Creatures;

namespace LushWorld.Mobs
{
    public class MobBase : CreatureBase
    {
        public static event Action<MobBase> OnMobDied;

        [SerializeField] private MobDefinition _definition;

        [Header("Hit Audio")]
        [SerializeField] private AudioClip _hitScream;
        [SerializeField, Range(0f, 1f)] private float _screamVolume = 0.8f;
        // How far away the player can hear the scream (3D falloff)
        [SerializeField] private float _screamMaxDistance = 20f;

        public MobDefinition Definition => _definition;

        protected override CreatureDefinitionBase BaseDefinition => _definition;

        private AudioSource _audioSource;

        private void Awake()
        {
            if (_definition == null)
            {
                Debug.LogError($"[MobBase] No MobDefinition assigned on {name}. Disabling.", this);
                enabled = false;
                return;
            }

            BaseAwake();
            SetupAudio();
            OnHit += PlayScream;
        }

        private void OnDestroy()
        {
            OnHit -= PlayScream;
        }

        protected override void FireDeathEvent() => OnMobDied?.Invoke(this);

        private void SetupAudio()
        {
            // Always add a new AudioSource for hit sounds so any AudioSource the user
            // placed manually (e.g. idle ambient clip with Play On Awake) is never touched.
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake  = false;
            _audioSource.spatialBlend = 1f;
            _audioSource.rolloffMode  = AudioRolloffMode.Linear;
            _audioSource.minDistance  = 1f;
            _audioSource.maxDistance  = _screamMaxDistance;
        }

        private void PlayScream()
        {
            if (_hitScream == null || _audioSource == null) return;
            _audioSource.PlayOneShot(_hitScream, _screamVolume);
        }
    }
}
