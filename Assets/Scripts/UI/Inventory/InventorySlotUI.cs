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
    public class InventorySlotUI : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private TextMeshProUGUI slotNumberText;

        [SerializeField] private Color selectedColor  = new Color(1f,  0.85f, 0.2f, 1f);
        [SerializeField] private Color hoverColor     = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color dropTargetColor = new Color(0.2f, 0.7f, 1f,  0.85f);

        // Fired when the player clicks the slot — consumed by future drag-drop / context menu system.
        public event Action<int, bool> OnSlotClicked;

        private const float DragGhostAlpha = 0.3f;

        private int _slotIndex;
        private bool _isHotbarSlot;
        private bool _isSelected;
        private bool _isHovered;
        private Image _background;
        private Color _normalColor;

        private ItemStack _currentStack;
        private Sprite _currentSprite;

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
            _currentStack = stack;
            _currentSprite = definition?.Icon;

            bool hasItem = !stack.IsEmpty;
            if (hasItem)
                iconImage.sprite = _currentSprite;

            ApplyStackVisuals();
            RefreshBackground();

            if (quantityText != null)
                quantityText.text = hasItem ? stack.Quantity.ToString() : string.Empty;
        }

        // Restores icon to its correct fully-opaque state based on current stack data.
        // Call this after any drag cancel or data-driven refresh.
        private void ApplyStackVisuals()
        {
            bool hasItem = !_currentStack.IsEmpty;
            iconImage.enabled = hasItem;
            if (hasItem)
                iconImage.color = _currentSprite != null ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            RefreshBackground();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Right-click: split floor(qty/2) into the first empty slot in the same section.
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (!_currentStack.IsEmpty)
                    InventorySystem.LocalPlayer?.RequestSplitStack(_slotIndex, _isHotbarSlot);
                return;
            }

            // Shift + Left-click: quick-move the whole stack to the other section.
            if (eventData.button == PointerEventData.InputButton.Left)
            {
#if ENABLE_INPUT_SYSTEM
                bool shiftHeld = UnityEngine.InputSystem.Keyboard.current != null &&
                    (UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed ||
                     UnityEngine.InputSystem.Keyboard.current.rightShiftKey.isPressed);
#else
                bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
                if (shiftHeld && !_currentStack.IsEmpty)
                {
                    InventorySystem.LocalPlayer?.RequestMoveToOtherSection(_slotIndex, _isHotbarSlot);
                    return;
                }
            }

            // Plain left-click: select hotbar slot (no-op on backpack slots).
            OnSlotClicked?.Invoke(_slotIndex, _isHotbarSlot);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            RefreshBackground();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            RefreshBackground();
        }

        // Single source of truth for background color. Priority: drop-target > selected > hover > normal.
        private void RefreshBackground()
        {
            if (_background == null) return;

            bool dragging = InventoryDragController.Instance?.IsDragging ?? false;
            bool isDropTarget = dragging && _isHovered &&
                !(InventoryDragController.Instance.DragSourceIndex == _slotIndex &&
                  InventoryDragController.Instance.DragSourceIsHotbar == _isHotbarSlot);

            _background.color = isDropTarget  ? dropTargetColor :
                                 _isSelected   ? selectedColor   :
                                 _isHovered    ? hoverColor      :
                                                 _normalColor;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_currentStack.IsEmpty || InventoryDragController.Instance == null)
            {
                eventData.pointerDrag = null; // cancel drag for empty slots
                return;
            }

            InventoryDragController.Instance.BeginDrag(_slotIndex, _isHotbarSlot, _currentSprite);
            // Ghost the source slot: keep it visible but faded so the player can see where the item came from.
            iconImage.color = new Color(iconImage.color.r, iconImage.color.g, iconImage.color.b, DragGhostAlpha);
        }

        public void OnDrag(PointerEventData eventData)
        {
            InventoryDragController.Instance?.UpdateDragPosition(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (InventoryDragController.Instance == null) return;

            if (InventoryDragController.Instance.IsDragging)
            {
                // Drag cancelled (released over empty space) — restore full opacity and visibility.
                ApplyStackVisuals();
                InventoryDragController.Instance.EndDrag();
            }
            // else: OnDrop already consumed the drag; data events call SetStack on both slots.
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (InventoryDragController.Instance == null) return;
            if (!InventoryDragController.Instance.TryConsumeDrag(out int srcIndex, out bool srcIsHotbar)) return;
            if (srcIndex == _slotIndex && srcIsHotbar == _isHotbarSlot) return; // dropped on self

            InventorySystem.LocalPlayer?.RequestSwapSlots(srcIndex, srcIsHotbar, _slotIndex, _isHotbarSlot);
        }
    }
}
