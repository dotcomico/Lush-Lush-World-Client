using UnityEngine;
using LushWorld.Creatures;
using LushWorld.Player;
using LushWorld.World;

namespace LushWorld.Enemies
{
    [RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
    [RequireComponent(typeof(EnemyBase))]
    public class EnemyAI : CreatureAIBase
    {
        private enum State { Patrol, Chase, Attack, Dead }

        private EnemyBase _base;
        private State _state = State.Patrol;
        private bool _isNight;

        private void Awake()
        {
            _base = GetComponent<EnemyBase>();
            BaseAwake();
        }

        private void OnEnable()
        {
            DayNightCycle.OnNightStarted += OnNightStarted;
            DayNightCycle.OnDayStarted   += OnDayStarted;
            EnemyBase.OnEnemyDied        += OnAnyEnemyDied;
        }

        private void OnDisable()
        {
            DayNightCycle.OnNightStarted -= OnNightStarted;
            DayNightCycle.OnDayStarted   -= OnDayStarted;
            EnemyBase.OnEnemyDied        -= OnAnyEnemyDied;
        }

        private void Start()
        {
            if (_base.Definition != null)
                Agent.speed = _base.Definition.moveSpeed;

            // Initialize current day/night state (single lookup on Start is acceptable).
            var dnc = FindFirstObjectByType<DayNightCycle>();
            if (dnc != null) _isNight = dnc.IsNight;

            if (PlayerStats.LocalPlayer == null)
                Debug.LogWarning("[EnemyAI] PlayerStats.LocalPlayer not found — AI detection disabled.", this);

            BaseStart(); // caches PlayerTransform + starts ThinkLoop
        }

        private void OnNightStarted() => _isNight = true;
        private void OnDayStarted()   => _isNight = false;

        private void OnAnyEnemyDied(EnemyBase dead)
        {
            if (dead == _base) TransitionTo(State.Dead);
        }

        protected override bool IsDead() => _base.IsDead;

        protected override void EvaluateState()
        {
            if (_base.Definition == null || _base.IsKnockedBack) return;

            // Patrol when passive (day) OR when player reference is missing —
            // enemies must always roam; never freeze waiting for the player.
            if (!CanAggro() || PlayerTransform == null)
            {
                if (_state != State.Patrol) TransitionTo(State.Patrol);
                ExecuteWander(_base.Definition.wanderRadius);
                return;
            }

            State next = EvaluateDistanceState(GetDistanceToPlayer());
            if (next != _state) TransitionTo(next);

            if (next == State.Chase)  Agent.SetDestination(PlayerTransform.position);
            if (next == State.Patrol) ExecuteWander(_base.Definition.wanderRadius);
        }

        private bool CanAggro()
            => _base.Definition.isAlwaysAggressive || _isNight;

        private float GetDistanceToPlayer()
            => Vector3.Distance(transform.position, PlayerTransform.position);

        private State EvaluateDistanceState(float dist)
        {
            if (dist <= _base.Definition.attackRadius)    return State.Attack;
            if (dist <= _base.Definition.detectionRadius) return State.Chase;
            return State.Patrol;
        }

        private void TransitionTo(State next)
        {
            _state = next;

            switch (next)
            {
                case State.Dead:
                    TransitionToDead();
                    break;
                case State.Attack:
                    Agent.ResetPath();
                    break;
            }
        }
    }
}
