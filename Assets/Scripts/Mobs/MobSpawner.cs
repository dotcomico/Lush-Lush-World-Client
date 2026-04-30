using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using LushWorld.Player;

namespace LushWorld.Mobs
{
    public class MobSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject[] _mobPrefabs;
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private int _maxMobs = 5;

        [Header("Spawn Scatter")]
        [Tooltip("Each mob spawns at a random point within this radius of the chosen spawn point.")]
        [SerializeField] private float _spawnScatterRadius = 8f;

        [Header("Proximity Activation")]
        [Tooltip("A spawn point activates when the player enters this radius around it.")]
        [SerializeField] private float _activationRadius   = 40f;
        [Tooltip("All mobs despawn when the player exits this radius. Must be > activationRadius.")]
        [SerializeField] private float _deactivationRadius = 70f;
        [Tooltip("How often (seconds) distances are evaluated.")]
        [SerializeField] private float _checkInterval = 1f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog;

        private readonly List<MobBase>  _activeMobs   = new();
        private readonly List<Transform> _nearbyBuffer = new();
        private Transform _player;
        private bool _isActive;

        private void Awake()
        {
            MobBase.OnMobDied += OnMobDied;
        }

        private void Start()
        {
            StartCoroutine(ProximityLoop());
        }

        private void OnDestroy()
        {
            MobBase.OnMobDied -= OnMobDied;
        }

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
                        Debug.Log($"[MobSpawner] active={_isActive}  mobs={_activeMobs.Count}" +
                                  $"  nearestPoint={minDist:F1}u  anyInActivation={anyInActivation}" +
                                  $"  anyInDeactivation={anyInDeactivation}", this);
                    }

                    if (!_isActive && anyInActivation)
                        Activate();
                    else if (_isActive && !anyInDeactivation)
                        Deactivate();
                    else if (_isActive)
                        TopUp();
                }

                yield return wait;
            }
        }

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

        private void TopUp()
        {
            while (_activeMobs.Count < _maxMobs)
            {
                if (!SpawnOne()) break;
            }
        }

        private void Activate()
        {
            _isActive = true;
            if (_debugLog) Debug.Log("[MobSpawner] ACTIVATED — spawning mobs.", this);
            TopUp();
        }

        private void Deactivate()
        {
            _isActive = false;
            if (_debugLog) Debug.Log($"[MobSpawner] DEACTIVATED — destroying {_activeMobs.Count} mobs.", this);
            foreach (var mob in _activeMobs)
            {
                if (mob != null)
                    Destroy(mob.gameObject);
            }
            _activeMobs.Clear();
        }

        private void OnMobDied(MobBase dead)
        {
            _activeMobs.Remove(dead);
        }

        private bool SpawnOne()
        {
            if (_mobPrefabs == null || _mobPrefabs.Length == 0) return false;
            if (_spawnPoints == null || _spawnPoints.Length == 0) return false;

            _nearbyBuffer.Clear();
            foreach (var pt in _spawnPoints)
            {
                if (pt != null && Vector3.Distance(pt.position, _player.position) <= _activationRadius)
                    _nearbyBuffer.Add(pt);
            }

            if (_nearbyBuffer.Count == 0) return false;

            GameObject prefab = _mobPrefabs[Random.Range(0, _mobPrefabs.Length)];
            Transform  point  = _nearbyBuffer[Random.Range(0, _nearbyBuffer.Count)];

            if (prefab == null) return false;

            Vector3 scatter = Random.insideUnitSphere * _spawnScatterRadius;
            scatter.y = 0f;
            Vector3 candidate = point.position + scatter;
            Vector3 spawnPos = candidate;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, Mathf.Max(3f, _spawnScatterRadius), NavMesh.AllAreas))
                spawnPos = hit.position;

            GameObject instance = Instantiate(prefab, spawnPos, point.rotation);
            var mobBase = instance.GetComponent<MobBase>();
            if (mobBase != null)
                _activeMobs.Add(mobBase);

            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_spawnPoints == null) return;
            foreach (var pt in _spawnPoints)
            {
                if (pt == null) continue;
                Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.25f);
                Gizmos.DrawSphere(pt.position, _spawnScatterRadius);
                Gizmos.color = new Color(0f, 0.8f, 1f, 0.18f);
                Gizmos.DrawSphere(pt.position, _activationRadius);
                Gizmos.color = new Color(1f, 0.8f, 0f, 0.08f);
                Gizmos.DrawSphere(pt.position, _deactivationRadius);
            }
        }
#endif
    }
}
