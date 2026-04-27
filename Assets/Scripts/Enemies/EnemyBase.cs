using System;
using UnityEngine;

namespace LushWorld.Enemies
{
    public class EnemyBase : MonoBehaviour
    {
        public static event Action<EnemyBase> OnEnemyDied;

        [SerializeField] private EnemyDefinition _definition;

        public EnemyDefinition Definition => _definition;
        public bool IsDead { get; private set; }

        private float _currentHealth;

        private void Awake()
        {
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
            if (_currentHealth <= 0f) Die();
        }

        private void Die()
        {
            IsDead = true;
            OnEnemyDied?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
