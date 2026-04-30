using UnityEngine;

namespace LushWorld.Mobs
{
    [CreateAssetMenu(menuName = "Lush World/Mob Definition", fileName = "NewMobDefinition")]
    public class MobDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string mobName = "Mob";

        [Header("Stats")]
        public float maxHealth = 50f;

        [Header("Movement")]
        public float moveSpeed = 1.8f;
        [Tooltip("How far the mob wanders from its spawn position when idle.")]
        public float wanderRadius = 10f;

        [Header("Follow")]
        [Tooltip("Distance at which the mob notices the player and starts following.")]
        public float followRadius = 14f;
        [Tooltip("Mob stops approaching when within this distance of the player.")]
        public float stopDistance = 2.5f;

        [Header("Hop")]
        [Tooltip("How high the visual mesh lifts on each hop.")]
        public float hopHeight = 0.18f;
        [Tooltip("Duration of one full hop cycle (up + down) in seconds.")]
        public float hopDuration = 0.32f;
    }
}
