using UnityEngine;
using LushWorld.Creatures;

namespace LushWorld.Mobs
{
    [CreateAssetMenu(menuName = "Lush World/Mob Definition", fileName = "NewMobDefinition")]
    public class MobDefinition : CreatureDefinitionBase
    {
        [Header("Follow")]
        [Tooltip("Distance at which the mob notices the player and starts following.")]
        public float followRadius  = 14f;
        [Tooltip("Mob stops approaching when within this distance of the player.")]
        public float stopDistance  = 2.5f;

        [Header("Hop")]
        [Tooltip("How high the visual mesh lifts on each hop.")]
        public float hopHeight     = 0.28f;
        [Tooltip("Duration of one full hop cycle (up + down) in seconds.")]
        public float hopDuration   = 0.42f;

        private void Reset()
        {
            creatureName    = "Mob";
            enableKnockback = true;
        }
    }
}
