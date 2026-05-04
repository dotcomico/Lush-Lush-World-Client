using System.Collections.Generic;
using LushWorld.Crafting;
using LushWorld.Inventory;
using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI
{
    public class CraftingUI : MenuUIBase, IPanelUI
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

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public void ForceClose() => CraftingSystem.LocalPlayer?.CloseCraftingMenu();

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
            if (_canvas != null) _canvas.sortingOrder = 150;
            if (_panel != null) _panel.SetActive(false);
            InitBackdrop("CraftingBackdrop", () => CraftingSystem.LocalPlayer?.CloseCraftingMenu(), _panel);
            BuildRows();
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
            ApplyMenuToggle(open, _panel, _inputBridge);
            if (open) RefreshAllRows();
            if (open)
                PanelManager.RequestOpen(this);
            else
                PanelManager.NotifyClosed(this);
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
