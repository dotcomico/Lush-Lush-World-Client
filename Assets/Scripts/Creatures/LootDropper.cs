using LushWorld.Enemies;
using LushWorld.Inventory;
using LushWorld.Mobs;
using UnityEngine;

namespace LushWorld.Creatures
{
    // Spawns an item drop when the creature on this GameObject dies.
    // Works for both MobBase and EnemyBase creatures — attach directly to the creature root.
    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private string _itemId;
        [SerializeField] [Range(0f, 1f)] private float _dropChance = 1f;
        [SerializeField] private ItemRegistry _itemRegistry;

        private MobBase _mob;
        private EnemyBase _enemy;

        private void Awake()
        {
            _mob = GetComponent<MobBase>();
            _enemy = GetComponent<EnemyBase>();
        }

        private void OnEnable()
        {
            if (_mob != null) MobBase.OnMobDied += HandleMobDied;
            if (_enemy != null) EnemyBase.OnEnemyDied += HandleEnemyDied;
        }

        private void OnDisable()
        {
            if (_mob != null) MobBase.OnMobDied -= HandleMobDied;
            if (_enemy != null) EnemyBase.OnEnemyDied -= HandleEnemyDied;
        }

        private void HandleMobDied(MobBase died)
        {
            if (died != _mob) return;
            TrySpawnDrop();
        }

        private void HandleEnemyDied(EnemyBase died)
        {
            if (died != _enemy) return;
            TrySpawnDrop();
        }

        private void TrySpawnDrop()
        {
            if (Random.value > _dropChance) return;
            if (_itemRegistry == null || !_itemRegistry.TryGetById(_itemId, out ItemDefinition def)) return;
            if (def.WorldPrefab == null) return;

            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            GameObject drop = Instantiate(def.WorldPrefab, spawnPos, Random.rotation);

            // Ensure a collider exists for ResourceInteractor proximity detection
            if (drop.GetComponentInChildren<Collider>() == null)
            {
                var col = drop.AddComponent<SphereCollider>();
                col.radius = 0.3f;
                col.isTrigger = true;
            }

            // Add physics pop so the drop bounces satisfyingly out of the creature
            var rb = drop.GetComponent<Rigidbody>();
            if (rb == null) rb = drop.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.linearDamping = 0.5f;

            Vector3 popDir = Vector3.up * 3f + Random.insideUnitSphere * 1.5f;
            rb.AddForce(popDir, ForceMode.Impulse);
        }
    }
}
