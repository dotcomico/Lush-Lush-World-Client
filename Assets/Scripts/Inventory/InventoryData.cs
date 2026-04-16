using System;

namespace LushWorld.Inventory
{
    // Pure data model — no MonoBehaviour, no Unity dependencies.
    // All mutations go through the public methods; every mutation fires a C# event.
    //
    // Phase 2 upgrade path:
    //   - _hotbar and _backpack become NetworkVariable<ItemStack>[]
    //   - Events re-fired from NetworkVariable.OnValueChanged callbacks
    //   - Read methods stay identical; write methods are gated behind ServerRpc
    public class InventoryData
    {
        public const int HotbarSize = 8;
        public const int BackpackSize = 24;

        public event Action<int, ItemStack> OnHotbarSlotChanged;
        public event Action<int, ItemStack> OnBackpackSlotChanged;
        public event Action<int> OnSelectedHotbarSlotChanged;

        private readonly ItemStack[] _hotbar = new ItemStack[HotbarSize];
        private readonly ItemStack[] _backpack = new ItemStack[BackpackSize];
        private int _selectedSlot;

        public int SelectedHotbarSlot => _selectedSlot;
        public ItemStack ActiveItem => _hotbar[_selectedSlot];

        public ItemStack GetHotbarSlot(int index) => _hotbar[index];
        public ItemStack GetBackpackSlot(int index) => _backpack[index];

        public bool TryAddItem(ItemStack stack, out int placedIndex)
        {
            placedIndex = -1;
            if (stack.IsEmpty) return false;

            // Try hotbar first, then backpack
            if (TryAddToArray(_hotbar, stack, out placedIndex, isHotbar: true)) return true;

            int backpackIndex;
            if (TryAddToArray(_backpack, stack, out backpackIndex, isHotbar: false))
            {
                placedIndex = backpackIndex;
                return true;
            }

            return false;
        }

        public bool TryRemoveItem(int slotIndex, bool isHotbar, int qty)
        {
            var slots = isHotbar ? _hotbar : _backpack;
            if (slotIndex < 0 || slotIndex >= slots.Length) return false;

            var slot = slots[slotIndex];
            if (slot.IsEmpty || slot.Quantity < qty) return false;

            slots[slotIndex] = slot.Quantity - qty <= 0
                ? ItemStack.Empty
                : new ItemStack(slot.ItemId, slot.Quantity - qty);

            FireSlotChanged(slotIndex, slots[slotIndex], isHotbar);
            return true;
        }

        public bool TrySwapSlots(int fromIndex, bool fromHotbar, int toIndex, bool toHotbar)
        {
            var fromSlots = fromHotbar ? _hotbar : _backpack;
            var toSlots = toHotbar ? _hotbar : _backpack;

            if (fromIndex < 0 || fromIndex >= fromSlots.Length) return false;
            if (toIndex < 0 || toIndex >= toSlots.Length) return false;

            var temp = fromSlots[fromIndex];
            fromSlots[fromIndex] = toSlots[toIndex];
            toSlots[toIndex] = temp;

            FireSlotChanged(fromIndex, fromSlots[fromIndex], fromHotbar);
            FireSlotChanged(toIndex, toSlots[toIndex], toHotbar);
            return true;
        }

        public void SetSelectedHotbarSlot(int index)
        {
            index = Math.Clamp(index, 0, HotbarSize - 1);
            if (_selectedSlot == index) return;
            _selectedSlot = index;
            OnSelectedHotbarSlotChanged?.Invoke(_selectedSlot);
        }

        public void CycleHotbarSelection(int direction)
        {
            int next = (_selectedSlot + direction + HotbarSize) % HotbarSize;
            SetSelectedHotbarSlot(next);
        }

        private bool TryAddToArray(ItemStack[] slots, ItemStack incoming, out int placedIndex, bool isHotbar)
        {
            // First pass: stack onto existing matching stacks
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty || slots[i].ItemId != incoming.ItemId) continue;
                // Stacking not fully implemented yet — just place at first match for now
                // (full stack-splitting comes with the crafting/resource system)
                slots[i] = new ItemStack(incoming.ItemId, slots[i].Quantity + incoming.Quantity);
                placedIndex = i;
                FireSlotChanged(i, slots[i], isHotbar);
                return true;
            }

            // Second pass: first empty slot
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty) continue;
                slots[i] = incoming;
                placedIndex = i;
                FireSlotChanged(i, slots[i], isHotbar);
                return true;
            }

            placedIndex = -1;
            return false;
        }

        private void FireSlotChanged(int index, ItemStack stack, bool isHotbar)
        {
            if (isHotbar)
                OnHotbarSlotChanged?.Invoke(index, stack);
            else
                OnBackpackSlotChanged?.Invoke(index, stack);
        }
    }
}
