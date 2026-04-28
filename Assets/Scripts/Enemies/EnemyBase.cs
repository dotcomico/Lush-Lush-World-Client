using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace LushWorld.Enemies
{
    public class EnemyBase : MonoBehaviour
    {
        public static event Action<EnemyBase> OnEnemyDied;

        [SerializeField] private EnemyDefinition _definition;

        [Header("Knockback")]
        [SerializeField] private float _knockbackHorizontalForce = 10f;
        [SerializeField] private float _knockbackUpwardForce = 3f;

        [Header("Hit Flash")]
        [SerializeField] private Color _hitFlashColor = new Color(1f, 0.15f, 0.15f);
        [SerializeField] private float _hitFlashDuration = 0.12f;

        public EnemyDefinition Definition => _definition;
        public bool IsDead { get; private set; }
        public bool IsKnockedBack { get; private set; }

        private float _currentHealth;
        private NavMeshAgent _agent;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private bool _isFlashing;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _renderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();

            if (_definition == null)
            {
                Debug.LogError($"[EnemyBase] No EnemyDefinition assigned on {name}. Disabling.", this);
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

        // MUST be called BEFORE TakeDamage — if the hit kills the enemy, IsDead is set
        // inside TakeDamage and this guard would block the coroutine from ever starting.
        public void ApplyKnockback(Vector3 hitDirection)
        {
            if (IsDead || IsKnockedBack) return;

            Vector3 horizontal = new Vector3(hitDirection.x, 0f, hitDirection.z).normalized;
            Vector3 velocity = horizontal * _knockbackHorizontalForce + Vector3.up * _knockbackUpwardForce;
            StartCoroutine(KnockbackCoroutine(velocity));
        }

        private IEnumerator KnockbackCoroutine(Vector3 velocity)
        {
            IsKnockedBack = true;

            // Use updatePosition=false instead of disabling the agent so EnemyAI
            // can still call SetDestination without throwing "inactive agent" errors.
            if (_agent != null && _agent.enabled)
            {
                _agent.updatePosition = false;
                _agent.isStopped = true;
            }

            float startY = transform.position.y;
            float elapsed = 0f;
            const float maxDuration = 1f;
            float drag = _knockbackHorizontalForce * 2f;

            while (elapsed < maxDuration)
            {
                elapsed += Time.deltaTime;

                velocity.y += Physics.gravity.y * Time.deltaTime;
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, drag * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, 0f, drag * Time.deltaTime);

                transform.position += velocity * Time.deltaTime;

                // Stop as soon as we land back at the original height
                if (velocity.y < 0f && transform.position.y <= startY)
                {
                    Vector3 pos = transform.position;
                    pos.y = startY;
                    transform.position = pos;
                    break;
                }

                yield return null;
            }

            // Re-sync the NavMeshAgent to wherever the enemy landed.
            // Guard: EnemyAI may have disabled the agent if the enemy died during flight.
            if (_agent != null && _agent.enabled)
            {
                _agent.Warp(transform.position);
                _agent.updatePosition = true;
                _agent.isStopped = false;
            }

            IsKnockedBack = false;
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
            OnEnemyDied?.Invoke(this);
            // Brief delay so knockback and flash coroutines get frames to run
            // before the GameObject is deactivated (which would cancel them instantly).
            StartCoroutine(DeactivateAfterDelay());
        }

        private IEnumerator DeactivateAfterDelay()
        {
            yield return new WaitForSeconds(0.25f);
            gameObject.SetActive(false);
        }
    }
}
