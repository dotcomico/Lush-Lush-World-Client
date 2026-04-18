using System;
using UnityEngine;

namespace LushWorld.Inventory
{
    // Player-owned MonoBehaviour. Single source of truth for inventory state.
    // All mutations go through Request*() methods — in Phase 2 these become [ServerRpc].
    // Static events let UI find the local player's inventory without a direct scene reference.
    //
    // Phase 2 upgrade: inherit NetworkBehaviour, Request*() → [ServerRpc(RequireOwnership = true)]
    public class InventorySystem : MonoBehaviour
    {
        public static event Action<InventoryData> OnInventoryReady;
        public static event Action OnInventoryDestroyed;
        public static event Action<bool> OnBackpackToggleRequested;

        // The locally-owned player's inventory. In Phase 2 (NGO), set only when IsOwner.
        public static InventorySystem LocalPlayer { get; private set; }

        public InventoryData Data { get; private set; }

        private bool _backpackOpen;

        private void Start()
        {
            LocalPlayer = this;
            Data = new InventoryData();
            OnInventoryReady?.Invoke(Data);
        }

        private void OnDestroy()
        {
            if (LocalPlayer == this) LocalPlayer = null;
            OnInventoryDestroyed?.Invoke();
        }

        // Global entry point for giving an item to the local player from anywhere in the codebase.
        public static bool GiveItem(string itemId, int quantity = 1)
        {
            if (LocalPlayer == null)
            {
                Debug.LogWarning("[Inventory] GiveItem called but no active player found.");
                return false;
            }
            return LocalPlayer.RequestAddItem(new ItemStack(itemId, quantity));
        }

        public bool RequestAddItem(ItemStack stack)
        {
            return Data.TryAddItem(stack, out _);
        }

        public void RequestRemoveItem(int slot, bool isHotbar, int qty)
        {
            Data.TryRemoveItem(slot, isHotbar, qty);
        }

        public void RequestSwapSlots(int fromIndex, bool fromHotbar, int toIndex, bool toHotbar)
        {
            Data.TrySwapSlots(fromIndex, fromHotbar, toIndex, toHotbar);
        }

        public void RequestSelectSlot(int index)
        {
            Data.SetSelectedHotbarSlot(index);
        }

        public void RequestCycleSlot(int direction)
        {
            Data.CycleHotbarSelection(direction);
        }

        public void RequestToggleBackpack()
        {
            _backpackOpen = !_backpackOpen;
            OnBackpackToggleRequested?.Invoke(_backpackOpen);
        }

        public void RequestDropActiveItem()
        {
            var active = Data.ActiveItem;
            if (active.IsEmpty) return;

            // Spawn world prefab — registry lookup handled by caller context
            // Full drop logic (with ItemRegistry) wired in Phase 2 resource system
            Data.TryRemoveItem(Data.SelectedHotbarSlot, isHotbar: true, qty: 1);
        }
    }
}
