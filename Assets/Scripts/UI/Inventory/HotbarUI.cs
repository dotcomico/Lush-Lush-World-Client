using LushWorld.Inventory;
using UnityEngine;

namespace LushWorld.UI.Inventory
{
    // Manages the 8 hotbar slot UIs. Subscribes to InventoryData events obtained
    // through InventorySystem.OnInventoryReady — no direct scene reference needed.
    public class HotbarUI : MonoBehaviour
    {
        [SerializeField] private InventorySlotUI[] slots;
        [SerializeField] private ItemRegistry itemRegistry;

        private InventoryData _data;

        private void OnEnable()
        {
            InventorySystem.OnInventoryReady += HandleInventoryReady;
            InventorySystem.OnInventoryDestroyed += HandleInventoryDestroyed;
        }

        // Fallback: if InventorySystem.Start() fired before our OnEnable(), we missed the event.
        private void Start()
        {
            if (_data == null && InventorySystem.LocalPlayer != null)
                HandleInventoryReady(InventorySystem.LocalPlayer.Data);
        }

        private void OnDisable()
        {
            InventorySystem.OnInventoryReady -= HandleInventoryReady;
            InventorySystem.OnInventoryDestroyed -= HandleInventoryDestroyed;
            UnsubscribeData();
        }

        private void HandleInventoryReady(InventoryData data)
        {
            UnsubscribeData();
            _data = data;
            _data.OnHotbarSlotChanged += HandleSlotChanged;
            _data.OnSelectedHotbarSlotChanged += HandleSelectionChanged;

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Initialize(i, isHotbarSlot: true);
                slots[i].OnSlotClicked += HandleSlotClicked;
                RefreshSlot(i);
            }

            HandleSelectionChanged(_data.SelectedHotbarSlot);
        }

        private void HandleInventoryDestroyed()
        {
            UnsubscribeData();
            _data = null;
        }

        private void HandleSlotChanged(int slotIndex, ItemStack newStack)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length) return;
            RefreshSlot(slotIndex);
        }

        private void HandleSlotClicked(int slotIndex, bool isHotbar)
        {
            InventorySystem.LocalPlayer?.RequestSelectSlot(slotIndex);
        }

        private void HandleSelectionChanged(int selectedIndex)
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i].SetSelected(i == selectedIndex);
        }

        private void RefreshSlot(int index)
        {
            var stack = _data.GetHotbarSlot(index);
            var definition = stack.IsEmpty ? null : itemRegistry.GetById(stack.ItemId);
            slots[index].SetStack(stack, definition);
        }

        private void UnsubscribeData()
        {
            if (_data == null) return;
            _data.OnHotbarSlotChanged -= HandleSlotChanged;
            _data.OnSelectedHotbarSlotChanged -= HandleSelectionChanged;
            for (int i = 0; i < slots.Length; i++)
                slots[i].OnSlotClicked -= HandleSlotClicked;
            _data = null;
        }
    }
}
