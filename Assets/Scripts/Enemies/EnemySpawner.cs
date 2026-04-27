using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using LushWorld.World;

namespace LushWorld.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject[] _enemyPrefabs;
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private int _maxEnemiesDay = 1;
        [SerializeField] private int _maxEnemiesNight = 5;

        private readonly List<EnemyBase> _activeEnemies = new();

        private void Awake()
        {
            DayNightCycle.OnNightStarted += OnNightStarted;
            DayNightCycle.OnDayStarted += OnDayStarted;
            EnemyBase.OnEnemyDied += OnEnemyDied;

            // Spawn initial enemies matching the current time of day.
            var dnc = FindFirstObjectByType<DayNightCycle>();
            int initialMax = (dnc != null && dnc.IsNight) ? _maxEnemiesNight : _maxEnemiesDay;
            while (_activeEnemies.Count < initialMax)
                SpawnOne();
        }

        private void OnDestroy()
        {
            DayNightCycle.OnNightStarted -= OnNightStarted;
            DayNightCycle.OnDayStarted -= OnDayStarted;
            EnemyBase.OnEnemyDied -= OnEnemyDied;
        }

        private void OnNightStarted()
        {
            while (_activeEnemies.Count < _maxEnemiesNight)
                SpawnOne();
        }

        private void OnDayStarted()
        {
            // Despawn enemies above the daytime cap — kill them instantly.
            while (_activeEnemies.Count > _maxEnemiesDay)
            {
                int last = _activeEnemies.Count - 1;
                EnemyBase enemy = _activeEnemies[last];
                _activeEnemies.RemoveAt(last);
                if (enemy != null) enemy.TakeDamage(float.MaxValue);
            }
        }

        private void OnEnemyDied(EnemyBase dead)
        {
            _activeEnemies.Remove(dead);
        }

        private void SpawnOne()
        {
            if (_enemyPrefabs == null || _enemyPrefabs.Length == 0) return;
            if (_spawnPoints == null || _spawnPoints.Length == 0) return;

            GameObject prefab = _enemyPrefabs[Random.Range(0, _enemyPrefabs.Length)];
            Transform point   = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

            if (prefab == null || point == null) return;

            // Snap to NavMesh surface so enemies don't spawn floating or buried
            // regardless of where the spawn point Transform was placed in the editor.
            Vector3 spawnPos = point.position;
            if (NavMesh.SamplePosition(point.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                spawnPos = hit.position;

            GameObject instance = Instantiate(prefab, spawnPos, point.rotation);
            var enemyBase = instance.GetComponent<EnemyBase>();
            if (enemyBase != null)
                _activeEnemies.Add(enemyBase);
        }
    }
}
