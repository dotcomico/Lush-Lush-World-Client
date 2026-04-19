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

        [SerializeField] private ItemRegistry _itemRegistry;

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
            var fromStack = Data.GetSlot(fromIndex, fromHotbar);
            var toStack   = Data.GetSlot(toIndex,   toHotbar);

            // Same item type: merge quantities up to MaxStackSize.
            // If dst is full, TryMergeIntoSlot returns false and we fall through to a normal swap.
            if (!fromStack.IsEmpty && !toStack.IsEmpty && fromStack.ItemId == toStack.ItemId)
            {
                var def      = _itemRegistry != null ? _itemRegistry.GetById(fromStack.ItemId) : null;
                int maxStack = def?.MaxStackSize ?? 64;
                if (Data.TryMergeIntoSlot(fromIndex, fromHotbar, toIndex, toHotbar, maxStack))
                    return;
            }

            Data.TrySwapSlots(fromIndex, fromHotbar, toIndex, toHotbar);
        }

        // Shift-click: moves the full stack at slotIndex to the other section (hotbar↔backpack).
        // Pass 1 merges into existing partial stacks; Pass 2 fills the first empty slot.
        public void RequestMoveToOtherSection(int slotIndex, bool isHotbar)
        {
            var srcStack = Data.GetSlot(slotIndex, isHotbar);
            if (srcStack.IsEmpty) return;

            bool dstHotbar = !isHotbar;
            int  dstSize   = dstHotbar ? InventoryData.HotbarSize : InventoryData.BackpackSize;

            var def      = _itemRegistry != null ? _itemRegistry.GetById(srcStack.ItemId) : null;
            int maxStack = def?.MaxStackSize ?? 64;

            // Pass 1: pour into existing partial stacks of the same item type.
            for (int i = 0; i < dstSize; i++)
            {
                var dstStack = Data.GetSlot(i, dstHotbar);
                if (dstStack.IsEmpty || dstStack.ItemId != srcStack.ItemId) continue;
                Data.TryMergeIntoSlot(slotIndex, isHotbar, i, dstHotbar, maxStack);
                srcStack = Data.GetSlot(slotIndex, isHotbar);
                if (srcStack.IsEmpty) return;
            }

            // Pass 2: place whatever remains in the first empty slot.
            for (int i = 0; i < dstSize; i++)
            {
                if (!Data.GetSlot(i, dstHotbar).IsEmpty) continue;
                Data.TrySwapSlots(slotIndex, isHotbar, i, dstHotbar);
                return;
            }
        }

        public void RequestSplitStack(int slotIndex, bool isHotbar)
        {
            Data.TrySplitStack(slotIndex, isHotbar, out _);
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
