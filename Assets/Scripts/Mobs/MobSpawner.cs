using System.Collections.Generic;
using UnityEngine;
using LushWorld.Creatures;
using LushWorld.World;

namespace LushWorld.Mobs
{
    public class MobSpawner : CreatureSpawnerBase
    {
        [SerializeField] private int _maxMobs = 5;

        [Header("Spawn Caps")]
        [Tooltip("-1 = same cap as day. Set >= 0 to use a separate night population.")]
        [SerializeField] private int _maxMobsNight = -1;

        private readonly List<MobBase> _activeMobs = new();
        private bool _isNight;
        private int  _killsThisPhase;

        private void Awake()
        {
            MobBase.OnMobDied            += OnMobDied;
            DayNightCycle.OnNightStarted += OnNightStarted;
            DayNightCycle.OnDayStarted   += OnDayStarted;
        }

        private void Start()
        {
            var dnc = FindFirstObjectByType<DayNightCycle>();
            if (dnc != null) _isNight = dnc.IsNight;

            BaseStart();
        }

        private void OnDestroy()
        {
            MobBase.OnMobDied            -= OnMobDied;
            DayNightCycle.OnNightStarted -= OnNightStarted;
            DayNightCycle.OnDayStarted   -= OnDayStarted;
        }

        // ── Abstracts ─────────────────────────────────────────────────────────────

        protected override int GetEffectiveCap()
        {
            int cap = _isNight && _maxMobsNight >= 0 ? _maxMobsNight : _maxMobs;
            return Mathf.Max(0, cap - _killsThisPhase);
        }

        protected override int GetActiveCount() => _activeMobs.Count;

        protected override void DestroyAllActive()
        {
            foreach (var mob in _activeMobs)
                if (mob != null) Destroy(mob.gameObject);
            _activeMobs.Clear();
        }

        protected override void OnCreatureSpawned(GameObject instance)
        {
            var mb = instance.GetComponent<MobBase>();
            if (mb != null) _activeMobs.Add(mb);
        }

        // ── Gizmo colors (teal) ───────────────────────────────────────────────────

#if UNITY_EDITOR
        protected override Color GizmoScatterColor    => new Color(0.2f, 1f,  0.5f, 0.25f);
        protected override Color GizmoActivateColor   => new Color(0f,   0.8f, 1f,  0.18f);
        protected override Color GizmoDeactivateColor => new Color(1f,   0.8f, 0f,  0.08f);
#endif

        // ── Day / Night ───────────────────────────────────────────────────────────

        private void OnNightStarted()
        {
            _isNight = true;
            _killsThisPhase = 0;
            if (_isActive) TopUp();
        }

        private void OnDayStarted()
        {
            _isNight = false;
            _killsThisPhase = 0;
        }

        private void OnMobDied(MobBase dead)
        {
            _activeMobs.Remove(dead);
            _killsThisPhase++;
        }
    }
}
