using System;
using LushWorld.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LushWorld.UI.Inventory
{
    // One visual slot cell. Data is always PUSHED in via SetStack() — this component
    // never reads from InventoryData directly. Safe to use for both hotbar and backpack.
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private TextMeshProUGUI slotNumberText;
        [SerializeField] private GameObject highlightBorder;

        // Fired when the player clicks the slot — consumed by future drag-drop / context menu system.
        public event Action<int, bool> OnSlotClicked;

        private int _slotIndex;
        private bool _isHotbarSlot;

        public void Initialize(int slotIndex, bool isHotbarSlot)
        {
            _slotIndex = slotIndex;
            _isHotbarSlot = isHotbarSlot;

            if (slotNumberText != null)
                slotNumberText.text = isHotbarSlot ? (slotIndex + 1).ToString() : string.Empty;

            SetStack(ItemStack.Empty, null);
            SetSelected(false);
        }

        public void SetStack(ItemStack stack, ItemDefinition definition)
        {
            bool hasItem = !stack.IsEmpty && definition != null;

            iconImage.enabled = hasItem;
            if (hasItem) iconImage.sprite = definition.Icon;

            if (quantityText != null)
                quantityText.text = hasItem && stack.Quantity > 1 ? stack.Quantity.ToString() : string.Empty;
        }

        public void SetSelected(bool selected)
        {
            if (highlightBorder != null)
                highlightBorder.SetActive(selected);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnSlotClicked?.Invoke(_slotIndex, _isHotbarSlot);
        }
    }
}
