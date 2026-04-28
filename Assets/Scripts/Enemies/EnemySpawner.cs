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

        [Header("Spawn Scatter")]
        [Tooltip("Each enemy spawns at a random point within this radius of the chosen spawn point. Prevents enemies piling up.")]
        [SerializeField] private float _spawnScatterRadius = 10f;

        [Header("Proximity Activation")]
        [Tooltip("A spawn point activates when the player enters this radius around it.")]
        [SerializeField] private float _activationRadius   = 50f;
        [Tooltip("A spawn point deactivates when the player exits this radius around it. Must be > activationRadius.")]
        [SerializeField] private float _deactivationRadius = 80f;
        [Tooltip("How often (seconds) distances are evaluated — not per-frame.")]
        [SerializeField] private float _checkInterval = 1f;

        [Header("Debug")]
        [Tooltip("Enable to print distance and state info to the Console each tick.")]
        [SerializeField] private bool _debugLog;

        private readonly List<EnemyBase>  _activeEnemies = new();
        private readonly List<Transform>  _nearbyBuffer  = new();
        private Transform _player;
        private bool _isActive;
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

            StartCoroutine(ProximityLoop());
        }

        private void OnDestroy()
        {
            DayNightCycle.OnNightStarted -= OnNightStarted;
            DayNightCycle.OnDayStarted   -= OnDayStarted;
            EnemyBase.OnEnemyDied        -= OnEnemyDied;
        }

        // Evaluates each spawn point independently — not the spawner's own position.
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
                    bool anyInActivation   = AnyPointWithinRadius(_activationRadius);
                    bool anyInDeactivation = AnyPointWithinRadius(_deactivationRadius);

                    if (_debugLog)
                    {
                        float minDist = MinPointDistance();
                        Debug.Log($"[EnemySpawner] active={_isActive}  enemies={_activeEnemies.Count}" +
                                  $"  nearestPoint={minDist:F1}u  anyInActivation={anyInActivation}" +
                                  $"  anyInDeactivation={anyInDeactivation}", this);
                    }

                    if (!_isActive && anyInActivation)
                        Activate();
                    else if (_isActive && !anyInDeactivation)
                        Deactivate();
                    else if (_isActive)
                        TopUp(); // new points may have entered range while already active
                }

                yield return wait;
            }
        }

        // Returns true if at least one spawn point is within the given radius of the player.
        private bool AnyPointWithinRadius(float radius)
        {
            if (_spawnPoints == null) return false;
            foreach (var pt in _spawnPoints)
            {
                if (pt != null && Vector3.Distance(pt.position, _player.position) <= radius)
                    return true;
            }
            return false;
        }

        private float MinPointDistance()
        {
            float min = float.MaxValue;
            if (_spawnPoints == null) return min;
            foreach (var pt in _spawnPoints)
            {
                if (pt == null) continue;
                float d = Vector3.Distance(pt.position, _player.position);
                if (d < min) min = d;
            }
            return min;
        }

        // Fills the enemy count up to the current cap using only nearby spawn points.
        // Kill debt (_killsThisPhase) reduces the cap so player-killed enemies don't respawn this phase.
        private void TopUp()
        {
            int cap = _isNight ? _maxEnemiesNight : _maxEnemiesDay;
            int effectiveCap = Mathf.Max(0, cap - _killsThisPhase);
            while (_activeEnemies.Count < effectiveCap)
            {
                if (!SpawnOne()) break; // no nearby points available — stop trying
            }
        }

        private void Activate()
        {
            _isActive = true;
            if (_debugLog) Debug.Log("[EnemySpawner] ACTIVATED — spawning enemies.", this);
            TopUp();
        }

        private void Deactivate()
        {
            _isActive = false;
            if (_debugLog) Debug.Log($"[EnemySpawner] DEACTIVATED — destroying {_activeEnemies.Count} enemies.", this);
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
            _killsThisPhase = 0; // new phase — reset kill debt so full night cap is available
            if (!_isActive) return;
            TopUp();
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
            _killsThisPhase = 0; // reset AFTER trimming so system-trimmed enemies don't count as player kills
        }

        private void OnEnemyDied(EnemyBase dead)
        {
            _activeEnemies.Remove(dead);
            _killsThisPhase++; // dead enemy lowers the effective cap for the rest of this phase
        }

        // Spawns one enemy at a randomly chosen spawn point that is within
        // activationRadius of the player. Returns false if no nearby point exists.
        private bool SpawnOne()
        {
            if (_enemyPrefabs == null || _enemyPrefabs.Length == 0) return false;
            if (_spawnPoints  == null || _spawnPoints.Length  == 0) return false;

            _nearbyBuffer.Clear();
            foreach (var pt in _spawnPoints)
            {
                if (pt != null && Vector3.Distance(pt.position, _player.position) <= _activationRadius)
                    _nearbyBuffer.Add(pt);
            }

            if (_nearbyBuffer.Count == 0) return false;

            GameObject prefab = _enemyPrefabs[Random.Range(0, _enemyPrefabs.Length)];
            Transform  point  = _nearbyBuffer [Random.Range(0, _nearbyBuffer.Count)];

            if (prefab == null) return false;

            Vector3 scatter = Random.insideUnitSphere * _spawnScatterRadius;
            scatter.y = 0f;
            Vector3 candidate = point.position + scatter;
            Vector3 spawnPos = candidate;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, Mathf.Max(3f, _spawnScatterRadius), NavMesh.AllAreas))
                spawnPos = hit.position;

            GameObject instance = Instantiate(prefab, spawnPos, point.rotation);
            var enemyBase = instance.GetComponent<EnemyBase>();
            if (enemyBase != null)
                _activeEnemies.Add(enemyBase);

            return true;
        }

#if UNITY_EDITOR
        // Draws radius spheres around each spawn point — not the spawner — so you can
        // see the actual per-point activation zones in the Scene view.
        private void OnDrawGizmosSelected()
        {
            if (_spawnPoints == null) return;
            foreach (var pt in _spawnPoints)
            {
                if (pt == null) continue;
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.25f);
                Gizmos.DrawSphere(pt.position, _spawnScatterRadius);
                Gizmos.color = new Color(1f, 0.8f, 0f, 0.2f);
                Gizmos.DrawSphere(pt.position, _activationRadius);
                Gizmos.color = new Color(1f, 0.2f, 0f, 0.1f);
                Gizmos.DrawSphere(pt.position, _deactivationRadius);
            }
        }
#endif
    }
}
