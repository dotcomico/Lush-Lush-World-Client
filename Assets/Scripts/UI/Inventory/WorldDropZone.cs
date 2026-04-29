using LushWorld.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LushWorld.UI.Inventory
{
    // Full-screen invisible panel — sibling index 0 inside InventoryUI canvas, behind every other panel.
    // raycastTarget is OFF by default; enabled only while a drag is in flight so this panel never
    // blocks the joystick canvas (which sits at a lower Canvas sortOrder) during normal gameplay.
    public class WorldDropZone : MonoBehaviour, IDropHandler
    {
        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _image.raycastTarget = false;
        }

        private void OnEnable()
        {
            InventoryDragController.OnDragBegan += EnableRaycast;
            InventoryDragController.OnDragEnded += DisableRaycast;
        }

        private void OnDisable()
        {
            InventoryDragController.OnDragBegan -= EnableRaycast;
            InventoryDragController.OnDragEnded -= DisableRaycast;
        }

        private void EnableRaycast() => _image.raycastTarget = true;
        private void DisableRaycast() => _image.raycastTarget = false;

        public void OnDrop(PointerEventData eventData)
        {
            if (!InventoryDragController.Instance.TryConsumeDrag(out int srcIndex, out bool srcIsHotbar)) return;
            InventorySystem.LocalPlayer?.RequestDropSlotItem(srcIndex, srcIsHotbar);
        }
    }
}
