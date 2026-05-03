using System;
using UnityEngine;
using LushWorld.Creatures;

namespace LushWorld.Enemies
{
    public class EnemyBase : CreatureBase
    {
        public static event Action<EnemyBase> OnEnemyDied;

        [SerializeField] private EnemyDefinition _definition;

        public EnemyDefinition Definition => _definition;

        protected override CreatureDefinitionBase BaseDefinition => _definition;

        private void Awake()
        {
            if (_definition == null)
            {
                Debug.LogError($"[EnemyBase] No EnemyDefinition assigned on {name}. Disabling.", this);
                enabled = false;
                return;
            }
            BaseAwake();
        }

        protected override void FireDeathEvent() => OnEnemyDied?.Invoke(this);
    }
}
