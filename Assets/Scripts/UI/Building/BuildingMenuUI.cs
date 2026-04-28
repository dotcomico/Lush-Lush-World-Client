using LushWorld.Building;
using LushWorld.Inventory;
using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI.Building
{
    // Nested inside PlayerRig.prefab as BuildingMenuUI.prefab.
    // Subscribes to BuildingSystem static events — no direct scene references needed.
    // Follows the same open/close + backdrop + cursor pattern as CraftingUI.
    public class BuildingMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject       _panel;
        [SerializeField] private Transform        _rowContainer;
        [SerializeField] private GameObject       _rowPrefab;
        [SerializeField] private BuildingRegistry _buildingRegistry;

        private StarterAssetsInputs _inputBridge;
        private Canvas              _canvas;
        private GameObject          _backdrop;

        private void OnEnable()  => BuildingSystem.OnBuildingMenuToggleRequested += HandleMenuToggle;
        private void OnDisable() => BuildingSystem.OnBuildingMenuToggleRequested -= HandleMenuToggle;

        private void Start()
        {
            _inputBridge = FindFirstObjectByType<StarterAssetsInputs>();
            _canvas      = GetComponent<Canvas>();
            if (_canvas != null) _canvas.sortingOrder = 150;
            if (_panel  != null) _panel.SetActive(false);
            CreateBackdrop();
            BuildRows();
        }

        private void CreateBackdrop()
        {
            _backdrop = new GameObject("BuildingBackdrop", typeof(RectTransform));
            _backdrop.transform.SetParent(transform, false);
            _backdrop.layer = gameObject.layer;

            var rt = _backdrop.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = _backdrop.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.004f);
            img.raycastTarget = true;

            var btn = _backdrop.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => BuildingSystem.LocalPlayer?.CloseBuildingMenu());

            if (_panel != null)
                _backdrop.transform.SetSiblingIndex(_panel.transform.GetSiblingIndex());

            _backdrop.SetActive(false);
        }

        private void HandleMenuToggle(bool open)
        {
            if (_panel    != null) _panel.SetActive(open);
            if (_backdrop != null) _backdrop.SetActive(open);

            if (open)
            {
                if (_inputBridge != null)
                {
                    _inputBridge.cursorLocked       = false;
                    _inputBridge.cursorInputForLook = false;
                    _inputBridge.look               = Vector2.zero;
                    _inputBridge.move               = Vector2.zero;
                }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else
            {
                if (_inputBridge != null)
                {
                    _inputBridge.cursorLocked       = true;
                    _inputBridge.cursorInputForLook = true;
                }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

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
