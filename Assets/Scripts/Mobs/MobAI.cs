using System.Collections;
using UnityEngine;
using LushWorld.Creatures;
using LushWorld.Player;

namespace LushWorld.Mobs
{
    [RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
    [RequireComponent(typeof(MobBase))]
    public class MobAI : CreatureAIBase
    {
        private enum State { Wander, Follow, Dead }

        [SerializeField] private Transform _visualModel;

        private MobBase _base;
        private State _state = State.Wander;

        private void Awake()
        {
            _base = GetComponent<MobBase>();
            BaseAwake();
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
                Agent.speed = _base.Definition.moveSpeed;
                Agent.stoppingDistance = WanderArrivalThreshold;
            }

            if (PlayerStats.LocalPlayer == null)
                Debug.LogWarning("[MobAI] PlayerStats.LocalPlayer not found — mob will wander only.", this);

            BaseStart(); // caches PlayerTransform + starts ThinkLoop
            StartCoroutine(HopLoop());
        }

        private void OnAnyMobDied(MobBase dead)
        {
            if (dead == _base) TransitionTo(State.Dead);
        }

        protected override bool IsDead() => _base.IsDead;

        protected override void EvaluateState()
        {
            if (_base.Definition == null) return;

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

        private void TransitionTo(State next)
        {
            _state = next;

            switch (next)
            {
                case State.Dead:
                    TransitionToDead();
                    break;
                case State.Follow:
                    Agent.stoppingDistance = _base.Definition?.stopDistance ?? 2.5f;
                    break;
                case State.Wander:
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
