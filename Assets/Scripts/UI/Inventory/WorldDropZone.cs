using LushWorld.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LushWorld.UI.Inventory
{
    // Full-screen invisible panel — sibling index 0 inside InventoryUI canvas, behind every other panel.
    // Unity's event routing sends OnDrop here only when the drag is released outside any slot,
    // so this is the single, reliable entry point for "drag item to world" drops.
    //
    // Setup: add this component to a full-stretch Image (alpha 0, Raycast Target ON) as the
    // first child of the InventoryUI canvas root.
    public class WorldDropZone : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            if (!InventoryDragController.Instance.TryConsumeDrag(out int srcIndex, out bool srcIsHotbar)) return;
            InventorySystem.LocalPlayer?.RequestDropSlotItem(srcIndex, srcIsHotbar);
        }
    }
}
