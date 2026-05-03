using UnityEngine;
using LushWorld.Creatures;

namespace LushWorld.Enemies
{
    [CreateAssetMenu(menuName = "Lush World/Enemy Definition", fileName = "NewEnemyDefinition")]
    public class EnemyDefinition : CreatureDefinitionBase
    {
        [Header("Detection")]
        public float detectionRadius = 8f;
        public float attackRadius    = 0.8f;

        [Header("Attack")]
        public float attackDamage   = 10f;
        [Tooltip("Seconds between damage ticks when touching the player.")]
        public float attackCooldown = 1f;

        [Header("Behavior")]
        [Tooltip("If true, chases and attacks regardless of time of day. If false, only aggressive at night.")]
        public bool isAlwaysAggressive = true;

        // Sets enemy-specific defaults that differ from the base (enableKnockback = true, wanderRadius = 15).
        // Called by Unity when a new asset is created via CreateAssetMenu or the Inspector Reset button.
        private void Reset()
        {
            creatureName             = "Enemy";
            maxHealth                = 100f;
            moveSpeed                = 2f;
            wanderRadius             = 15f;
            enableKnockback          = true;
            knockbackHorizontalForce = 10f;
            knockbackUpwardForce     = 3f;
            hitFlashColor            = new Color(1f, 0.15f, 0.15f);
            hitFlashDuration         = 0.12f;
        }
    }
}
