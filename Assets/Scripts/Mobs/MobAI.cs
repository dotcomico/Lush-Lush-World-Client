using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using LushWorld.Player;

namespace LushWorld.Mobs
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(MobBase))]
    public class MobAI : MonoBehaviour
    {
        private enum State { Wander, Follow, Dead }

        [SerializeField] private Transform _visualModel;

        private NavMeshAgent _agent;
        private MobBase _base;
        private Transform _player;
        private State _state = State.Wander;
        private Vector3 _spawnPoint;

        private const float ThinkInterval = 0.25f;
        private const float WanderArrivalThreshold = 0.5f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _base = GetComponent<MobBase>();
            _spawnPoint = transform.position;
        }

        private void OnEnable()
        {
            MobBase.OnMobDied += OnAnyMobDied;
        }

        private void OnDisable()
        {
            MobBase.OnMobDied -= OnAnyMobDied;
        }

        private void Start()
        {
            if (_base.Definition != null)
            {
                _agent.speed = _base.Definition.moveSpeed;
                _agent.stoppingDistance = WanderArrivalThreshold;
            }

            if (PlayerStats.LocalPlayer != null)
                _player = PlayerStats.LocalPlayer.transform;
            else
                Debug.LogWarning("[MobAI] PlayerStats.LocalPlayer not found — mob will wander only.", this);

            StartCoroutine(ThinkLoop());
            StartCoroutine(HopLoop());
        }

        private void OnAnyMobDied(MobBase dead)
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

            if (_player == null)
            {
                if (_state != State.Wander) TransitionTo(State.Wander);
                ExecuteWander();
                return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist <= _base.Definition.followRadius)
            {
                if (_state != State.Follow) TransitionTo(State.Follow);
                _agent.SetDestination(_player.position);
            }
            else
            {
                if (_state != State.Wander) TransitionTo(State.Wander);
                ExecuteWander();
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
                case State.Follow:
                    _agent.stoppingDistance = _base.Definition?.stopDistance ?? 2.5f;
                    break;
                case State.Wander:
                    _agent.stoppingDistance = WanderArrivalThreshold;
                    break;
            }
        }

        private void ExecuteWander()
        {
            if (!_agent.enabled || _agent.pathPending) return;
            if (_agent.remainingDistance > WanderArrivalThreshold) return;

            Vector3 randomOffset = Random.insideUnitSphere * _base.Definition.wanderRadius;
            randomOffset.y = 0f;
            Vector3 target = _spawnPoint + randomOffset;

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, _base.Definition.wanderRadius, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
        }

        // Hop loop: oscillates _visualModel localY whenever the agent is moving.
        // The NavMeshAgent root stays flat on the NavMesh; only the visual child bounces.
        private IEnumerator HopLoop()
        {
            while (true)
            {
                if (_state == State.Dead)
                {
                    if (_visualModel != null)
                        _visualModel.localPosition = Vector3.zero;
                    yield break;
                }

                bool isMoving = _visualModel != null
                    && _agent.enabled
                    && _agent.velocity.sqrMagnitude > 0.01f;

                if (isMoving && _base.Definition != null)
                {
                    yield return DoHop(
                        _base.Definition.hopHeight,
                        _base.Definition.hopDuration);
                }
                else
                {
                    // Smoothly settle back to rest position
                    if (_visualModel != null && _visualModel.localPosition.y > 0.001f)
                    {
                        Vector3 pos = _visualModel.localPosition;
                        pos.y = Mathf.MoveTowards(pos.y, 0f, 2f * Time.deltaTime);
                        _visualModel.localPosition = pos;
                    }
                    yield return null;
                }
            }
        }

        private IEnumerator DoHop(float hopHeight, float hopDuration)
        {
            float halfDur = hopDuration * 0.5f;
            float elapsed = 0f;

            // Rise
            while (elapsed < halfDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDur);
                _visualModel.localPosition = new Vector3(0f, t * hopHeight, 0f);
                yield return null;
            }

            elapsed = 0f;

            // Fall
            while (elapsed < halfDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDur);
                _visualModel.localPosition = new Vector3(0f, (1f - t) * hopHeight, 0f);
                yield return null;
            }

            _visualModel.localPosition = Vector3.zero;
        }
    }
}
