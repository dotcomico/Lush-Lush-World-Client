using System.Collections.Generic;
using LushWorld.Building;
using LushWorld.Inventory;
using LushWorld.UI;
using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI.Building
{
    public class BuildingMenuUI : MenuUIBase, IPanelUI
    {
        [SerializeField] private GameObject       _panel;
        [SerializeField] private Transform        _rowContainer;
        [SerializeField] private GameObject       _rowPrefab;
        [SerializeField] private BuildingRegistry _buildingRegistry;
        [SerializeField] private ItemRegistry     _itemRegistry;

        private readonly List<BuildingMenuPieceRowUI> _rows = new();
        private InventoryData       _inventoryData;
        private StarterAssetsInputs _inputBridge;
        private Canvas              _canvas;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public void ForceClose() => BuildingSystem.LocalPlayer?.CloseBuildingMenu();

        private void OnEnable()
        {
            BuildingSystem.OnBuildingMenuToggleRequested += HandleMenuToggle;
            InventorySystem.OnInventoryReady             += HandleInventoryReady;
            InventorySystem.OnInventoryDestroyed         += HandleInventoryDestroyed;
        }

        private void OnDisable()
        {
            BuildingSystem.OnBuildingMenuToggleRequested -= HandleMenuToggle;
            InventorySystem.OnInventoryReady             -= HandleInventoryReady;
            InventorySystem.OnInventoryDestroyed         -= HandleInventoryDestroyed;
            UnsubscribeInventory();
        }

        private void Start()
        {
            _inputBridge = FindFirstObjectByType<StarterAssetsInputs>();
            _canvas      = GetComponent<Canvas>();
            if (_canvas != null) _canvas.sortingOrder = 150;
            if (_panel  != null) _panel.SetActive(false);
            InitBackdrop("BuildingBackdrop", () => BuildingSystem.LocalPlayer?.CloseBuildingMenu(), _panel);
            BuildRows();
            if (InventorySystem.LocalPlayer != null)
                HandleInventoryReady(InventorySystem.LocalPlayer.Data);
        }

        private void HandleInventoryReady(InventoryData data)
        {
            UnsubscribeInventory();
            _inventoryData = data;
            data.OnHotbarSlotChanged   += OnSlotChanged;
            data.OnBackpackSlotChanged += OnSlotChanged;
        }

        private void HandleInventoryDestroyed() => UnsubscribeInventory();

        private void UnsubscribeInventory()
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

        private void BuildRows()
        {
            if (_buildingRegistry == null || _rowPrefab == null || _rowContainer == null) return;
            foreach (var def in _buildingRegistry.Pieces)
            {
                var go  = Instantiate(_rowPrefab, _rowContainer);
                var row = go.GetComponent<BuildingMenuPieceRowUI>();
                if (row == null) continue;
                row.Init(def, _itemRegistry);
                _rows.Add(row);
            }
        }

        private void RefreshAllRows()
        {
            foreach (var row in _rows) row.Refresh();
        }
    }
}
