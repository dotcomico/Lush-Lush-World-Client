using UnityEngine;
using LushWorld.Player;

namespace LushWorld.Enemies
{
    // Place on the AttackZone child GO that has a SphereCollider (isTrigger=true).
    // Damages the player once per attackCooldown while they remain inside the trigger.
    public class ZombieSnailAttack : MonoBehaviour
    {
        private EnemyBase _enemyBase;
        private float _lastAttackTime = float.NegativeInfinity;

        private void Awake()
        {
            _enemyBase = GetComponentInParent<EnemyBase>();
            if (_enemyBase == null)
                Debug.LogError("[ZombieSnailAttack] No EnemyBase found in parent hierarchy.", this);
        }

        private void OnTriggerStay(Collider other)
        {
            if (_enemyBase == null || _enemyBase.IsDead) return;
            if (_enemyBase.Definition == null) return;
            if (Time.time < _lastAttackTime + _enemyBase.Definition.attackCooldown) return;

            // Only damage the player — ignore terrain, props, etc.
            if (other.GetComponent<PlayerStats>() == null) return;

            PlayerStats.TakeDamage(_enemyBase.Definition.attackDamage);
            _lastAttackTime = Time.time;
        }
    }
}
