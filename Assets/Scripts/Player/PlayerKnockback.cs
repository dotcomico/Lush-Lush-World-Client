using System.Collections;
using UnityEngine;
using StarterAssets;

namespace LushWorld.Player
{
    // Attach to PlayerCapsule (same GameObject as FirstPersonController and PlayerStats).
    // Provides Minecraft-style knockback on hit: horizontal push away from attacker + upward hop.
    // Static entry: PlayerKnockback.Knockback(hitDirection) — mirrors PlayerStats.TakeDamage pattern.
    public class PlayerKnockback : MonoBehaviour
    {
        public static PlayerKnockback LocalKnockback { get; private set; }

        [Header("Knockback")]
        [SerializeField] private float _knockbackHorizontalForce = 8f;
        [SerializeField] private float _knockbackUpwardForce = 3.5f;

        private CharacterController _controller;
        private FirstPersonController _fpc;
        private bool _isKnockedBack;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _fpc = GetComponent<FirstPersonController>();
        }

        private void Start()
        {
            LocalKnockback = this;
        }

        private void OnDestroy()
        {
            if (LocalKnockback == this) LocalKnockback = null;
        }

        // Global entry point — call from any damage source just like PlayerStats.TakeDamage.
        // hitDirection = world-space vector pointing FROM the attacker TO the player.
        public static void Knockback(Vector3 hitDirection)
            => LocalKnockback?.ApplyKnockback(hitDirection);

        public void ApplyKnockback(Vector3 hitDirection)
        {
            if (_isKnockedBack) return;

            Vector3 horizontal = new Vector3(hitDirection.x, 0f, hitDirection.z).normalized;
            Vector3 velocity = horizontal * _knockbackHorizontalForce + Vector3.up * _knockbackUpwardForce;
            StartCoroutine(KnockbackCoroutine(velocity));
        }

        private IEnumerator KnockbackCoroutine(Vector3 velocity)
        {
            _isKnockedBack = true;

            // Pause FPC's horizontal movement so it doesn't fight the knockback.
            // DisableHorizontalMovement keeps the component enabled so camera rotation
            // (FPC.LateUpdate) and gravity tracking (FPC.JumpAndGravity) keep running.
            if (_fpc != null) _fpc.DisableHorizontalMovement = true;

            float drag = _knockbackHorizontalForce * 2f;
            float elapsed = 0f;
            const float maxDuration = 1.5f; // safety — stops if player never lands
            bool hasLeftGround = false;

            while (elapsed < maxDuration)
            {
                elapsed += Time.deltaTime;

                velocity.y += Physics.gravity.y * Time.deltaTime;
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, drag * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, 0f, drag * Time.deltaTime);

                _controller.Move(velocity * Time.deltaTime);

                // Wait until the player has actually left the ground before checking for landing,
                // otherwise isGrounded=true on the first frame would exit the coroutine immediately.
                if (!_controller.isGrounded) hasLeftGround = true;
                if (hasLeftGround && _controller.isGrounded) break;

                yield return null;
            }

            if (_fpc != null) _fpc.DisableHorizontalMovement = false;
            _isKnockedBack = false;
        }
    }
}
