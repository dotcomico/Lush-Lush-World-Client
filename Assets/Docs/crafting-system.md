# Crafting System

## Overview

Turn-based crafting: the player opens a menu (G), selects a recipe, and clicks Craft.
Ingredients are consumed from the inventory; the output item is added directly.
The system is NGO-ready — all state mutations go through `Request*()` methods that will become `[ServerRpc]` in Phase 2.

---

## Key Binding

| Key | Action |
|-----|--------|
| **G** | Toggle crafting menu open/close |
| Click outside panel | Close menu (backdrop button) |

---

## Architecture

```
InventoryInputHandler.Update
  gKey pressed → CraftingSystem.LocalPlayer.ToggleCraftingMenu()
    → fires OnCraftingMenuToggleRequested(bool)
      → CraftingUI.HandleMenuToggle(bool)          ← shows/hides panel + cursor
      → (backdrop Button) → CraftingSystem.CloseCraftingMenu()
```

### CraftingSystem (on PlayerCapsule)
- Static `LocalPlayer` ref — UI finds it without scene references
- `ToggleCraftingMenu()` / `CloseCraftingMenu()` — fire `OnCraftingMenuToggleRequested`
- `RequestCraft(recipeId, qty)` — validates ingredients → `ConsumeIngredients` → `InventorySystem.GiveItem`
- `GetMaxCraftable(recipe)` / `CountItem(itemId)` — public helpers used by the row UI
- Static events: `OnCraftSuccess(string)`, `OnCraftFailed(string)`, `OnCraftingMenuToggleRequested(bool)`

### RecipeDefinition (ScriptableObject)
Fields: `RecipeId`, `DisplayName`, `Category` (enum), `Ingredients` (List<Ingredient>), `OutputItemId`, `OutputQuantity`
Nested struct `Ingredient { string ItemId; int Quantity }`

### RecipeRegistry (ScriptableObject)
`List<RecipeDefinition>` + dictionary cache via `TryGetRecipe(id, out recipe)`
Assigned to both `CraftingSystem._recipeRegistry` and `CraftingUI._recipeRegistry` in Inspector.

### CraftingUI (on CraftingMenuUI Canvas root)
- Subscribes to `CraftingSystem` and `InventorySystem` static events
- `Start()`: raises Canvas `sortingOrder = 150` (above HUD), spawns transparent backdrop Button
- `HandleMenuToggle(open)`: shows/hides Panel + backdrop; on open → unlocks cursor + zeros `StarterAssetsInputs.look/move`; on close → re-locks cursor
- `BuildRows()`: instantiates one `CraftingRowPrefab` per recipe into `Content` transform
- `RefreshAllRows()`: called on craft result or inventory slot change

### CraftingRecipeRowUI (on CraftingRowPrefab)
- `Init(recipe, itemRegistry)` — sets recipe name, wires Craft button
- `Refresh()` — updates ingredient counts (have/need) + max craftable + button interactable state
- Calls `CraftingSystem.LocalPlayer.RequestCraft(recipeId, 1)` on Craft click

---

## Prefab Layout

```
PlayerRig.prefab
└── CraftingMenuUI          ← Canvas, Screen Space Overlay
    │   CanvasScaler         Scale With Screen Size, 1920×1080, match=0.5
    │   CraftingUI           sortingOrder=150 at runtime
    └── Panel               anchors (0.1,0.175)→(0.9,0.825) — 80%×65% centered
        └── Content         top-stretch anchor, VerticalLayoutGroup (padding 16, spacing 8)
            └── [rows]      CraftingRowPrefab instances (runtime)

CraftingRowPrefab.prefab
    HorizontalLayoutGroup (childControlWidth+Height=true, spacing=12)
    CraftingRecipeRowUI
    ├── NameText            TMP, auto-size 14–28pt
    ├── IngredientsText     TMP, auto-size 14–28pt
    ├── MaxCraftableText    TMP, auto-size 14–28pt
    └── CraftButton         Button + TMP label, auto-size 12–22pt
```

---

## Data Assets

| Asset | Path |
|-------|------|
| RecipeRegistry | `Assets/App/Crafting/RecipeRegistry.asset` |
| SimpleTorchRecipe | `Assets/App/Crafting/SimpleTorchRecipe.asset` |

---

## Adding a New Recipe

1. Right-click → **Create → Lush World → Recipe Definition** → fill fields
2. Open `RecipeRegistry.asset` → add to Recipes list
3. Done — CraftingUI reads the registry at runtime

---

## Key Files

| File | Role |
|------|------|
| `Assets/Scripts/Crafting/CraftingSystem.cs` | Core logic, static events |
| `Assets/Scripts/Crafting/RecipeDefinition.cs` | SO: one recipe |
| `Assets/Scripts/Crafting/RecipeRegistry.cs` | SO: all recipes + lookup |
| `Assets/Scripts/UI/Crafting/CraftingUI.cs` | Panel show/hide, cursor, backdrop, row spawning |
| `Assets/Scripts/UI/Crafting/CraftingRecipeRowUI.cs` | Per-recipe row display + craft button |
| `Assets/Scripts/Inventory/InventoryInputHandler.cs` | G key input |
| `Assets/App/Prefabs/CraftingMenuUI.prefab` | Canvas prefab nested in PlayerRig |
| `Assets/App/Prefabs/UI/CraftingRowPrefab.prefab` | Row prefab |

---

## Next: Building System

Planned features (not yet implemented):
- Placeable structure blueprints (snap-to-grid or free-place)
- `BlueprintInstance` implementing `IInteractable` — player approaches, presses E to confirm placement
- Build recipes: same `RecipeDefinition` format, Category = `Building`
- Structure prefabs stored in `Assets/App/Prefabs/Structures/`
