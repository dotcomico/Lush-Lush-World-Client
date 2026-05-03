using UnityEngine;
using UnityEngine.Serialization;

namespace LushWorld.Creatures
{
    public abstract class CreatureDefinitionBase : ScriptableObject
    {
        [Header("Identity")]
        public string creatureName = "Creature";

        [Header("Stats")]
        public float maxHealth = 100f;

        [Header("Movement")]
        public float moveSpeed = 2f;
        // FormerlySerializedAs migrates EnemyDefinition.patrolRadius → wanderRadius
        // on existing .asset files without any manual data fix.
        [FormerlySerializedAs("patrolRadius")]
        public float wanderRadius = 10f;

        [Header("Knockback")]
        public bool  enableKnockback         = false;
        public float knockbackHorizontalForce = 10f;
        public float knockbackUpwardForce     = 3f;

        [Header("Hit Flash")]
        public Color hitFlashColor    = new Color(1f, 0.15f, 0.15f);
        public float hitFlashDuration = 0.12f;
    }
}
