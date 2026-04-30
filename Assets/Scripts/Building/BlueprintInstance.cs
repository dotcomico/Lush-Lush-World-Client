using System;
using System.Collections.Generic;
using LushWorld.Inventory;
using LushWorld.Utilities;
using UnityEngine;

namespace LushWorld.Building
{
    // Added to a skeleton blueprint GameObject at spawn time by BuildingSystem.RequestPlaceBlueprint.
    // Tracks deposited materials; when all Cost ingredients are met, converts skeleton to a real building.
    // Phase 2: RequestDeposit becomes [ServerRpc(RequireOwnership = true)].
    public class BlueprintInstance : MonoBehaviour, IInteractable
    {
        public static event Action<BlueprintInstance> OnCompleted;

        private BuildingDefinition       _def;
        private readonly Dictionary<string, int> _deposited         = new();
        private readonly List<MeshRenderer>      _renderers         = new();
        private readonly List<Material[]>        _originalMaterials = new();
        private SphereCollider _interactionTrigger;

        public BuildingDefinition Def             => _def;
        public string             InteractionLabel => "Add Materials [E]";

        public string GetNextNeededItemId()
        {
            foreach (var ing in _def.Cost)
            {
                _deposited.TryGetValue(ing.ItemId, out int have);
                if (have < ing.Quantity) return ing.ItemId;
            }
            return null;
        }

        // Called immediately after AddComponent by BuildingSystem, before skeleton material is applied.
        // That ordering is intentional: saves the original prefab materials for restoration on complete.
        public void Init(BuildingDefinition def)
        {
            _def = def;

            _renderers.AddRange(GetComponentsInChildren<MeshRenderer>());
            foreach (var mr in _renderers)
                _originalMaterials.Add((Material[])mr.sharedMaterials.Clone());

            foreach (var ing in _def.Cost)
                if (!_deposited.ContainsKey(ing.ItemId))
                    _deposited[ing.ItemId] = 0;

            // Trigger detected by ResourceInteractor's overlap sphere — enables IInteractable prompt.
            _interactionTrigger = gameObject.AddComponent<SphereCollider>();
            _interactionTrigger.isTrigger = true;
            _interactionTrigger.radius = _def.SnapSize * 1.2f;
        }

        public void Interact(GameObject player)
        {
            string nextId = GetNextNeededItemId();
            if (nextId != null) RequestDeposit(nextId, 1);
        }

        public int GetDeposited(string itemId) =>
            _deposited.TryGetValue(itemId, out int n) ? n : 0;

        // Phase 2: [ServerRpc(RequireOwnership = true)]
        public void RequestDeposit(string itemId, int qty)
        {
            if (_def == null || InventorySystem.LocalPlayer == null) return;

            int required = GetTotalRequired(itemId);
            _deposited.TryGetValue(itemId, out int already);
            int stillNeeded = Mathf.Max(0, required - already);
            if (stillNeeded == 0) return;

            int toDeposit = Mathf.Min(qty, stillNeeded);
            toDeposit = Mathf.Min(toDeposit, CountItem(InventorySystem.LocalPlayer.Data, itemId));
            if (toDeposit <= 0) return;

            RemoveFromInventory(itemId, toDeposit);
            _deposited[itemId] = already + toDeposit;
            TryComplete();
        }

        public void Demolish() => Destroy(gameObject);

        // Restores deposited material amounts from a save file (called after Init).
        public void SetDepositedFromSave(Dictionary<string, int> deposits)
        {
            foreach (var kv in deposits)
                _deposited[kv.Key] = kv.Value;
        }

        private int GetTotalRequired(string itemId)
        {
            int total = 0;
            foreach (var ing in _def.Cost)
                if (ing.ItemId == itemId) total += ing.Quantity;
            return total;
        }

        private void TryComplete()
        {
            foreach (var ing in _def.Cost)
            {
                _deposited.TryGetValue(ing.ItemId, out int have);
                if (have < ing.Quantity) return;
            }

            // Restore original prefab materials (skeleton material is replaced).
            for (int i = 0; i < _renderers.Count; i++)
                _renderers[i].sharedMaterials = _originalMaterials[i];

            // Re-enable physics colliders — building is now a real blocking structure.
            foreach (var col in GetComponentsInChildren<Collider>())
                if (col != _interactionTrigger) col.enabled = true;

            gameObject.AddComponent<BuildingPiece>().Init(_def);

            OnCompleted?.Invoke(this);

            // Remove the interaction trigger we added and destroy only this component, not the GO.
            if (_interactionTrigger != null) Destroy(_interactionTrigger);
            Destroy(this);
        }

        private static int CountItem(InventoryData data, string itemId)
        {
            int total = 0;
            for (int i = 0; i < InventoryData.HotbarSize; i++)
            {
                var s = data.GetHotbarSlot(i);
                if (!s.IsEmpty && s.ItemId == itemId) total += s.Quantity;
            }
            for (int i = 0; i < InventoryData.BackpackSize; i++)
            {
                var s = data.GetBackpackSlot(i);
                if (!s.IsEmpty && s.ItemId == itemId) total += s.Quantity;
            }
            return total;
        }

        private static void RemoveFromInventory(string itemId, int qty)
        {
            var inv  = InventorySystem.LocalPlayer;
            var data = inv.Data;
            int remaining = qty;

            for (int i = 0; i < InventoryData.HotbarSize && remaining > 0; i++)
            {
                var s = data.GetHotbarSlot(i);
                if (s.IsEmpty || s.ItemId != itemId) continue;
                int take = Mathf.Min(remaining, s.Quantity);
                inv.RequestRemoveItem(i, isHotbar: true, take);
                remaining -= take;
            }
            for (int i = 0; i < InventoryData.BackpackSize && remaining > 0; i++)
            {
                var s = data.GetBackpackSlot(i);
                if (s.IsEmpty || s.ItemId != itemId) continue;
                int take = Mathf.Min(remaining, s.Quantity);
                inv.RequestRemoveItem(i, isHotbar: false, take);
                remaining -= take;
            }
        }
    }
}
