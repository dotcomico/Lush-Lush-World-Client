using System;
using System.Collections;
using UnityEngine;

namespace LushWorld.Mobs
{
    public class MobBase : MonoBehaviour
    {
        public static event Action<MobBase> OnMobDied;

        [SerializeField] private MobDefinition _definition;

        [Header("Hit Flash")]
        [SerializeField] private Color _hitFlashColor = new Color(1f, 0.4f, 0.1f);
        [SerializeField] private float _hitFlashDuration = 0.12f;

        public MobDefinition Definition => _definition;
        public bool IsDead { get; private set; }

        private float _currentHealth;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private bool _isFlashing;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();

            if (_definition == null)
            {
                Debug.LogError($"[MobBase] No MobDefinition assigned on {name}. Disabling.", this);
                enabled = false;
                return;
            }
            _currentHealth = _definition.maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            _currentHealth = Mathf.Max(0f, _currentHealth - amount);

            if (!_isFlashing)
                StartCoroutine(HitFlashCoroutine());

            if (_currentHealth <= 0f)
                Die();
        }

        private IEnumerator HitFlashCoroutine()
        {
            _isFlashing = true;

            _mpb.SetColor("_BaseColor", _hitFlashColor);
            foreach (var r in _renderers)
                r.SetPropertyBlock(_mpb);

            yield return new WaitForSeconds(_hitFlashDuration);

            _mpb.Clear();
            foreach (var r in _renderers)
                r.SetPropertyBlock(_mpb);

            _isFlashing = false;
        }

        private void Die()
        {
            IsDead = true;
            OnMobDied?.Invoke(this);
            StartCoroutine(DeactivateAfterDelay());
        }

        private IEnumerator DeactivateAfterDelay()
        {
            yield return new WaitForSeconds(0.25f);
            gameObject.SetActive(false);
        }
    }
}
