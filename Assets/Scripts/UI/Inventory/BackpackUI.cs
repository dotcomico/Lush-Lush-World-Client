using LushWorld.Inventory;
using UnityEngine;

namespace LushWorld.UI.Inventory
{
    // Manages the 4×6 backpack grid and panel visibility.
    // Panel toggle is driven by InventorySystem.OnBackpackToggleRequested.
    public class BackpackUI : MonoBehaviour
    {
        [SerializeField] private GameObject backpackPanel;
        [SerializeField] private InventorySlotUI[] slots;
        [SerializeField] private ItemRegistry itemRegistry;

        private InventoryData _data;

        private void OnEnable()
        {
            InventorySystem.OnInventoryReady += HandleInventoryReady;
            InventorySystem.OnInventoryDestroyed += HandleInventoryDestroyed;
            InventorySystem.OnBackpackToggleRequested += HandleBackpackToggle;
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
            InventorySystem.OnBackpackToggleRequested -= HandleBackpackToggle;
            UnsubscribeData();
        }

        private void HandleInventoryReady(InventoryData data)
        {
            UnsubscribeData();
            _data = data;
            _data.OnBackpackSlotChanged += HandleSlotChanged;

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Initialize(i, isHotbarSlot: false);
                RefreshSlot(i);
            }
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

        private void HandleBackpackToggle(bool isOpen)
        {
            backpackPanel.SetActive(isOpen);
        }

        private void RefreshSlot(int index)
        {
            var stack = _data.GetBackpackSlot(index);
            var definition = stack.IsEmpty ? null : itemRegistry.GetById(stack.ItemId);
            slots[index].SetStack(stack, definition);
        }

        private void UnsubscribeData()
        {
            if (_data == null) return;
            _data.OnBackpackSlotChanged -= HandleSlotChanged;
        }
    }
}
