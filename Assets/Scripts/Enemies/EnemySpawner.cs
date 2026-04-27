using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using LushWorld.Player;
using LushWorld.World;

namespace LushWorld.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject[] _enemyPrefabs;
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private int _maxEnemiesDay   = 3;
        [SerializeField] private int _maxEnemiesNight = 6;

        [Header("Proximity Activation")]
        [Tooltip("Enemies spawn when the player enters this radius.")]
        [SerializeField] private float _activationRadius   = 50f;
        [Tooltip("Enemies are destroyed when the player exits this radius. Must be > activationRadius.")]
        [SerializeField] private float _deactivationRadius = 80f;
        [Tooltip("How often (seconds) the spawner checks player distance.")]
        [SerializeField] private float _checkInterval = 1f;

        private readonly List<EnemyBase> _activeEnemies = new();
        private Transform _player;
        private bool _isActive;
        private bool _isNight;

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

            StartCoroutine(ProximityLoop());
        }

        private void OnDestroy()
        {
            DayNightCycle.OnNightStarted -= OnNightStarted;
            DayNightCycle.OnDayStarted   -= OnDayStarted;
            EnemyBase.OnEnemyDied        -= OnEnemyDied;
        }

        // Checks player distance on a fixed interval — not every frame.
        // Lazy player lookup handles Start() execution-order race with PlayerStats.
        private IEnumerator ProximityLoop()
        {
            var wait = new WaitForSeconds(_checkInterval);
            while (true)
            {
                if (_player == null && PlayerStats.LocalPlayer != null)
                    _player = PlayerStats.LocalPlayer.transform;

                if (_player != null)
                {
                    float dist = Vector3.Distance(transform.position, _player.position);

                    if (!_isActive && dist <= _activationRadius)
                        Activate();
                    else if (_isActive && dist > _deactivationRadius)
                        Deactivate();
                }
                yield return wait;
            }
        }

        private void Activate()
        {
            _isActive = true;
            int cap = _isNight ? _maxEnemiesNight : _maxEnemiesDay;
            while (_activeEnemies.Count < cap)
                SpawnOne();
        }

        private void Deactivate()
        {
            _isActive = false;
            foreach (var enemy in _activeEnemies)
            {
                if (enemy != null)
                    Destroy(enemy.gameObject);
            }
            _activeEnemies.Clear();
        }

        private void OnNightStarted()
        {
            _isNight = true;
            if (!_isActive) return;
            while (_activeEnemies.Count < _maxEnemiesNight)
                SpawnOne();
        }

        private void OnDayStarted()
        {
            _isNight = false;
            if (!_isActive) return;
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
            if (_spawnPoints  == null || _spawnPoints.Length  == 0) return;

            GameObject prefab = _enemyPrefabs[Random.Range(0, _enemyPrefabs.Length)];
            Transform  point  = _spawnPoints [Random.Range(0, _spawnPoints.Length)];

            if (prefab == null || point == null) return;

            Vector3 spawnPos = point.position;
            if (NavMesh.SamplePosition(point.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                spawnPos = hit.position;

            GameObject instance = Instantiate(prefab, spawnPos, point.rotation);
            var enemyBase = instance.GetComponent<EnemyBase>();
            if (enemyBase != null)
                _activeEnemies.Add(enemyBase);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, _activationRadius);
            Gizmos.color = new Color(1f, 0.2f, 0f, 0.1f);
            Gizmos.DrawSphere(transform.position, _deactivationRadius);
        }
#endif
    }
}
