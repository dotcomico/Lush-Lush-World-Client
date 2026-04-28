using System.Collections.Generic;
using LushWorld.Crafting;
using LushWorld.Inventory;
using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI
{
    // Nested inside PlayerRig.prefab as CraftingMenuUI.prefab.
    // Subscribes to CraftingSystem static events — no direct scene references needed.
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField] private GameObject      _panel;
        [SerializeField] private Transform       _rowContainer;
        [SerializeField] private GameObject      _rowPrefab;
        [SerializeField] private RecipeRegistry  _recipeRegistry;
        [SerializeField] private ItemRegistry    _itemRegistry;

        private readonly List<CraftingRecipeRowUI> _rows = new();
        private InventoryData       _inventoryData;
        private StarterAssetsInputs _inputBridge;
        private Canvas              _canvas;
        private GameObject          _backdrop;

        private void OnEnable()
        {
            CraftingSystem.OnCraftingMenuToggleRequested += HandleMenuToggle;
            CraftingSystem.OnCraftSuccess += HandleCraftResult;
            CraftingSystem.OnCraftFailed  += HandleCraftResult;
            InventorySystem.OnInventoryReady     += OnInventoryReady;
            InventorySystem.OnInventoryDestroyed += OnInventoryDestroyed;
        }

        private void OnDisable()
        {
            CraftingSystem.OnCraftingMenuToggleRequested -= HandleMenuToggle;
            CraftingSystem.OnCraftSuccess -= HandleCraftResult;
            CraftingSystem.OnCraftFailed  -= HandleCraftResult;
            InventorySystem.OnInventoryReady     -= OnInventoryReady;
            InventorySystem.OnInventoryDestroyed -= OnInventoryDestroyed;
            UnsubscribeInventorySlots();
        }

        private void Start()
        {
            _inputBridge = FindFirstObjectByType<StarterAssetsInputs>();
            _canvas      = GetComponent<Canvas>();

            // Raise sort order so this canvas is always on top of HUD/inventory
            // when visible — backdrop clicks won't be intercepted by other canvases.
            if (_canvas != null) _canvas.sortingOrder = 150;

            if (_panel != null) _panel.SetActive(false);
            CreateBackdrop();
            BuildRows();
        }

        // Full-screen button behind the panel — clicking outside the panel closes the menu.
        private void CreateBackdrop()
        {
            _backdrop = new GameObject("CraftingBackdrop", typeof(RectTransform));
            _backdrop.transform.SetParent(transform, false);
            _backdrop.layer = gameObject.layer;

            var rt = _backdrop.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // Tiny non-zero alpha: invisible to the eye but guarantees raycast detection.
            var img = _backdrop.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.004f);
            img.raycastTarget = true;

            var btn = _backdrop.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => CraftingSystem.LocalPlayer?.CloseCraftingMenu());

            // Backdrop renders BEHIND the panel (lower sibling index = drawn first).
            if (_panel != null)
                _backdrop.transform.SetSiblingIndex(_panel.transform.GetSiblingIndex());

            _backdrop.SetActive(false);
        }

        private void OnInventoryReady(InventoryData data)
        {
            UnsubscribeInventorySlots();
            _inventoryData = data;
            data.OnHotbarSlotChanged   += OnSlotChanged;
            data.OnBackpackSlotChanged += OnSlotChanged;
        }

        private void OnInventoryDestroyed() => UnsubscribeInventorySlots();

        private void UnsubscribeInventorySlots()
        {
            if (_inventoryData == null) return;
            _inventoryData.OnHotbarSlotChanged   -= OnSlotChanged;
            _inventoryData.OnBackpackSlotChanged -= OnSlotChanged;
            _inventoryData = null;
        }

        private void OnSlotChanged(int _, ItemStack __)
        {
            if (_panel != null && _panel.activeSelf) RefreshAllRows();
        }

        private void HandleMenuToggle(bool open)
        {
            if (_panel    != null) _panel.SetActive(open);
            if (_backdrop != null) _backdrop.SetActive(open);

            if (open)
            {
                // Stop camera immediately: clear buffered look + unlock cursor.
                if (_inputBridge != null)
                {
                    _inputBridge.cursorLocked        = false;
                    _inputBridge.cursorInputForLook  = false;
                    _inputBridge.look                = Vector2.zero;
                    _inputBridge.move                = Vector2.zero;
                }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
                RefreshAllRows();
            }
            else
            {
                // Resume camera: re-lock cursor and re-enable look input.
                if (_inputBridge != null)
                {
                    _inputBridge.cursorLocked       = true;
                    _inputBridge.cursorInputForLook = true;
                }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        private void HandleCraftResult(string _) => RefreshAllRows();

        private void BuildRows()
        {
            if (_recipeRegistry == null || _rowPrefab == null || _rowContainer == null) return;

            foreach (var recipe in _recipeRegistry.Recipes)
            {
                var go  = Instantiate(_rowPrefab, _rowContainer);
                var row = go.GetComponent<CraftingRecipeRowUI>();
                if (row == null) continue;
                row.Init(recipe, _itemRegistry);
                _rows.Add(row);
            }
        }

        private void RefreshAllRows()
        {
            foreach (var row in _rows) row.Refresh();
        }
    }
}
