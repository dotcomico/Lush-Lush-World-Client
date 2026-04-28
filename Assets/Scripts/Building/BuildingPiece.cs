using UnityEngine;
using LushWorld.Inventory;

namespace LushWorld.Building
{
    // Attached to a building GameObject when BlueprintInstance.TryComplete() succeeds.
    // Manages structural health; refunds 50% of the original cost on demolish or death.
    public class BuildingPiece : MonoBehaviour
    {
        private BuildingDefinition _def;
        private float _health;

        public void Init(BuildingDefinition def)
        {
            _def    = def;
            _health = def.MaxHealth;
        }

        public void TakeDamage(float amount)
        {
            _health -= amount;
            if (_health <= 0f) Die();
        }

        private void Die() => Demolish();

        public void Demolish()
        {
            if (_def != null)
            {
                foreach (var ing in _def.Cost)
                    InventorySystem.GiveItem(ing.ItemId, Mathf.CeilToInt(ing.Quantity * 0.5f));
            }
            Destroy(gameObject);
        }
    }
}
