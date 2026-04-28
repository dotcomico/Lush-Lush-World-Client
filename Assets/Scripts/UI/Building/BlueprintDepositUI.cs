using System.Collections.Generic;
using LushWorld.Building;
using LushWorld.Inventory;
using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI.Building
{
    // Nested inside PlayerRig.prefab as BlueprintDepositUI.prefab.
    // Subscribes to BlueprintInstance static events — no direct scene references needed.
    // Follows the same open/close + backdrop + cursor pattern as CraftingUI and BuildingMenuUI.
    public class BlueprintDepositUI : MonoBehaviour
    {
        [SerializeField] private GameObject   _panel;
        [SerializeField] private Transform    _rowContainer;
        [SerializeField] private GameObject   _rowPrefab;
        [SerializeField] private ItemRegistry _itemRegistry;

        private BlueprintInstance   _current;
        private StarterAssetsInputs _inputBridge;
        private Canvas              _canvas;
        private Transform           _playerTransform;
        private GameObject          _backdrop;

        private readonly List<BlueprintDepositRowUI> _rows = new();
        private const float CloseDistance = 3.5f;

        private void OnEnable()
        {
            BlueprintInstance.OnInteracted += HandleInteracted;
            BlueprintInstance.OnCompleted  += HandleCompleted;
        }

        private void OnDisable()
        {
            BlueprintInstance.OnInteracted -= HandleInteracted;
            BlueprintInstance.OnCompleted  -= HandleCompleted;
        }

        private void Start()
        {
            _inputBridge = FindFirstObjectByType<StarterAssetsInputs>();
            _canvas      = GetComponent<Canvas>();
            if (_canvas != null) _canvas.sortingOrder = 150;
            if (_panel  != null) _panel.SetActive(false);
            CreateBackdrop();
        }

        private void Update()
        {
            if (_current == null || _panel == null || !_panel.activeSelf) return;
            if (_playerTransform == null) return;
            if (Vector3.Distance(_playerTransform.position, _current.transform.position) > CloseDistance)
                Close();
        }

        private void CreateBackdrop()
        {
            _backdrop = new GameObject("DepositBackdrop", typeof(RectTransform));
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
            btn.onClick.AddListener(Close);

            if (_panel != null)
                _backdrop.transform.SetSiblingIndex(_panel.transform.GetSiblingIndex());

            _backdrop.SetActive(false);
        }

        private void HandleInteracted(BlueprintInstance blueprint)
        {
            _current         = blueprint;
            _playerTransform = InventorySystem.LocalPlayer?.transform;
            Open();
        }

        private void HandleCompleted(BlueprintInstance blueprint)
        {
            if (_current == blueprint) Close();
        }

        private void Open()
        {
            if (_panel    != null) _panel.SetActive(true);
            if (_backdrop != null) _backdrop.SetActive(true);

            if (_inputBridge != null)
            {
                _inputBridge.cursorLocked       = false;
                _inputBridge.cursorInputForLook = false;
                _inputBridge.look               = Vector2.zero;
                _inputBridge.move               = Vector2.zero;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            RebuildRows();
        }

        private void Close()
        {
            _current = null;
            if (_panel    != null) _panel.SetActive(false);
            if (_backdrop != null) _backdrop.SetActive(false);

            if (_inputBridge != null)
            {
                _inputBridge.cursorLocked       = true;
                _inputBridge.cursorInputForLook = true;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            ClearRows();
        }

        private void RebuildRows()
        {
            ClearRows();
            if (_current == null || _rowPrefab == null || _rowContainer == null) return;

            foreach (var ing in _current.Def.Cost)
            {
                string displayName = ing.ItemId;
                if (_itemRegistry != null && _itemRegistry.TryGetById(ing.ItemId, out var def))
                    displayName = def.DisplayName;

                var go  = Instantiate(_rowPrefab, _rowContainer);
                var row = go.GetComponent<BlueprintDepositRowUI>();
                if (row == null) continue;
                row.Init(_current, ing.ItemId, ing.Quantity, displayName);
                _rows.Add(row);
            }
        }

        private void ClearRows()
        {
            _rows.Clear();
            if (_rowContainer == null) return;
            for (int i = _rowContainer.childCount - 1; i >= 0; i--)
                Destroy(_rowContainer.GetChild(i).gameObject);
        }
    }
}
