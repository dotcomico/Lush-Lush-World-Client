using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using LushWorld.Utilities;

namespace LushWorld.Creatures
{
    public abstract class CreatureBase : MonoBehaviour
    {
        public bool IsDead { get; private set; }
        public bool IsKnockedBack { get; private set; }

        public event System.Action OnHit;

        // Subclass returns its typed definition cast to the base type.
        protected abstract CreatureDefinitionBase BaseDefinition { get; }

        // Subclass fires its own static typed event (preserves all spawner subscriptions).
        protected abstract void FireDeathEvent();

        private float _currentHealth;
        private NavMeshAgent _agent;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private bool _isFlashing;

        protected void BaseAwake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _renderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _currentHealth = BaseDefinition.maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            _currentHealth = Mathf.Max(0f, _currentHealth - amount);

            if (!_isFlashing)
                StartCoroutine(HitFlashCoroutine());

            OnHit?.Invoke();

            if (_currentHealth <= 0f)
                Die();
        }

        // Call BEFORE TakeDamage on the same hit — if the hit kills the creature, IsDead
        // is set inside TakeDamage and the guard here would block the coroutine from starting.
        public void ApplyKnockback(Vector3 hitDirection)
        {
            if (IsDead || IsKnockedBack) return;
            if (BaseDefinition == null || !BaseDefinition.enableKnockback) return;

            Vector3 horizontal = new Vector3(hitDirection.x, 0f, hitDirection.z).normalized;
            Vector3 velocity = horizontal * BaseDefinition.knockbackHorizontalForce
                             + Vector3.up * BaseDefinition.knockbackUpwardForce;
            StartCoroutine(KnockbackCoroutine(velocity));
        }

        private IEnumerator KnockbackCoroutine(Vector3 velocity)
        {
            IsKnockedBack = true;

            // updatePosition=false instead of disabling the agent so AI can still call
            // SetDestination without throwing "inactive agent" errors.
            if (_agent != null && _agent.enabled)
            {
                _agent.updatePosition = false;
                _agent.isStopped = true;
            }

            float startY = transform.position.y;
            float elapsed = 0f;
            const float maxDuration = 1f;
            float drag = BaseDefinition.knockbackHorizontalForce * 2f;

            while (elapsed < maxDuration)
            {
                elapsed += Time.deltaTime;

                velocity.y += Physics.gravity.y * Time.deltaTime;
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, drag * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, 0f, drag * Time.deltaTime);

                transform.position += velocity * Time.deltaTime;

                if (velocity.y < 0f && transform.position.y <= startY)
                {
                    Vector3 pos = transform.position;
                    pos.y = startY;
                    transform.position = pos;
                    break;
                }

                yield return null;
            }

            // Re-sync agent to wherever the creature landed.
            // Guard: AI may have disabled the agent if creature died during flight.
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

            _mpb.SetColor("_BaseColor", BaseDefinition.hitFlashColor);
            foreach (var r in _renderers)
                r.SetPropertyBlock(_mpb);

            yield return new WaitForSeconds(BaseDefinition.hitFlashDuration);

            _mpb.Clear();
            foreach (var r in _renderers)
                r.SetPropertyBlock(_mpb);

            _isFlashing = false;
        }

        private void Die()
        {
            IsDead = true;
            FireDeathEvent();
            // Brief delay so knockback and flash coroutines get frames to finish
            // before SetActive(false) cancels them.
            StartCoroutine(DeactivateAfterDelay());
        }

        private IEnumerator DeactivateAfterDelay()
        {
            yield return CoroutineUtils.Wait0_25;
            gameObject.SetActive(false);
        }
    }
}
