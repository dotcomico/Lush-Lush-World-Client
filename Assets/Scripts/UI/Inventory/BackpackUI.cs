using LushWorld.Inventory;
using LushWorld.UI.Crafting;
using StarterAssets;
using UnityEngine;

namespace LushWorld.UI.Inventory
{
    public class BackpackUI : MonoBehaviour, IPanelUI
    {
        [SerializeField] private GameObject        backpackPanel;
        [SerializeField] private InventorySlotUI[] slots;
        [SerializeField] private ItemRegistry      itemRegistry;
        [SerializeField] private CraftingSidebarUI _craftingSidebar;

        private InventoryData       _data;
        private StarterAssetsInputs _inputBridge;

        public bool IsOpen => backpackPanel != null && backpackPanel.activeSelf;

        public void ForceClose() => InventorySystem.LocalPlayer?.ForceCloseBackpack();

        private void OnEnable()
        {
            InventorySystem.OnInventoryReady      += HandleInventoryReady;
            InventorySystem.OnInventoryDestroyed  += HandleInventoryDestroyed;
            InventorySystem.OnBackpackToggleRequested += HandleBackpackToggle;
        }

        private void Start()
        {
            _inputBridge = FindFirstObjectByType<StarterAssetsInputs>();

            // Fallback: if InventorySystem.Start() fired before our OnEnable(), we missed the event.
            if (_data == null && InventorySystem.LocalPlayer != null)
                HandleInventoryReady(InventorySystem.LocalPlayer.Data);
        }

        private void OnDisable()
        {
            InventorySystem.OnInventoryReady      -= HandleInventoryReady;
            InventorySystem.OnInventoryDestroyed  -= HandleInventoryDestroyed;
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
            if (isOpen)
            {
                UnlockCursor();
                _craftingSidebar?.RefreshAllRows();
                PanelManager.RequestOpen(this);
            }
            else
            {
                RestoreCursor();
                PanelManager.NotifyClosed(this);
            }
        }

        private void UnlockCursor()
        {
            if (_inputBridge != null)
            {
                _inputBridge.look = Vector2.zero;
                _inputBridge.move = Vector2.zero;
                _inputBridge.cursorInputForLook = false;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        private void RestoreCursor()
        {
            if (_inputBridge != null)
            {
                _inputBridge.cursorInputForLook = true;
                _inputBridge.cursorLocked       = true;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void RefreshSlot(int index)
        {
            var stack      = _data.GetBackpackSlot(index);
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
