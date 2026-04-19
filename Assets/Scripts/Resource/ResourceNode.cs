using System;
using UnityEngine;
using LushWorld.Inventory;

namespace LushWorld.Resource
{
    // Placed on interactive rock prefabs. No physics — proximity detection is in ResourceInteractor.
    // Phase 2 upgrade: fire a [ServerRpc] instead of calling inventory directly.
    public class ResourceNode : MonoBehaviour
    {
        [SerializeField] private string itemId = "rock_small";
        [SerializeField] private int quantity = 1;

        public string ItemId => itemId;
        public string DisplayName => itemId; // overridden by caller using ItemRegistry if needed

        public event Action<ResourceNode> OnPickedUp;

        public void TryPickup()
        {
            if (!InventorySystem.GiveItem(itemId, quantity)) return;
            OnPickedUp?.Invoke(this);
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;
            Destroy(gameObject);
        }
    }
}
