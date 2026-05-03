using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using LushWorld.Building;
using LushWorld.Creatures;

namespace LushWorld.Player
{
    public class TongueAttack : MonoBehaviour
    {
        [Header("Tongue References")]
        [SerializeField] private Transform _tonguePivot;
        [SerializeField] private LineRenderer _tongueRenderer;

        [Header("Attack Settings")]
        [SerializeField] private float _damage = 20f;
        [SerializeField] private float _extendDistance = 0.6f;
        [SerializeField] private float _extendDuration = 0.12f;
        [SerializeField] private float _holdDuration = 0.05f;
        [SerializeField] private float _retractDuration = 0.18f;
        [SerializeField] private float _cooldown = 0.5f;

        [Header("Hit Detection")]
        [SerializeField] private float _hitRadius = 0.15f;
        [SerializeField] private LayerMask _hitLayers = ~0;

        public static TongueAttack LocalPlayer { get; private set; }

        private InputAction _attackAction;
        private bool _isAttacking;
        private float _lastAttackTime = -999f;
        private WaitForSeconds _holdWait;

        private void Awake()
        {
            LocalPlayer = this;
            _attackAction = GetComponent<PlayerInput>().actions["Player/Attack"];
            _holdWait = new WaitForSeconds(_holdDuration);
        }

        private void OnDestroy()
        {
            if (LocalPlayer == this) LocalPlayer = null;
        }

        private void OnEnable()
        {
            _attackAction.performed += OnAttackPerformed;
        }

        private void OnDisable()
        {
            _attackAction.performed -= OnAttackPerformed;
        }

        private void OnAttackPerformed(InputAction.CallbackContext ctx)
        {
            if (BuildingSystem.IsMenuOpen) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (_isAttacking) return;
            if (Time.time - _lastAttackTime < _cooldown) return;
            RequestAttack();
        }

        // Phase 2: [ServerRpc(RequireOwnership = true)]
        public void RequestAttack()
        {
            StartCoroutine(AttackCoroutine());
        }

        private IEnumerator AttackCoroutine()
        {
            _isAttacking = true;
            _lastAttackTime = Time.time;

            _tongueRenderer.enabled = true;

            yield return TweenTongue(0f, _extendDistance, _extendDuration, easeOut: true);

            Vector3 tip = _tonguePivot.position + _tonguePivot.forward * _extendDistance;
            Collider[] hits = Physics.OverlapSphere(tip, _hitRadius, _hitLayers);
            foreach (Collider col in hits)
            {
                CreatureBase creature = col.GetComponentInParent<CreatureBase>();
                if (creature == null) continue;

                // Knockback BEFORE damage: TakeDamage may set IsDead=true on a killing
                // blow, which would cause ApplyKnockback to early-exit and do nothing.
                Vector3 knockDir = creature.transform.position - _tonguePivot.position;
                creature.ApplyKnockback(knockDir);
                creature.TakeDamage(_damage);
            }

            yield return _holdWait;

            yield return TweenTongue(_extendDistance, 0f, _retractDuration, easeOut: false);

            _tongueRenderer.enabled = false;
            _isAttacking = false;
        }

        // Animates the tip of the LineRenderer along TonguePivot's local +Z axis.
        // positions[0] stays fixed at origin; positions[1] slides forward and back.
        private IEnumerator TweenTongue(float from, float to, float duration, bool easeOut)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float raw = Mathf.Clamp01(elapsed / duration);
                float t01 = easeOut ? 1f - (1f - raw) * (1f - raw) : raw * raw;
                _tongueRenderer.SetPosition(1, Vector3.forward * Mathf.Lerp(from, to, t01));
                yield return null;
            }
            _tongueRenderer.SetPosition(1, Vector3.forward * to);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_tonguePivot == null) return;

            Vector3 origin = _tonguePivot.position;
            Vector3 tip    = origin + _tonguePivot.forward * _extendDistance;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(origin, 0.03f);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(origin, tip);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(tip, _hitRadius);
        }
#endif
    }
}
