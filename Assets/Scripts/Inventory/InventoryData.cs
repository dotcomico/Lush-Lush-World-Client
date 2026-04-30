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
        public ItemStack GetSlot(int index, bool isHotbar) => isHotbar ? _hotbar[index] : _backpack[index];

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

        // Merges src quantity into dst (same ItemId) up to maxStackSize.
        // Returns true if any quantity moved; leftover stays in src.
        // Returns false when dst is already full — caller should swap instead.
        public bool TryMergeIntoSlot(int srcIndex, bool srcHotbar, int dstIndex, bool dstHotbar, int maxStackSize)
        {
            var srcSlots = srcHotbar ? _hotbar : _backpack;
            var dstSlots = dstHotbar ? _hotbar : _backpack;

            if (srcIndex < 0 || srcIndex >= srcSlots.Length) return false;
            if (dstIndex < 0 || dstIndex >= dstSlots.Length) return false;

            var src = srcSlots[srcIndex];
            var dst = dstSlots[dstIndex];

            if (src.IsEmpty || src.ItemId != dst.ItemId) return false;

            int canAccept = maxStackSize - dst.Quantity;
            if (canAccept <= 0) return false;

            int transfer = Math.Min(canAccept, src.Quantity);
            dstSlots[dstIndex] = new ItemStack(dst.ItemId, dst.Quantity + transfer);
            int remaining = src.Quantity - transfer;
            srcSlots[srcIndex] = remaining <= 0 ? ItemStack.Empty : new ItemStack(src.ItemId, remaining);

            FireSlotChanged(srcIndex, srcSlots[srcIndex], srcHotbar);
            FireSlotChanged(dstIndex, dstSlots[dstIndex], dstHotbar);
            return true;
        }

        // Splits floor(qty/2) from src into the first empty slot in the same section.
        // Returns false if qty <= 1 or no empty slot is available.
        public bool TrySplitStack(int srcIndex, bool isHotbar, out int dstIndex)
        {
            dstIndex = -1;
            var slots = isHotbar ? _hotbar : _backpack;

            if (srcIndex < 0 || srcIndex >= slots.Length) return false;
            var src = slots[srcIndex];
            if (src.IsEmpty || src.Quantity <= 1) return false;

            for (int i = 0; i < slots.Length; i++)
            {
                if (i == srcIndex || !slots[i].IsEmpty) continue;

                int half = src.Quantity / 2;
                slots[srcIndex] = new ItemStack(src.ItemId, src.Quantity - half);
                slots[i]        = new ItemStack(src.ItemId, half);

                FireSlotChanged(srcIndex, slots[srcIndex], isHotbar);
                FireSlotChanged(i,        slots[i],        isHotbar);

                dstIndex = i;
                return true;
            }

            return false;
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

        public void ClearAll()
        {
            for (int i = 0; i < HotbarSize; i++)
            {
                _hotbar[i] = ItemStack.Empty;
                OnHotbarSlotChanged?.Invoke(i, ItemStack.Empty);
            }
            for (int i = 0; i < BackpackSize; i++)
            {
                _backpack[i] = ItemStack.Empty;
                OnBackpackSlotChanged?.Invoke(i, ItemStack.Empty);
            }
        }

        // Restores full inventory state from a save file; fires all slot-changed events so UI refreshes.
        public void LoadState(ItemStack[] hotbar, ItemStack[] backpack, int selectedSlot)
        {
            if (hotbar != null)
            {
                int len = Math.Min(hotbar.Length, HotbarSize);
                for (int i = 0; i < HotbarSize; i++)
                {
                    _hotbar[i] = i < len ? hotbar[i] : ItemStack.Empty;
                    OnHotbarSlotChanged?.Invoke(i, _hotbar[i]);
                }
            }
            if (backpack != null)
            {
                int len = Math.Min(backpack.Length, BackpackSize);
                for (int i = 0; i < BackpackSize; i++)
                {
                    _backpack[i] = i < len ? backpack[i] : ItemStack.Empty;
                    OnBackpackSlotChanged?.Invoke(i, _backpack[i]);
                }
            }
            _selectedSlot = Math.Clamp(selectedSlot, 0, HotbarSize - 1);
            OnSelectedHotbarSlotChanged?.Invoke(_selectedSlot);
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
