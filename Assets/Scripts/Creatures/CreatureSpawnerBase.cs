using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using LushWorld.Player;

namespace LushWorld.Creatures
{
    public abstract class CreatureSpawnerBase : MonoBehaviour
    {
        [SerializeField] protected GameObject[] _creaturePrefabs;
        [SerializeField] protected Transform[]  _spawnPoints;

        [Header("Spawn Scatter")]
        [Tooltip("Each creature spawns within this radius of the chosen spawn point.")]
        [SerializeField] private float _spawnScatterRadius = 10f;

        [Header("Proximity Activation")]
        [Tooltip("A spawn point activates when the player enters this radius around it.")]
        [SerializeField] private float _activationRadius = 50f;
        [Tooltip("All creatures despawn when the player exits this radius. Must be > activationRadius.")]
        [SerializeField] private float _deactivationRadius = 80f;
        [Tooltip("How often (seconds) distances are evaluated.")]
        [SerializeField] private float _checkInterval = 1f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog;

        private readonly List<Transform> _nearbyBuffer = new();
        private Transform _player;
        protected bool _isActive;

        // ── Abstracts ────────────────────────────────────────────────────────────

        protected abstract int  GetEffectiveCap();
        protected abstract int  GetActiveCount();
        protected abstract void DestroyAllActive();
        // Called immediately after Instantiate so subclass can track the typed reference.
        protected abstract void OnCreatureSpawned(GameObject instance);

        // ── Gizmo color overrides ─────────────────────────────────────────────────

#if UNITY_EDITOR
        protected virtual Color GizmoScatterColor    => new Color(0.2f, 1f,  0.2f, 0.25f);
        protected virtual Color GizmoActivateColor   => new Color(1f,  0.8f, 0f,   0.2f);
        protected virtual Color GizmoDeactivateColor => new Color(1f,  0.2f, 0f,   0.1f);
#endif

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        protected void BaseStart()
        {
            StartCoroutine(ProximityLoop());
        }

        // ── Core loop ─────────────────────────────────────────────────────────────

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
                        Debug.Log($"[{GetType().Name}] active={_isActive}  count={GetActiveCount()}" +
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

        protected void TopUp()
        {
            while (GetActiveCount() < GetEffectiveCap())
            {
                if (!SpawnOneCreature()) break;
            }
        }

        private void Activate()
        {
            _isActive = true;
            if (_debugLog) Debug.Log($"[{GetType().Name}] ACTIVATED — spawning creatures.", this);
            TopUp();
        }

        private void Deactivate()
        {
            _isActive = false;
            if (_debugLog) Debug.Log($"[{GetType().Name}] DEACTIVATED — destroying {GetActiveCount()} creatures.", this);
            DestroyAllActive();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

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

        private bool SpawnOneCreature()
        {
            if (_creaturePrefabs == null || _creaturePrefabs.Length == 0) return false;
            if (_spawnPoints     == null || _spawnPoints.Length     == 0) return false;

            _nearbyBuffer.Clear();
            foreach (var pt in _spawnPoints)
            {
                if (pt != null && Vector3.Distance(pt.position, _player.position) <= _activationRadius)
                    _nearbyBuffer.Add(pt);
            }

            if (_nearbyBuffer.Count == 0) return false;

            GameObject prefab = _creaturePrefabs[Random.Range(0, _creaturePrefabs.Length)];
            Transform  point  = _nearbyBuffer   [Random.Range(0, _nearbyBuffer.Count)];

            if (prefab == null) return false;

            Vector3 scatter = Random.insideUnitSphere * _spawnScatterRadius;
            scatter.y = 0f;
            Vector3 candidate = point.position + scatter;
            Vector3 spawnPos  = candidate;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, Mathf.Max(3f, _spawnScatterRadius), NavMesh.AllAreas))
                spawnPos = hit.position;

            GameObject instance = Instantiate(prefab, spawnPos, point.rotation);
            OnCreatureSpawned(instance);
            return true;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_spawnPoints == null) return;
            foreach (var pt in _spawnPoints)
            {
                if (pt == null) continue;
                Gizmos.color = GizmoScatterColor;
                Gizmos.DrawSphere(pt.position, _spawnScatterRadius);
                Gizmos.color = GizmoActivateColor;
                Gizmos.DrawSphere(pt.position, _activationRadius);
                Gizmos.color = GizmoDeactivateColor;
                Gizmos.DrawSphere(pt.position, _deactivationRadius);
            }
        }
#endif
    }
}
