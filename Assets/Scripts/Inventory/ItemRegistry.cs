using System.Collections.Generic;
using UnityEngine;

namespace LushWorld.Inventory
{
    // Singleton asset. Assign one instance via Inspector on HotbarUI / BackpackUI.
    // UI resolves ItemId strings → ItemDefinition assets through this registry.
    // In Phase 2 this stays unchanged — clients each hold their own copy of this asset.
    [CreateAssetMenu(fileName = "ItemRegistry", menuName = "Lush World/Item Registry")]
    public class ItemRegistry : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> allItems = new List<ItemDefinition>();

        private Dictionary<string, ItemDefinition> _lookup;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, ItemDefinition>(allItems.Count);
            foreach (var item in allItems)
            {
                if (item == null || string.IsNullOrEmpty(item.ItemId)) continue;
                if (!_lookup.TryAdd(item.ItemId, item))
                    Debug.LogWarning($"[ItemRegistry] Duplicate ItemId: '{item.ItemId}'", this);
            }
        }

        public ItemDefinition GetById(string itemId)
        {
            if (_lookup == null) BuildLookup();
            _lookup.TryGetValue(itemId, out var def);
            return def;
        }

        public bool TryGetById(string itemId, out ItemDefinition definition)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(itemId, out definition);
        }
    }
}
