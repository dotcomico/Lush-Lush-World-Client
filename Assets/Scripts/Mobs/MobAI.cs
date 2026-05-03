using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using LushWorld.Creatures;
using LushWorld.Player;

namespace LushWorld.Mobs
{
    [RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
    [RequireComponent(typeof(MobBase))]
    public class MobAI : CreatureAIBase
    {
        private enum State { Wander, Follow, Flee, Dead }

        private const float FleeDuration  = 5f;
        private const float FleeDistance  = 12f;

        [SerializeField] private Transform _visualModel;

        private MobBase _base;
        private State _state = State.Wander;
        private Coroutine _fleeCoroutine;

        private void Awake()
        {
            _base = GetComponent<MobBase>();
            BaseAwake();
        }

        private void OnEnable()
        {
            MobBase.OnMobDied += OnAnyMobDied;
            if (_base != null) _base.OnHit += OnHit;
        }

        private void OnDisable()
        {
            MobBase.OnMobDied -= OnAnyMobDied;
            if (_base != null) _base.OnHit -= OnHit;
        }

        private void Start()
        {
            if (_base.Definition != null)
            {
                Agent.speed = _base.Definition.moveSpeed;
                Agent.stoppingDistance = WanderArrivalThreshold;
            }

            if (PlayerStats.LocalPlayer == null)
                Debug.LogWarning("[MobAI] PlayerStats.LocalPlayer not found — mob will wander only.", this);

            BaseStart();
            StartCoroutine(HopLoop());
        }

        private void OnAnyMobDied(MobBase dead)
        {
            if (dead == _base) TransitionTo(State.Dead);
        }

        private void OnHit()
        {
            if (_base.IsDead) return;
            if (_fleeCoroutine != null) StopCoroutine(_fleeCoroutine);
            TransitionTo(State.Flee);
            _fleeCoroutine = StartCoroutine(FleeRoutine());
        }

        protected override bool IsDead() => _base.IsDead;

        protected override void EvaluateState()
        {
            if (_base.Definition == null) return;
            if (_state == State.Flee) return; // flee coroutine owns this state

            if (PlayerTransform == null)
            {
                if (_state != State.Wander) TransitionTo(State.Wander);
                ExecuteWander(_base.Definition.wanderRadius);
                return;
            }

            float dist = Vector3.Distance(transform.position, PlayerTransform.position);

            if (dist <= _base.Definition.followRadius)
            {
                if (_state != State.Follow) TransitionTo(State.Follow);
                Agent.SetDestination(PlayerTransform.position);
            }
            else
            {
                if (_state != State.Wander) TransitionTo(State.Wander);
                ExecuteWander(_base.Definition.wanderRadius);
            }
        }

        private IEnumerator FleeRoutine()
        {
            SetFleeDestination();

            float elapsed = 0f;
            while (elapsed < FleeDuration && !_base.IsDead)
            {
                elapsed += Time.deltaTime;
                if (Agent.enabled && !Agent.pathPending && Agent.remainingDistance < 1f)
                    SetFleeDestination();
                yield return null;
            }

            if (!_base.IsDead) TransitionTo(State.Wander);
            _fleeCoroutine = null;
        }

        private void SetFleeDestination()
        {
            if (!Agent.enabled) return;

            Vector3 fleeDir;
            if (PlayerTransform != null)
                fleeDir = (transform.position - PlayerTransform.position);
            else
                fleeDir = Random.insideUnitSphere;

            fleeDir.y = 0f;
            if (fleeDir.sqrMagnitude < 0.01f)
                fleeDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            fleeDir.Normalize();

            // Spread the escape angle so mobs don't all run in the exact same line
            fleeDir = Quaternion.Euler(0f, Random.Range(-40f, 40f), 0f) * fleeDir;

            Vector3 target = transform.position + fleeDir * FleeDistance;
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, FleeDistance, NavMesh.AllAreas))
                Agent.SetDestination(hit.position);
        }

        private void TransitionTo(State next)
        {
            _state = next;

            float baseSpeed = _base.Definition?.moveSpeed ?? 2f;

            switch (next)
            {
                case State.Dead:
                    TransitionToDead();
                    break;
                case State.Flee:
                    Agent.speed = baseSpeed * 2f;
                    Agent.stoppingDistance = 0f;
                    break;
                case State.Follow:
                    Agent.speed = baseSpeed;
                    Agent.stoppingDistance = _base.Definition?.stopDistance ?? 2.5f;
                    break;
                case State.Wander:
                    Agent.speed = baseSpeed;
                    Agent.stoppingDistance = WanderArrivalThreshold;
                    break;
            }
        }

        // Oscillates _visualModel localY whenever the agent is moving.
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
                    && Agent.enabled
                    && Agent.velocity.sqrMagnitude > 0.01f;

                if (isMoving && _base.Definition != null)
                {
                    yield return DoHop(
                        _base.Definition.hopHeight,
                        _base.Definition.hopDuration);
                }
                else
                {
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

            while (elapsed < halfDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDur);
                _visualModel.localPosition = new Vector3(0f, t * hopHeight, 0f);
                yield return null;
            }

            elapsed = 0f;

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
