# Inventory Drop System

## Overview

Allows the player to drop items from the inventory in two ways:

| Input | Behaviour |
|-------|-----------|
| **Q key** | Drops **1 item** from the selected hotbar slot. The rest of the stack stays. |
| **Drag slot → game viewport** | Drops the **full stack** as a single world object. If qty > 1, a floating label shows the count. |

The item spawns at the ground point under the cursor, clamped to a max radius from the player, falls with gravity, and is picked back up with E.

---

## Architecture

### Drop Flow

**Q key path:**
```
InventoryInputHandler.Update (Q pressed)
  → InventorySystem.RequestDropActiveItem()
  → DropFromSlot(selectedHotbarSlot, isHotbar:true, quantity:1)
```

**Drag-to-world path:**
```
WorldDropZone.OnDrop (IDropHandler on full-screen background panel)
  → InventoryDragController.TryConsumeDrag()
  → InventorySystem.RequestDropSlotItem(srcIndex, srcIsHotbar)
  → DropFromSlot(srcIndex, srcIsHotbar, quantity:stack.Quantity)
```

Both paths converge in the private `DropFromSlot(int slotIndex, bool isHotbar, int quantity)` method.

### Why WorldDropZone, not OnEndDrag

`OnEndDrag` + `IsPointerOverGameObject()` is unreliable with Unity's new Input System (pointer events don't always fire over the game viewport). `WorldDropZone` is a full-stretch `Image` (alpha 0, Raycast Target ON) at sibling index 0 inside the `InventoryUI` canvas root. Unity's EventSystem routes `OnDrop` there whenever the drag ends outside any slot, making it the single reliable entry point.

### Spawn Position & Radius Clamp

`GetDropSpawnPosition()` raycasts from the camera through `Mouse.current.position` (new Input System) to the ground. `ClampDropPoint()` then enforces a max XZ distance of `_dropMaxDistance` (default 2.5 m) from the player, preventing items from spawning on distant terrain when the cursor is far away.

### Stack Quantity Label

When dropping qty > 1 via drag, `SpawnQuantityLabel()` creates a child `GameObject` with `TextMeshPro` (bold white, black outline) and `ItemDropLabel` (billboards toward camera each `LateUpdate`). Q key drops always spawn qty = 1, so they never show a label.

---

## Components Required on World Prefabs

| Component | Purpose |
|-----------|---------|
| `ResourceNode` | itemId + pickup logic |
| `Collider` (SphereCollider) | Physics + pickup trigger |
| **No Rigidbody on the prefab** | Added dynamically by `DropFromSlot` on the clone only |

`DropFromSlot` sets `MeshCollider.convex = true` on all child MeshColliders before adding the Rigidbody (Unity disallows concave MeshColliders on dynamic Rigidbodies).

### Rigidbody values applied in code

| Property | Value |
|----------|-------|
| Mass | 1 |
| Linear Damping | 0.5 |
| Angular Damping | `_dropAngularDrag` (Inspector, default 5) |
| Throw impulse | `(forward + up*0.2).normalized * 2` |

---

## Player Push

`BasicRigidBodyPush` (StarterAssets) uses `OnControllerColliderHit` to push any Rigidbody the CharacterController walks into.

**Add to PlayerCapsule (one-time Inspector setup):**

| Field | Value |
|-------|-------|
| Can Push | ✅ |
| Push Layers | Default |
| Strength | 2.5 |

---

## Inspector-Tunable Settings (InventorySystem on PlayerCapsule)

| Field | Default | Effect |
|-------|---------|--------|
| Drop Max Distance | 2.5 m | XZ clamp radius for spawn position |
| Drop Angular Drag | 5 | Higher = items stop rolling sooner |

---

## Key Files

| File | Role |
|------|------|
| `Assets/Scripts/Inventory/InventorySystem.cs` | Core drop logic: `RequestDropActiveItem`, `RequestDropSlotItem`, `DropFromSlot`, `GetDropSpawnPosition`, `ClampDropPoint`, `SpawnQuantityLabel` |
| `Assets/Scripts/World/ItemDropLabel.cs` | Billboard MonoBehaviour for the quantity label |
| `Assets/Scripts/UI/Inventory/WorldDropZone.cs` | Full-screen IDropHandler — drag-to-world entry point |
| `Assets/Scripts/UI/Inventory/InventoryDragController.cs` | Drag state + `TryConsumeDrag()` |
| `Assets/Scripts/Inventory/InventoryInputHandler.cs` | Q key → `RequestDropActiveItem()` |
| `Assets/Scripts/Resource/ResourceNode.cs` | `SetQuantity(int)` stamps qty on spawned clone |
| `Assets/StarterAssets/…/BasicRigidBodyPush.cs` | Player push — enable on PlayerCapsule |

---

## Adding a New Droppable Item

1. Create world prefab (see `adding-pickup-items.md`) — **no Rigidbody**
2. Assign `ItemDefinition.WorldPrefab` **from the Project window** (Hierarchy drag stores a Component ref, which breaks `Instantiate`)
3. Set `ItemDefinition.IsDroppable = true` (default)
4. Done — everything else is automatic

## One-Time Inspector Setup

| What | Where | Value |
|------|-------|-------|
| `InventorySystem._itemRegistry` | PlayerCapsule → InventorySystem | `Assets/App/Items/ItemRegistry.asset` |
| `ItemDefinition.WorldPrefab` (each SO) | Each asset in `Assets/App/Items/` | Project window drag |
| `BasicRigidBodyPush` | PlayerCapsule | Can Push ✅, Layers = Default, Strength = 2.5 |
| `WorldDropZone` | InventoryUI prefab child (full-stretch, alpha 0, Raycast ON, index 0) | Add `WorldDropZone` component |

---

## Testing

1. Pick up a stack of 5 rocks → press **Q** five times → each press drops 1, slot empties after 5
2. Pick up 5 rocks → drag slot to game viewport → single mesh drops with **"5"** label floating above
3. Drop a qty-1 item by drag → no label
4. Walk into a dropped item → it slides away
5. Press E near any dropped item → correct quantity restored to inventory
