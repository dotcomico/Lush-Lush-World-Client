using LushWorld.Inventory;
using UnityEngine;

namespace LushWorld.Player
{
    // Renders the selected hotbar item as a 3D model attached to the camera (Minecraft-style).
    // HeldItemAnchor must be a child of MainCamera inside PlayerRig.prefab.
    // Position the anchor in the Inspector to tune where the item appears (e.g. bottom-right).
    public class HeldItemView : MonoBehaviour
    {
        [SerializeField] private ItemRegistry _itemRegistry;
        [SerializeField] private Transform _heldItemAnchor;

        private InventoryData _data;
        private GameObject _currentHeldObject;

        private void OnEnable()
        {
            InventorySystem.OnInventoryReady += HandleInventoryReady;
            InventorySystem.OnInventoryDestroyed += HandleInventoryDestroyed;
        }

        private void Start()
        {
            if (_data == null && InventorySystem.LocalPlayer != null)
                HandleInventoryReady(InventorySystem.LocalPlayer.Data);
        }

        private void OnDisable()
        {
            InventorySystem.OnInventoryReady -= HandleInventoryReady;
            InventorySystem.OnInventoryDestroyed -= HandleInventoryDestroyed;
            UnsubscribeData();
        }

        private void HandleInventoryReady(InventoryData data)
        {
            UnsubscribeData();
            _data = data;
            _data.OnSelectedHotbarSlotChanged += OnSelectionChanged;
            _data.OnHotbarSlotChanged += OnSlotChanged;
            RefreshHeldItem();
        }

        private void HandleInventoryDestroyed()
        {
            UnsubscribeData();
            _data = null;
            ClearHeldItem();
        }

        private void OnSelectionChanged(int _) => RefreshHeldItem();

        private void OnSlotChanged(int changedSlot, ItemStack _)
        {
            if (_data != null && changedSlot == _data.SelectedHotbarSlot)
                RefreshHeldItem();
        }

        private void RefreshHeldItem()
        {
            ClearHeldItem();
            if (_data == null || _heldItemAnchor == null) return;

            var activeStack = _data.ActiveItem;
            if (activeStack.IsEmpty) return;

            var def = _itemRegistry != null ? _itemRegistry.GetById(activeStack.ItemId) : null;
            if (def?.WorldPrefab == null) return;

            _currentHeldObject = Instantiate(def.WorldPrefab, _heldItemAnchor);
            _currentHeldObject.transform.localPosition = Vector3.zero;
            _currentHeldObject.transform.localRotation = Quaternion.identity;
            _currentHeldObject.transform.localScale = Vector3.one * def.HeldScale;

            // Held visuals must not block gameplay — disable physics on the clone
            foreach (var col in _currentHeldObject.GetComponentsInChildren<Collider>(true))
                col.enabled = false;
            foreach (var rb in _currentHeldObject.GetComponentsInChildren<Rigidbody>(true))
                rb.isKinematic = true;
        }

        private void ClearHeldItem()
        {
            if (_currentHeldObject == null) return;
            Destroy(_currentHeldObject);
            _currentHeldObject = null;
        }

        private void UnsubscribeData()
        {
            if (_data == null) return;
            _data.OnSelectedHotbarSlotChanged -= OnSelectionChanged;
            _data.OnHotbarSlotChanged -= OnSlotChanged;
        }
    }
}
