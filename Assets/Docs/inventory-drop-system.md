# Inventory Drop System

## Overview

Allows the player to drop items from the inventory by dragging a slot icon outside the inventory UI panel. The item spawns as a world object at the ground position under the cursor and falls with gravity. The player can walk up to it and press E to pick it back up.

---

## Architecture

### How Drag-to-Drop Detection Works

Unity's EventSystem fires events in this order during a drag:

1. `OnDrop()` fires on the **target slot** (only if drag was released over a valid slot)
2. `OnEndDrag()` fires on the **source slot** always

When released on a valid slot:
- `OnDrop()` → `InventoryDragController.TryConsumeDrag()` → `IsDragging = false`
- `OnEndDrag()` sees `IsDragging == false` → successful swap, nothing more to do

When released **outside the inventory UI**:
- No `OnDrop()` fires
- `OnEndDrag()` sees `IsDragging == true` → drag was NOT consumed
- `EventSystem.current.IsPointerOverGameObject()` returns `false` → pointer is over game viewport
- This is the world-drop trigger → call `RequestDropSlotItem()`

### Spawn Position

`InventorySystem.GetDropSpawnPosition()` raycasts from the camera through `Input.mousePosition` to find the exact ground point under the cursor. Falls back to `player.position + player.forward * 1.5f + Vector3.up * 0.5f` if the raycast misses.

### Quantity Preservation

`ItemDefinition.WorldPrefab` has `ResourceNode.quantity = 1` baked in. After instantiation, `ResourceNode.SetQuantity(stack.Quantity)` overrides it — so dropping a stack of 5 rocks and picking it back up gives 5, not 1.

---

## Components Required on World Prefabs

Every droppable item's `WorldPrefab` must have:

| Component | Purpose |
|-----------|---------|
| `ResourceNode` | itemId + pickup logic (already present on all world prefabs) |
| `Collider` (SphereCollider) | Physics contact + pickup trigger (already present) |
| `Rigidbody` | Gravity and tumble on drop — **must be added manually** |

### Rigidbody Settings (Standard for all droppable items)

| Field | Value |
|-------|-------|
| Mass | 1 |
| Drag | 0.3 |
| Angular Drag | 0.05 |
| Use Gravity | checked |
| Is Kinematic | unchecked |
| Collision Detection | Discrete |

**Prefabs that need Rigidbody added:**
- `Assets/App/Prefabs/Rocks/World_Small_Rock_HardRock_13 Variant.prefab`
- `Assets/App/Prefabs/Mushrooms/World_Mushroom_PT_Caesars_Mushroom_01 Variant.prefab`
- `Assets/App/Prefabs/Sticks/World_Branch_01 Variant.prefab`

How to add: open the prefab → select root GameObject → Add Component → Physics → Rigidbody → set values above → Ctrl+S.

---

## Files to Modify

| File | Change |
|------|--------|
| `Assets/Scripts/Resource/ResourceNode.cs` | Add `public void SetQuantity(int qty) => quantity = qty;` |
| `Assets/Scripts/Inventory/InventorySystem.cs` | Add `RequestDropSlotItem(int slotIndex, bool isHotbar)` and `GetDropSpawnPosition()` |
| `Assets/Scripts/UI/Inventory/InventorySlotUI.cs` | Modify `OnEndDrag()` to detect world drop and call `RequestDropSlotItem` |

---

## Key Code to Add

### ResourceNode.cs — new method

```csharp
public void SetQuantity(int qty) => quantity = qty;
```

### InventorySystem.cs — new methods

```csharp
public void RequestDropSlotItem(int slotIndex, bool isHotbar)
{
    ItemStack stack = Data.GetSlot(slotIndex, isHotbar);
    if (stack.IsEmpty) return;
    if (!_itemRegistry.TryGetById(stack.ItemId, out ItemDefinition def)) return;
    if (!def.IsDroppable || def.WorldPrefab == null) return;

    Data.TryRemoveItem(slotIndex, isHotbar, stack.Quantity);

    Vector3 spawnPos = GetDropSpawnPosition();
    GameObject dropped = Object.Instantiate(def.WorldPrefab, spawnPos, Random.rotation);

    if (dropped.TryGetComponent(out ResourceNode node))
        node.SetQuantity(stack.Quantity);

    if (dropped.TryGetComponent(out Rigidbody rb))
    {
        Vector3 throwDir = transform.forward + Vector3.up * 0.5f;
        rb.AddForce(throwDir.normalized * 3f, ForceMode.Impulse);
    }
}

private Vector3 GetDropSpawnPosition()
{
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out RaycastHit hit, 20f))
        return hit.point + Vector3.up * 0.3f;

    return transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
}
```

Note: verify `_itemRegistry` is already a `[SerializeField] private ItemRegistry` on `InventorySystem`. If not, add it and wire it in the PlayerCapsule prefab Inspector.

### InventorySlotUI.cs — modified OnEndDrag

Replace the current `OnEndDrag` body with:

```csharp
public void OnEndDrag(PointerEventData eventData)
{
    bool dragWasConsumed = !InventoryDragController.Instance.IsDragging;

    InventoryDragController.Instance.EndDrag();
    _iconImage.color = new Color(1, 1, 1, 1); // restore ghost opacity

    if (!dragWasConsumed && !EventSystem.current.IsPointerOverGameObject())
        InventorySystem.LocalPlayer.RequestDropSlotItem(_slotIndex, _isHotbarSlot);
}
```

Confirm `_slotIndex` and `_isHotbarSlot` match the actual private field names in `Initialize()`.

---

## Adding a New Droppable Item

1. Create the world prefab (see `adding-pickup-items.md`)
2. Add a Rigidbody with the standard settings from this doc
3. Set `ItemDefinition.WorldPrefab` to that prefab
4. Set `ItemDefinition.IsDroppable = true`
5. Done — the drop system handles it automatically

---

## Testing

1. Enter Play mode
2. Pick up any item (walk near it, press E)
3. Drag the item's slot icon outside the inventory panel into the game viewport
4. **Expected:** item disappears from slot; world object spawns at ground under cursor and falls with gravity
5. Walk up to the dropped item and press E
6. **Expected:** item returns to inventory with correct quantity
7. Test with a stack (qty > 1) — confirm full stack is recovered, not just 1
