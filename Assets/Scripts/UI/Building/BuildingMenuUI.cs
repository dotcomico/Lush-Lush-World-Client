using LushWorld.Building;
using LushWorld.Inventory;
using LushWorld.UI;
using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI.Building
{
    // Nested inside PlayerRig.prefab as BuildingMenuUI.prefab.
    // Subscribes to BuildingSystem static events — no direct scene references needed.
    // Follows the same open/close + backdrop + cursor pattern as CraftingUI.
    public class BuildingMenuUI : MenuUIBase
    {
        [SerializeField] private GameObject       _panel;
        [SerializeField] private Transform        _rowContainer;
        [SerializeField] private GameObject       _rowPrefab;
        [SerializeField] private BuildingRegistry _buildingRegistry;

        private StarterAssetsInputs _inputBridge;
        private Canvas              _canvas;

        private void OnEnable()  => BuildingSystem.OnBuildingMenuToggleRequested += HandleMenuToggle;
        private void OnDisable() => BuildingSystem.OnBuildingMenuToggleRequested -= HandleMenuToggle;

        private void Start()
        {
            _inputBridge = FindFirstObjectByType<StarterAssetsInputs>();
            _canvas      = GetComponent<Canvas>();
            if (_canvas != null) _canvas.sortingOrder = 150;
            if (_panel  != null) _panel.SetActive(false);
            InitBackdrop("BuildingBackdrop", () => BuildingSystem.LocalPlayer?.CloseBuildingMenu(), _panel);
            BuildRows();
        }

        private void HandleMenuToggle(bool open) => ApplyMenuToggle(open, _panel, _inputBridge);

        private void BuildRows()
        {
            if (_buildingRegistry == null || _rowPrefab == null || _rowContainer == null) return;
            foreach (var def in _buildingRegistry.Pieces)
            {
                var go  = Instantiate(_rowPrefab, _rowContainer);
                var row = go.GetComponent<BuildingMenuPieceRowUI>();
                row?.Init(def);
            }
        }
    }
}
