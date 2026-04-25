# Terrain Tree Pickup System — Known Issues & Rules

## How the System Works

Rocks (and other pickup items) are placed as **Terrain Trees** using the Paint Trees brush.
At runtime, `TerrainResourceManager` reads their positions, and:

- **Far away** → rock stays as a Unity terrain tree (billboard / mesh rendered by Unity)
- **Within `spawnRadius` (default 25u)** → terrain tree is removed, interactive prefab is spawned
- **Past `despawnRadius` (default 30u)** → prefab is destroyed, terrain tree is re-added
- **Picked up** → rock is permanently removed from both systems

The terrain asset on disk is **never modified** — all changes are runtime-only and restore on Play exit.

---

## Rule: Always Paint Trees at Width = 1.0 or Higher

### The Bug We Hit (April 2026)

Rocks were painted with the Terrain Tree Brush at **Width ≈ 0.44, Height ≈ 0.43**.

**Result:**
- The terrain tree instance has `widthScale = 0.44` — less than half the mesh size
- Unity's terrain renderer drops sub-scale billboards at any meaningful distance → **rocks invisible from far away**
- When player gets close (within 25u), the prefab spawns. Our code clamps scale to `Mathf.Max(widthScale, 1.0)` → **prefab appears at full size**
- Player sees: nothing at distance, rock appears out of thin air when close

**Diagnosis:** Console will print:
```
[TerrainResourceManager] Rock at (x,y,z) has widthScale=0.44 (<1). Rocks will be invisible.
```

### The Fix

1. Select the **Terrain** in the Hierarchy
2. Inspector → **Paint Trees** (tree icon, 4th toolbar button)
3. Click the rock prototype to select it
4. Set **Width = 1** and **Height = 1**
5. Erase existing rocks (Shift + brush) and repaint

> If you want visually smaller rocks, **do not use the terrain brush Width slider below 1**.
> Instead, scale down the mesh inside the prefab itself, or use a smaller source mesh.
> The brush Width controls the terrain LOD size — not just visual scale.

---

## Inspector Settings to Check (Terrain Component)

Select the Terrain → Inspector → **Terrain Settings** tab → **Tree & Detail Objects**:

| Setting | Recommended | What it does |
|---|---|---|
| Tree Distance | 500–2000 | Max distance Unity renders terrain trees. If too low, trees vanish before swap range. Must be > `despawnRadius`. |
| Billboard Start | 50–100 | Distance where 3D mesh switches to flat billboard |

---

## TerrainResourceManager Inspector Fields

| Field | Default | Meaning |
|---|---|---|
| Spawn Radius | 25 | Player must be within this distance for prefab to spawn |
| Despawn Radius | 30 | Prefab is destroyed and tree re-added when player moves past this |
| Resource Prototypes → Tree Prefab | — | The exact prefab used in Terrain → Paint Trees for this item type |
| Resource Prototypes → Interactive Prefab | — | The pickup prefab to spawn near the player |

The prototype index is **resolved automatically at runtime** by comparing `Tree Prefab` against `terrainData.treePrototypes`. No manual index counting needed — reordering Paint Trees entries won't break anything.

---

## Startup Diagnostics (Console)

On Play, the system logs:

```
[TerrainResourceManager] Found N resource spots on terrain.
```

- **N = 0** → No `Tree Prefab` in Resource Prototypes matched any prototype in Paint Trees. Make sure the `Tree Prefab` field is the exact same prefab dragged into Paint Trees — not a variant or a copy.
- **treeDistance warning** → Terrain's Tree Distance is too small. Fix in Terrain Settings.
- **widthScale < 1 warning** → Rocks were painted too small. Repaint at Width = 1.
- **RemoveTreeInstance warning** → Float position mismatch. Report to developer.
