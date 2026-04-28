using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using LushWorld.Player;
using LushWorld.World;

namespace LushWorld.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyBase))]
    public class EnemyAI : MonoBehaviour
    {
        private enum State { Patrol, Chase, Attack, Dead }

        private NavMeshAgent _agent;
        private EnemyBase _base;
        private Transform _player;
        private State _state = State.Patrol;
        private Vector3 _spawnPoint;
        private bool _isNight;

        private const float ThinkInterval = 0.25f;
        private const float PatrolArrivalThreshold = 0.5f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _base = GetComponent<EnemyBase>();
            _spawnPoint = transform.position;
        }

        private void OnEnable()
        {
            DayNightCycle.OnNightStarted += OnNightStarted;
            DayNightCycle.OnDayStarted += OnDayStarted;
            EnemyBase.OnEnemyDied += OnAnyEnemyDied;
        }

        private void OnDisable()
        {
            DayNightCycle.OnNightStarted -= OnNightStarted;
            DayNightCycle.OnDayStarted -= OnDayStarted;
            EnemyBase.OnEnemyDied -= OnAnyEnemyDied;
        }

        private void Start()
        {
            if (_base.Definition != null)
                _agent.speed = _base.Definition.moveSpeed;

            if (PlayerStats.LocalPlayer != null)
                _player = PlayerStats.LocalPlayer.transform;
            else
                Debug.LogWarning("[EnemyAI] PlayerStats.LocalPlayer not found — AI detection disabled.", this);

            // Initialize current day/night state (single lookup on Start is acceptable).
            var dnc = FindFirstObjectByType<DayNightCycle>();
            if (dnc != null) _isNight = dnc.IsNight;

            StartCoroutine(ThinkLoop());
        }

        private void OnNightStarted() => _isNight = true;
        private void OnDayStarted()   => _isNight = false;

        private void OnAnyEnemyDied(EnemyBase dead)
        {
            if (dead == _base) TransitionTo(State.Dead);
        }

        private IEnumerator ThinkLoop()
        {
            var wait = new WaitForSeconds(ThinkInterval);
            while (true)
            {
                if (_state != State.Dead)
                    EvaluateState();
                yield return wait;
            }
        }

        private void EvaluateState()
        {
            if (_base.Definition == null) return;
            if (_base.IsKnockedBack) return;

            bool canAggro = _base.Definition.isAlwaysAggressive || _isNight;

            // Patrol when passive (day) OR when player reference is missing —
            // enemies must always roam; never freeze waiting for the player.
            if (!canAggro || _player == null)
            {
                if (_state != State.Patrol) TransitionTo(State.Patrol);
                ExecutePatrol();
                return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist <= _base.Definition.attackRadius)
            {
                if (_state != State.Attack) TransitionTo(State.Attack);
            }
            else if (dist <= _base.Definition.detectionRadius)
            {
                if (_state != State.Chase) TransitionTo(State.Chase);
                _agent.SetDestination(_player.position);
            }
            else
            {
                if (_state != State.Patrol) TransitionTo(State.Patrol);
                ExecutePatrol();
            }
        }

        private void TransitionTo(State next)
        {
            _state = next;

            switch (next)
            {
                case State.Dead:
                    _agent.enabled = false;
                    enabled = false;
                    break;
                case State.Attack:
                    _agent.ResetPath();
                    break;
            }
        }

        private void ExecutePatrol()
        {
            if (!_agent.enabled || _agent.pathPending) return;
            if (_agent.remainingDistance > PatrolArrivalThreshold) return;

            Vector3 randomOffset = Random.insideUnitSphere * _base.Definition.patrolRadius;
            randomOffset.y = 0f;
            Vector3 target = _spawnPoint + randomOffset;

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, _base.Definition.patrolRadius, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
        }
    }
}
