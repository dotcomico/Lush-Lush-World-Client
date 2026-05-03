using System;
using UnityEngine;
using LushWorld.Creatures;

namespace LushWorld.Mobs
{
    public class MobBase : CreatureBase
    {
        public static event Action<MobBase> OnMobDied;

        [SerializeField] private MobDefinition _definition;

        public MobDefinition Definition => _definition;

        protected override CreatureDefinitionBase BaseDefinition => _definition;

        private void Awake()
        {
            if (_definition == null)
            {
                Debug.LogError($"[MobBase] No MobDefinition assigned on {name}. Disabling.", this);
                enabled = false;
                return;
            }
            BaseAwake();
        }

        protected override void FireDeathEvent() => OnMobDied?.Invoke(this);
    }
}
