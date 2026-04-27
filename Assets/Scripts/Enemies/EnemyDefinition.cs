using UnityEngine;

namespace LushWorld.Enemies
{
    [CreateAssetMenu(menuName = "Lush World/Enemy Definition", fileName = "NewEnemyDefinition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName = "Enemy";

        [Header("Stats")]
        public float maxHealth = 100f;

        [Header("Movement")]
        public float moveSpeed = 2f;
        public float patrolRadius = 5f;

        [Header("Detection")]
        public float detectionRadius = 8f;
        public float attackRadius = 0.8f;

        [Header("Attack")]
        public float attackDamage = 10f;
        [Tooltip("Seconds between damage ticks when touching the player.")]
        public float attackCooldown = 1f;

        [Header("Behavior")]
        [Tooltip("If true, chases and attacks regardless of time of day. If false, only aggressive at night (GDD default).")]
        public bool isAlwaysAggressive = true;
    }
}
