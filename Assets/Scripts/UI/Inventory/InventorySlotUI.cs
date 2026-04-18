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

        [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);

        // Fired when the player clicks the slot — consumed by future drag-drop / context menu system.
        public event Action<int, bool> OnSlotClicked;

        private int _slotIndex;
        private bool _isHotbarSlot;
        private Image _background;
        private Color _normalColor;

        private void Awake()
        {
            _background = GetComponent<Image>();
            if (_background != null)
                _normalColor = _background.color;
        }

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
            bool hasItem = !stack.IsEmpty;

            iconImage.enabled = hasItem;
            if (hasItem)
            {
                var icon = definition?.Icon;
                iconImage.sprite = icon;
                iconImage.color = icon != null ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
            }
            else
            {
                iconImage.color = Color.white;
            }

            if (quantityText != null)
                quantityText.text = hasItem ? stack.Quantity.ToString() : string.Empty;
        }

        public void SetSelected(bool selected)
        {
            if (_background != null)
                _background.color = selected ? selectedColor : _normalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnSlotClicked?.Invoke(_slotIndex, _isHotbarSlot);
        }
    }
}
