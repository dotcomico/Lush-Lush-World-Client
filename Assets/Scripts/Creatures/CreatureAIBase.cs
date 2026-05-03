using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using LushWorld.Player;
using LushWorld.Utilities;

namespace LushWorld.Creatures
{
    public abstract class CreatureAIBase : MonoBehaviour
    {
        protected NavMeshAgent Agent { get; private set; }
        protected Transform PlayerTransform { get; private set; }
        protected Vector3 SpawnPoint { get; private set; }

        protected const float WanderArrivalThreshold = 0.5f;

        // Subclass implements its per-tick state machine.
        protected abstract void EvaluateState();

        // Subclass returns its creature base's IsDead flag.
        protected abstract bool IsDead();

        protected void BaseAwake()
        {
            Agent = GetComponent<NavMeshAgent>();
            SpawnPoint = transform.position;
        }

        // Caches player reference and starts the shared think loop.
        // Call from the subclass Start() after setting agent speed.
        protected void BaseStart()
        {
            if (PlayerStats.LocalPlayer != null)
                PlayerTransform = PlayerStats.LocalPlayer.transform;

            StartCoroutine(ThinkLoop());
        }

        // Shared wander locomotion: picks a random NavMesh point within wanderRadius of
        // SpawnPoint and sets it as the destination once the previous path is complete.
        protected void ExecuteWander(float wanderRadius)
        {
            if (!Agent.enabled || Agent.pathPending) return;
            if (Agent.remainingDistance > WanderArrivalThreshold) return;

            Vector3 randomOffset = Random.insideUnitSphere * wanderRadius;
            randomOffset.y = 0f;
            Vector3 target = SpawnPoint + randomOffset;

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                Agent.SetDestination(hit.position);
        }

        protected void TransitionToDead()
        {
            Agent.enabled = false;
            enabled = false;
        }

        private IEnumerator ThinkLoop()
        {
            while (true)
            {
                if (!IsDead()) EvaluateState();
                yield return CoroutineUtils.Wait0_25;
            }
        }
    }
}
