using System.Collections.Generic;
using LushWorld.Crafting;
using LushWorld.Inventory;
using UnityEngine;

namespace LushWorld.UI.Crafting
{
    // Embedded crafting list — lives inside BackpackUI as a right-hand sidebar.
    // No MenuUIBase, no canvas, no PanelManager: BackpackUI owns the panel lifecycle.
    // Rows are built once and refreshed whenever the backpack is opened or inventory changes.
    public class CraftingSidebarUI : MonoBehaviour
    {
        [SerializeField] private Transform      _rowContainer;
        [SerializeField] private GameObject     _rowPrefab;
        [SerializeField] private RecipeRegistry _recipeRegistry;
        [SerializeField] private ItemRegistry   _itemRegistry;

        private readonly List<LushWorld.UI.CraftingRecipeRowUI> _rows = new();
        private InventoryData _inventoryData;
        private bool _rowsBuilt;

        private void OnEnable()
        {
            InventorySystem.OnInventoryReady     += HandleInventoryReady;
            InventorySystem.OnInventoryDestroyed += HandleInventoryDestroyed;
            CraftingSystem.OnCraftSuccess        += HandleCraftResult;
            CraftingSystem.OnCraftFailed         += HandleCraftResult;
        }

        private void OnDisable()
        {
            InventorySystem.OnInventoryReady     -= HandleInventoryReady;
            InventorySystem.OnInventoryDestroyed -= HandleInventoryDestroyed;
            CraftingSystem.OnCraftSuccess        -= HandleCraftResult;
            CraftingSystem.OnCraftFailed         -= HandleCraftResult;
            UnsubscribeInventory();
        }

        private void Start()
        {
            BuildRowsOnce();
            // Fallback: InventorySystem.Start() may have fired before our OnEnable.
            if (_inventoryData == null && InventorySystem.LocalPlayer != null)
                HandleInventoryReady(InventorySystem.LocalPlayer.Data);
        }

        private void HandleInventoryReady(InventoryData data)
        {
            UnsubscribeInventory();
            _inventoryData = data;
            data.OnHotbarSlotChanged   += OnSlotChanged;
            data.OnBackpackSlotChanged += OnSlotChanged;
        }

        private void HandleInventoryDestroyed()
        {
            UnsubscribeInventory();
        }

        private void OnSlotChanged(int _, ItemStack __)
        {
            if (gameObject.activeInHierarchy) RefreshAllRows();
        }

        private void HandleCraftResult(string _) => RefreshAllRows();

        private void BuildRowsOnce()
        {
            if (_rowsBuilt) return;
            _rowsBuilt = true;

            if (_recipeRegistry == null || _rowPrefab == null || _rowContainer == null) return;

            foreach (var recipe in _recipeRegistry.Recipes)
            {
                var go  = Instantiate(_rowPrefab, _rowContainer);
                var row = go.GetComponent<LushWorld.UI.CraftingRecipeRowUI>();
                if (row == null) continue;
                row.Init(recipe, _itemRegistry);
                _rows.Add(row);
            }
        }

        public void RefreshAllRows()
        {
            foreach (var row in _rows) row.Refresh();
        }

        private void UnsubscribeInventory()
        {
            if (_inventoryData == null) return;
            _inventoryData.OnHotbarSlotChanged   -= OnSlotChanged;
            _inventoryData.OnBackpackSlotChanged -= OnSlotChanged;
            _inventoryData = null;
        }
    }
}
