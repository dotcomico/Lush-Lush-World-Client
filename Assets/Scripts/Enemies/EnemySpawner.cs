using System.Collections.Generic;
using UnityEngine;
using LushWorld.Creatures;
using LushWorld.World;

namespace LushWorld.Enemies
{
    public class EnemySpawner : CreatureSpawnerBase
    {
        [SerializeField] private int _maxEnemiesDay   = 3;
        [SerializeField] private int _maxEnemiesNight = 6;

        private readonly List<EnemyBase> _activeEnemies = new();
        private bool _isNight;
        private int  _killsThisPhase;

        private void Awake()
        {
            DayNightCycle.OnNightStarted += OnNightStarted;
            DayNightCycle.OnDayStarted   += OnDayStarted;
            EnemyBase.OnEnemyDied        += OnEnemyDied;
        }

        private void Start()
        {
            var dnc = FindFirstObjectByType<DayNightCycle>();
            if (dnc != null) _isNight = dnc.IsNight;

            BaseStart();
        }

        private void OnDestroy()
        {
            DayNightCycle.OnNightStarted -= OnNightStarted;
            DayNightCycle.OnDayStarted   -= OnDayStarted;
            EnemyBase.OnEnemyDied        -= OnEnemyDied;
        }

        // ── Abstracts ─────────────────────────────────────────────────────────────

        protected override int GetEffectiveCap()
            => Mathf.Max(0, (_isNight ? _maxEnemiesNight : _maxEnemiesDay) - _killsThisPhase);

        protected override int GetActiveCount() => _activeEnemies.Count;

        protected override void DestroyAllActive()
        {
            foreach (var enemy in _activeEnemies)
                if (enemy != null) Destroy(enemy.gameObject);
            _activeEnemies.Clear();
        }

        protected override void OnCreatureSpawned(GameObject instance)
        {
            var eb = instance.GetComponent<EnemyBase>();
            if (eb != null) _activeEnemies.Add(eb);
        }

        // ── Gizmo colors (green) ──────────────────────────────────────────────────

#if UNITY_EDITOR
        protected override Color GizmoScatterColor    => new Color(0.2f, 1f,  0.2f, 0.25f);
        protected override Color GizmoActivateColor   => new Color(1f,  0.8f, 0f,   0.2f);
        protected override Color GizmoDeactivateColor => new Color(1f,  0.2f, 0f,   0.1f);
#endif

        // ── Day / Night ───────────────────────────────────────────────────────────

        private void OnNightStarted()
        {
            _isNight = true;
            _killsThisPhase = 0; // new phase — full night cap available
            if (!_isActive) return;
            TopUp();
        }

        private void OnDayStarted()
        {
            _isNight = false;
            if (!_isActive) return;

            // Trim excess enemies down to the day cap.
            while (_activeEnemies.Count > _maxEnemiesDay)
            {
                int last = _activeEnemies.Count - 1;
                EnemyBase enemy = _activeEnemies[last];
                _activeEnemies.RemoveAt(last);
                if (enemy != null) enemy.TakeDamage(float.MaxValue);
            }
            _killsThisPhase = 0; // reset AFTER trim so system-trimmed enemies don't count
        }

        private void OnEnemyDied(EnemyBase dead)
        {
            _activeEnemies.Remove(dead);
            _killsThisPhase++;
        }
    }
}
