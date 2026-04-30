using System;
using StarterAssets;
using UnityEngine;

namespace LushWorld.Player
{
    // Single source of truth for player health and hunger.
    // Same static-event pattern as InventorySystem — UI subscribes, no direct reference needed.
    // Phase 2 upgrade: inherit NetworkBehaviour, Request*() → [ServerRpc(RequireOwnership = true)]
    public class PlayerStats : MonoBehaviour
    {
        public static event Action<float, float> OnStatsReady;   // (healthNorm, hungerNorm)
        public static event Action<float> OnHealthChanged;        // normalised 0-1
        public static event Action<float> OnHungerChanged;        // normalised 0-1
        public static event Action OnPlayerDied;

        public static PlayerStats LocalPlayer { get; private set; }

        [Header("Max Values")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _maxHunger = 100f;

        [Header("Hunger Drain")]
        [SerializeField] private float _passiveHungerDrain = 1f;     // HP/sec at rest
        [SerializeField] private float _sprintHungerMultiplier = 2f; // extra drain while sprinting

        [Header("Starvation")]
        [SerializeField] private float _starvationDamageRate = 2f;   // HP/sec when hunger = 0

        [Header("Health Regen")]
        [SerializeField] private float _healthRegenInterval = 5f;    // seconds between passive regen ticks
        [SerializeField] private float _healthRegenAmount   = 5f;    // HP restored per tick
        [SerializeField] private float _eatHealthBonus      = 15f;   // HP restored on any food consumption

        private float _health;
        private float _healthRegenTimer;
        private float _hunger;
        private bool _isDead;
        private StarterAssetsInputs _inputs;

        public float HealthNormalized => _health / Mathf.Max(1f, _maxHealth);
        public float HungerNormalized => _hunger / Mathf.Max(1f, _maxHunger);
        public float Health => _health;
        public float Hunger => _hunger;

        private void Awake()
        {
            _inputs = GetComponent<StarterAssetsInputs>();
            if (_inputs == null)
                Debug.LogWarning("[PlayerStats] StarterAssetsInputs not found on this GameObject — sprint drain disabled.", this);
        }

        private void Start()
        {
            LocalPlayer = this;
            _health = _maxHealth;
            _hunger = _maxHunger;
            OnStatsReady?.Invoke(HealthNormalized, HungerNormalized);
        }

        private void OnDestroy()
        {
            if (LocalPlayer == this) LocalPlayer = null;
        }

        private void Update()
        {
            if (_isDead) return;
            DrainHunger();
            if (_hunger <= 0f) ApplyStarvationDamage();
            RegenerateHealth();
        }

        // ── Private logic ─────────────────────────────────────────────────────

        private void DrainHunger()
        {
            float rate = _passiveHungerDrain;
            if (_inputs != null && _inputs.sprint) rate *= _sprintHungerMultiplier;

            float newHunger = Mathf.Max(0f, _hunger - rate * Time.deltaTime);
            if (Mathf.Approximately(newHunger, _hunger)) return;

            _hunger = newHunger;
            OnHungerChanged?.Invoke(HungerNormalized);
        }

        private void ApplyStarvationDamage()
        {
            RequestTakeDamage(_starvationDamageRate * Time.deltaTime);
        }

        private void RegenerateHealth()
        {
            if (_health >= _maxHealth) return;
            _healthRegenTimer += Time.deltaTime;
            if (_healthRegenTimer < _healthRegenInterval) return;
            _healthRegenTimer = 0f;
            RequestHeal(_healthRegenAmount);
        }

        // ── Request API (Phase 2: become [ServerRpc]) ─────────────────────────

        public void RequestConsumeFood(float amount)
        {
            if (_isDead) return;
            _hunger = Mathf.Min(_maxHunger, _hunger + amount);
            OnHungerChanged?.Invoke(HungerNormalized);
            RequestHeal(_eatHealthBonus);
        }

        public void RequestTakeDamage(float amount)
        {
            if (_isDead) return;
            _health = Mathf.Max(0f, _health - amount);
            OnHealthChanged?.Invoke(HealthNormalized);
            if (_health <= 0f) HandleDeath();
        }

        public void RequestHeal(float amount)
        {
            if (_isDead) return;
            _health = Mathf.Min(_maxHealth, _health + amount);
            OnHealthChanged?.Invoke(HealthNormalized);
        }

        // ── Global static entry points (called from item use, combat, etc.) ───

        public static void ConsumeFood(float amount) => LocalPlayer?.RequestConsumeFood(amount);
        public static void TakeDamage(float amount)  => LocalPlayer?.RequestTakeDamage(amount);
        public static void Heal(float amount)         => LocalPlayer?.RequestHeal(amount);

        // ── Death & Respawn ───────────────────────────────────────────────────

        private void HandleDeath()
        {
            _isDead = true;
            OnPlayerDied?.Invoke();
            Debug.Log("[PlayerStats] Player died.");
        }

        public void Respawn()
        {
            _isDead = false;
            _health = _maxHealth;
            _hunger = _maxHunger;
            _healthRegenTimer = 0f;
            OnHealthChanged?.Invoke(HealthNormalized);
            OnHungerChanged?.Invoke(HungerNormalized);
        }

        // Restores exact health/hunger from a save file without going through Request* clamping.
        public void LoadState(float health, float hunger)
        {
            _isDead = false;
            _health = Mathf.Clamp(health, 0f, _maxHealth);
            _hunger = Mathf.Clamp(hunger, 0f, _maxHunger);
            _healthRegenTimer = 0f;
            OnHealthChanged?.Invoke(HealthNormalized);
            OnHungerChanged?.Invoke(HungerNormalized);
        }
    }
}
