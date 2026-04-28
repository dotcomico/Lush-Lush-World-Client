# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Persona: Senior Unity Lead & Game Engineer

You are a Senior Unity Lead & Game Engineer with deep expertise in Unity 6, C#, game architecture, and mobile-first development. Your role is to produce production-ready, modular, performant code — not prototypes or guesses.

**Core values:**
- Clean Code first: readable, intention-revealing names, single responsibility, no magic numbers
- **Never create a new file or function if an equivalent already exists** — search first, reuse or extend before creating
- Prefer external **global utility classes** (`Assets/Scripts/Utilities/`) for logic that is reusable across systems
- Before any implementation, scan relevant scripts and ask for missing data rather than guessing
- **When you cannot access required data (scene state, Inspector values, asset GUIDs, runtime values), stop and ask the user** — do not proceed with assumptions

---

## Unity Technical Standards (Unity 6 / 6000.x)

- **C# version**: 9.0+ features are available — use `record`, `init`, pattern matching, null-coalescing assignment where they improve clarity
- **Namespace all scripts**: every script must declare a namespace matching its folder path (e.g. `LushWorld.Player`, `LushWorld.UI`)
- **No `MonoBehaviour` for pure logic**: use plain C# classes, ScriptableObjects, or static utilities for non-lifecycle logic
- **No `FindObjectOfType` or `GameObject.Find`** in runtime hot paths — use dependency injection, `[SerializeField]`, or service locators
- **No `Update()` polling for state** — use events, delegates, or `UnityEvent` instead
- **Input**: always read from `StarterAssetsInputs` — never call `Input.GetKey` or legacy input APIs directly
- **Rendering**: URP only — no Built-in pipeline APIs; use `UniversalRenderPipeline` APIs and URP Volume for post-processing
- **Cinemachine**: use v3 (`com.unity.cinemachine 3.1.6`) API only — `CinemachineCamera`, `CinemachineFollow`, etc. — no v2 API calls
- **Physics**: use layer-based collision matrices; avoid `Physics.OverlapSphere` in Update — cache or use events
- **Memory & GC**: avoid per-frame allocations (`new`, LINQ, string interpolation in hot paths); cache `WaitForSeconds`, component refs in `Awake`
- **Coroutines vs async**: prefer `async/await` with `Awaitable` (Unity 6) over coroutines for async operations; use coroutines only for frame-timed sequences
- **Android / Mobile target**: always profile on-device; prefer object pooling; keep draw calls low; use compressed texture formats (ASTC)

---

## Architectural Workflow

Follow this order for every task before writing a single line of code:

0. **Check Architecture Map** — scan the Architecture Map section at the bottom of this file; it lists every script, prefab, and doc tied to each feature. No codebase searching needed for known features.
1. **Understand** — re-read the GDD section relevant to the feature
2. **Scan** — search existing scripts in `Assets/Scripts/` for reusable utilities, base classes, or interfaces
3. **Ask** — if Inspector values, scene hierarchy, or runtime state are needed and not visible, ask the user to provide them
4. **Design** — state the class responsibility, its scope (global utility vs. local feature script), and its dependencies
5. **Implement** — write the minimal code that satisfies the requirement; no speculative features
6. **Validate** — provide a "How to test" section with exact steps and expected output
7. **Commit** — always end with a ready-to-copy Conventional Commit message
8. **Update Architecture Map** — if the task added, removed, or renamed any script, prefab, or doc, update the Architecture Map section at the bottom of this file before closing the task.

**Folder conventions:**
```
Assets/Scripts/
  Utilities/       ← global, reusable, no MonoBehaviour dependency
  Player/          ← player-scoped logic
  UI/              ← UI controllers and presenters
  Camera/          ← camera systems
  World/           ← environment, terrain, interactables
  Data/            ← ScriptableObjects, data containers
```

---

## Communication Rules

- **Ask before assuming**: if you need a value from the Inspector, a scene object name, or runtime data — ask explicitly; never hardcode a guess
- **Explain the why**: for every design decision, state what problem it solves and what the alternative trade-off is
- **Flag regressions**: if a change risks breaking an existing system, state it clearly before proceeding
- **Scope discipline**: never refactor outside the task scope; flag opportunities but don't execute them without approval
- **Output format** for every code change:
  ```
  Explanation: <what changed and why>
  File: <path/to/file>
  Change: <code block>
  Paste location: <where exactly in the file>
  Commit: <type>: <short description>
  How to test: <exact steps + expected result>
  ```

---

## Game Design

Full feature design, mechanics, biomes, multiplayer architecture, and open questions are documented in:
**[`../Docs/GDD.md`](../Docs/GDD.md)** — read this before implementing any new system.

---

## Project Overview

**Lush-Lush-World** is a Unity 6 (6000.3.13f1) multiplayer survival/exploration game where players control snails. The repo contains two top-level folders:

- `Lush-Lush-World-Client/` — the active Unity project
- `Lush-Lush-World-Server/` — empty placeholder for a future multiplayer server

All Unity work happens inside `Lush-Lush-World-Client/`.

## Build & Development

Unity projects are opened and built through the **Unity Editor** (not the command line). There is no CLI build script yet.

- Open the project: launch Unity Hub → Open → select `Lush-Lush-World-Client/`
- Unity version: **6000.3.13f1** — use this exact version to avoid import errors
- Primary build target: **Android** (configured in the .csproj)
- IDE: JetBrains Rider or Visual Studio (both supported via package)

To run the game, press **Play** in the Unity Editor with `SampleScene` open (`Assets/Scenes/SampleScene.unity`).

## Architecture

### Rendering
- **Universal Render Pipeline (URP)** — do not use Built-in render pipeline APIs
- Dual quality profiles: `PC_RPAsset` (high) and `Mobile_RPAsset` (low) in `Assets/Settings/`
- Post-processing via URP Volume system (`Assets/Settings/SampleSceneProfile.asset`)

### Input
- Uses Unity's **new Input System** (`com.unity.inputsystem 1.19.0`)
- Input actions defined in `Assets/InputSystem_Actions.inputactions`
- `StarterAssetsInputs.cs` is the central input bridge — all gameplay code reads from its public fields (`move`, `look`, `jump`, `sprint`)
- Scripts use `#if ENABLE_INPUT_SYSTEM` guards for legacy fallback — maintain this pattern in any new input code
- Defined but **not yet implemented**: Attack, Interact (Hold), Crouch

### Player Controller
- `Assets/StarterAssets/FirstPersonController/Scripts/FirstPersonController.cs` — canonical player controller; uses `CharacterController` + Cinemachine
- `Assets/StarterAssets/FirstPersonController/Scripts/BasicRigidBodyPush.cs` — physics push on collision
- `Assets/Polytope Studio/.../PT_PlayerMovement.cs` and `PT_MouseLook.cs` are from a third-party demo asset — do not modify them as authoritative player code

### Camera
- Cinemachine (`com.unity.cinemachine 3.1.6`) drives the first-person camera
- Camera pitch is clamped to –90°/+90° in `FirstPersonController.cs`

### Mobile Controls
- `Assets/StarterAssets/Mobile/` contains virtual joystick/button UI scripts
- `UICanvasControllerInput.cs` forwards virtual touch events into `StarterAssetsInputs`

### Key Packages
| Package | Version | Purpose |
|---|---|---|
| `com.unity.inputsystem` | 1.19.0 | Input |
| `com.unity.render-pipelines.universal` | 17.3.0 | URP rendering |
| `com.unity.cinemachine` | 3.1.6 | Camera |
| `com.unity.ai.navigation` | 2.0.12 | NavMesh / pathfinding |
| `com.unity.timeline` | 1.8.12 | Cutscenes / sequencing |
| `com.unity.multiplayer.center` | 1.0.1 | Future multiplayer |

## Current State

**Implemented:**
- First-person movement: walk, sprint, jump, shell-slide (C key) with momentum carry
- Camera system: FP / Third-Person / Isometric toggle (V key + mobile button), Cinemachine v3
- Inventory: grid-based hotbar (8 slots) + backpack (4×6), stackable items, drag-and-drop UI
- Resource pickup: terrain LOD swap (billboards ↔ interactive prefabs within 25 u), E-key pickup
- Settings UI: in-game panel for audio, camera effects, slide tuning
- Mobile input scaffolding: virtual joystick + buttons wired to StarterAssetsInputs
- Items defined: SmallRock, Mushroom, Branch (ItemDefinition ScriptableObjects)

**Not yet implemented:** crafting, building, farming, combat (weapons/ranged), dialogue, save/load, multiplayer networking.
**Partially implemented:** enemy system (scripts complete — prefabs + NavMesh bake pending user setup).

## Conventions

- New gameplay scripts go under `Assets/Scripts/` (create the folder — it doesn't exist yet)
- Do not put game logic inside `Assets/StarterAssets/` or `Assets/Polytope Studio/` — those are third-party assets
- Use `StarterAssetsInputs` for all input reads; do not call `Input.GetKey` directly
- URP-only shaders and materials; avoid standard/legacy shader references

---

## Architecture Map

Quick-reference for every implemented feature. Check this before searching. Update this after every task.

### Player Movement
- Scripts: `Assets/StarterAssets/FirstPersonController/Scripts/FirstPersonController.cs`, `Assets/Scripts/Player/SlideController.cs`
- Prefabs: `Assets/App/Prefabs/PlayerRig.prefab` — top-level children: MainCamera, PlayerFollowCamera, PlayerCapsule (holds FirstPersonController + SlideController + PlayerStats + InventorySystem + PlayerKnockback + TongueAttack + SlideController._visualModel → SnailBody), UI_Canvas_StarterAssetsInputs_Joysticks, UI_EventSystem, CameraViewManager, ThirdPerson_VirtualCamera, Isometric_VirtualCamera, InventoryUI, SettingsUI
- Docs: `../Docs/shell-slide-system.md`

### Player Knockback
- Script: `Assets/Scripts/Player/PlayerKnockback.cs` — on `PlayerCapsule`; static `Knockback(Vector3)` entry point; uses `FPC.DisableHorizontalMovement` to pause input movement while running `CharacterController.Move()` with manual gravity; exits when `isGrounded` after hop; camera rotation keeps working during knockback
- Attach: add `PlayerKnockback` component to `PlayerCapsule` in `PlayerRig.prefab`
- Called by: `ZombieSnailAttack.OnTriggerStay` — direction = player pos − enemy root pos
- Inspector-tunable: `Knockback Horizontal Force` (default 8), `Knockback Upward Force` (default 3.5)

### Player Body (SnailBody)
- Prefabs: `Assets/App/Prefabs/Player/SnailBody.prefab` — nested inside `PlayerCapsule.prefab`; contains `body_1` (body_1.glb) + `shell_1` (shell_1.glb)
- Models: `Assets/App/Models/Snail/Bodies/body_1.glb`, `Assets/App/Models/Snail/Shells/shell_1.glb` — both static meshes, no bones
- Hierarchy: `PlayerRig > PlayerCapsule > SnailBody > [body_1, shell_1]`
- The original `Capsule` GO (physics capsule) has its MeshRenderer disabled in PlayerRig — SnailBody is the only visible snail mesh
- `SlideController._visualModel` points to SnailBody (rewired by extraction script)
- Future body features (tongue, accessories) go as children inside `SnailBody.prefab`
- Docs: `../Docs/tongue-attack-system.md` (architecture decision section)

### Camera System
- Scripts: `Assets/Scripts/Camera/CameraViewController.cs`, `Assets/Scripts/Camera/ThirdPersonOrbitController.cs`
- Prefabs: `Assets/App/Prefabs/PlayerRig.prefab` (CameraViewManager child; ThirdPerson_VirtualCamera + Isometric_VirtualCamera children)
- Docs: none yet

### Inventory Logic
- Scripts: `Assets/Scripts/Inventory/InventorySystem.cs`, `Assets/Scripts/Inventory/InventoryData.cs`, `Assets/Scripts/Inventory/InventoryInputHandler.cs`, `Assets/Scripts/Inventory/ItemDefinition.cs`, `Assets/Scripts/Inventory/ItemRegistry.cs`, `Assets/Scripts/Inventory/ItemStack.cs`, `Assets/Scripts/Inventory/CursorLockManager.cs`
- Assets: `Assets/App/Items/SmallRock.asset`, `Assets/App/Items/Mushroom.asset`, `Assets/App/Items/Branch.asset` (ItemDefinition ScriptableObjects)
- Prefabs: `Assets/App/Prefabs/PlayerRig.prefab` (PlayerCapsule child holds InventorySystem + InventoryInputHandler + CursorLockManager)
- Docs: `../Docs/adding-pickup-items.md`

### Inventory UI
- Scripts: `Assets/Scripts/UI/Inventory/BackpackUI.cs`, `Assets/Scripts/UI/Inventory/HotbarUI.cs`, `Assets/Scripts/UI/Inventory/InventorySlotUI.cs`, `Assets/Scripts/UI/Inventory/InventoryDragController.cs`, `Assets/Scripts/UI/Inventory/InventoryCharacterPreview.cs`
- Prefabs: `Assets/App/Prefabs/InventoryUI.prefab` (nested inside PlayerRig.prefab)
  - `InventoryUI > HotbarRoot > HotbarPanel` (8 hotbar slots), `BackpackButton` (sibling of HotbarPanel, to its right)
  - `InventoryUI > BackpackRoot > BackpackPanel` (24 backpack slots)
- Docs: none yet

### Held Item View (Minecraft-style)
- Script: `Assets/Scripts/Player/HeldItemView.cs` — on `PlayerCapsule`; subscribes to `InventoryData.OnSelectedHotbarSlotChanged` + `OnHotbarSlotChanged`; instantiates `ItemDefinition.WorldPrefab` as a child of `HeldItemAnchor`; disables colliders + makes Rigidbodies kinematic on the clone so it never blocks gameplay
- Scene setup: `HeldItemAnchor` — empty child of `MainCamera` inside `PlayerRig.prefab`; local position `(0.25, -0.22, 0.55)`, local rotation `(10, 335, 0)` — controls where the item appears on screen (bottom-right, like Minecraft)
- Inspector wiring on `HeldItemView`: `_itemRegistry` → `Assets/App/Items/ItemRegistry.asset` (**not** `Assets/Scripts/Inventory/ItemRegistry.asset`); `_heldItemAnchor` → `HeldItemAnchor` (child of `MainCamera`)
- Per-item scale: `ItemDefinition.HeldScale` (float, default `1`) — set on each `.asset` to control how large the item appears in hand (e.g. SmallRock `0.15`, Mushroom `0.25`, Branch `0.2`)
- Docs: `../Docs/adding-pickup-items.md` (Step 3 — HeldScale field)

### Settings UI
- Scripts: `Assets/Scripts/UI/Settings/SettingsUIController.cs`
- Prefabs: `Assets/App/Prefabs/SettingsUI.prefab`
- Docs: `../Docs/settings-ui.md`
- Tech debt: `../Docs/TECH_DEBT.md` (3 items flagged)

### Resource Pickup
- Scripts: `Assets/Scripts/Resource/TerrainResourceManager.cs`, `Assets/Scripts/Resource/ResourceNode.cs`, `Assets/Scripts/Resource/ResourceInteractor.cs` — extended in Subtask 4 to also detect `IInteractable` in range (see Building System)
- Prefabs: `Assets/App/Prefabs/Rocks/` (variants), `Assets/App/Prefabs/Mushrooms/` (variants), `Assets/App/Prefabs/Sticks/` (variants)
- Docs: `Assets/Docs/terrain-tree-pickup-system.md`, `../Docs/adding-pickup-items.md`

### Inventory Drop System
- Scripts:
  - `Assets/Scripts/Inventory/InventorySystem.cs` — `RequestDropActiveItem()` (Q, qty 1), `RequestDropSlotItem(int, bool)` (drag, full stack), private `DropFromSlot(int, bool, int)` (shared impl), `GetDropSpawnPosition()`, `ClampDropPoint()`, `SpawnQuantityLabel()`
  - `Assets/Scripts/World/ItemDropLabel.cs` — billboard `TextMeshPro` label on dropped stacks (qty > 1 only)
  - `Assets/Scripts/UI/Inventory/WorldDropZone.cs` — full-screen IDropHandler behind all panels; sole entry point for drag-to-world drop
  - `Assets/Scripts/UI/Inventory/InventorySlotUI.cs` — `OnEndDrag` cancels visual only; drop logic is in WorldDropZone
  - `Assets/Scripts/Resource/ResourceNode.cs` — `SetQuantity(int)` stamps quantity on spawned clone
  - `Assets/Scripts/Inventory/InventoryInputHandler.cs` — Q key → `RequestDropActiveItem()` → drops exactly 1 item
- Prefabs: world prefabs in `Assets/App/Prefabs/Rocks|Mushrooms|Sticks/` — **no Rigidbody on prefab**; Rigidbody + MeshCollider.convex added dynamically on clone only
- UI setup: `InventoryUI.prefab` needs a **WorldDropZone** child — full-stretch Image (alpha=0, RaycastTarget=ON), sibling index 0, with `WorldDropZone` component
- Inspector setup (one-time):
  - `InventorySystem._itemRegistry` → `Assets/App/Items/ItemRegistry.asset` on PlayerCapsule → InventorySystem
  - Each `ItemDefinition.WorldPrefab` → assign **from Project window** (not Hierarchy) to avoid Component ref breaking `Instantiate`
  - `BasicRigidBodyPush` on PlayerCapsule → Can Push ✅, Push Layers = Default, Strength = 2.5 (enables player to push dropped items)
- Inspector-tunable on InventorySystem: `Drop Max Distance` (spawn radius clamp, default 2.5 m), `Drop Angular Drag` (rolling damping, default 5)
- Data: `ItemDefinition.WorldPrefab` (GameObject), `ItemDefinition.IsDroppable` (default true)
- Docs: `Assets/Docs/inventory-drop-system.md`, `../Docs/adding-pickup-items.md`
- Q key: `InventoryInputHandler` → `RequestDropActiveItem` → `DropFromSlot(slot, hotbar, qty:1)` — always 1 unit regardless of stack size
- Drag drop: `WorldDropZone.OnDrop` → `TryConsumeDrag` → `RequestDropSlotItem` → `DropFromSlot(slot, hotbar, qty:fullStack)`
- Spawn: camera raycast through `Mouse.current.position` → clamped to `_dropMaxDistance` XZ radius from player; fallback = `player.forward * 1.5f + up * 0.3f`

### Mobile Input
- Scripts: `Assets/StarterAssets/Mobile/Scripts/UICanvasControllerInput.cs`, `Assets/Scripts/UI/MobileInventoryButton.cs`, `Assets/Scripts/UI/MobilePickupButton.cs`
- Prefabs: `Assets/App/Prefabs/PlayerRig.prefab` (UI_Canvas_StarterAssetsInputs_Joysticks child)
- Docs: none yet

### Day / Night Cycle
- Scripts: `Assets/Scripts/World/DayNightCycle.cs`
- Scene objects: `DayNightCycle` (root), `Directional Light` (sun)
- Wire in Inspector: `DayNightCycle.sun` → `Directional Light`
- Events: static `DayNightCycle.OnDayStarted` / `OnNightStarted` (C# events) + `onDayStart` / `onNightStart` (UnityEvents in Inspector)
- Public API: `TimeOfDay` (0–1), `IsNight`, `IsDay`
- Note: sets `RenderSettings.ambientMode = Flat` in Awake — this overrides Lighting window ambient mode setting
- Docs: none yet

### Health & Hunger (PlayerStats)
- Scripts: `Assets/Scripts/Player/PlayerStats.cs`, `Assets/Scripts/UI/Stats/StatsUI.cs`
- Prefabs: `Assets/App/Prefabs/PlayerRig.prefab` (PlayerCapsule child holds `PlayerStats`); `Assets/App/Prefabs/InventoryUI.prefab` (`StatsRoot` child of Canvas — health bar left/red, hunger bar right/orange, anchored bottom-center above hotbar at Y=130)
- Static events: `OnStatsReady(float health, float hunger)`, `OnHealthChanged(float)`, `OnHungerChanged(float)`, `OnPlayerDied`
- Static entry points: `PlayerStats.ConsumeFood(float)`, `PlayerStats.TakeDamage(float)`, `PlayerStats.Heal(float)`
- Food items: `ItemDefinition.FoodValue` (float) — set > 0 on food SO assets (Mushroom.asset); consumed via `PlayerStats.ConsumeFood()` from item-use system (not yet wired)
- Hunger drains passively (`_passiveHungerDrain` /sec); doubles when `StarterAssetsInputs.sprint == true`; at 0 health drains at `_starvationDamageRate` /sec
- Setup tool: `Lush World > Setup > Add Player Stats & Bars` (run once, idempotent)
- Docs: `../Docs/health-hunger-system.md`

### Enemy System
- Scripts:
  - `Assets/Scripts/Enemies/EnemyDefinition.cs` — ScriptableObject config (health, speed, radii, damage, day/night aggression flag)
  - `Assets/Scripts/Enemies/EnemyBase.cs` — health, TakeDamage, Die; fires static `OnEnemyDied(EnemyBase)`
  - `Assets/Scripts/Enemies/EnemyAI.cs` — state machine (Patrol/Chase/Attack/Dead) + NavMeshAgent; ThinkLoop coroutine every 0.25 s; patrol runs even when player reference is null; subscribes to DayNightCycle events for live aggression gating
  - `Assets/Scripts/Enemies/Attacks/ZombieSnailAttack.cs` — touch melee; place on child `AttackZone` GO with SphereCollider (trigger); OnTriggerStay → `PlayerStats.TakeDamage` on cooldown
  - `Assets/Scripts/Enemies/EnemySpawner.cs` — proximity-based spawner; per-spawn-point activation radius (not spawner position); `maxEnemiesDay` / `maxEnemiesNight` caps; scatter radius prevents enemies piling on same spot; reacts to `DayNightCycle.OnNightStarted` / `OnDayStarted`; lazy player lookup avoids Start() race condition; **kill debt** (`_killsThisPhase`) reduces effective cap so player-killed enemies never respawn until the next day/night phase
- Prefabs (to be created by user): `Assets/App/Prefabs/Enemies/ZombieSnailMan.prefab`, `Assets/App/Prefabs/Enemies/ZombieSnailWoman.prefab`
- Definition assets (to be created by user): `Assets/App/Enemies/Definitions/ZombieSnailDefinition.asset`
- Models: `Assets/App/Enemies/Zombie_Snail_Man.glb`, `Assets/App/Enemies/Zombie_Snail_Woman.glb`
- Static events: `EnemyBase.OnEnemyDied(EnemyBase)` — fires before GO is deactivated
- Static damage entry: `PlayerStats.TakeDamage(float)` (existing) — called by ZombieSnailAttack
- Day/night integration: `DayNightCycle.OnNightStarted` / `OnDayStarted` (existing static events) — consumed by EnemyAI + EnemySpawner
- EnemyDefinition `isAlwaysAggressive = true` (default): always chases; set to false for passive-by-day behaviour
- Prefab structure: root has NavMeshAgent + CapsuleCollider + EnemyBase + EnemyAI; child `AttackZone` GO has SphereCollider (trigger) + ZombieSnailAttack; set NavMeshAgent `Base Offset` = half model height to prevent terrain burial
- Docs: `../Docs/enemy-system.md`

### Tongue Attack
- Script: `Assets/Scripts/Player/TongueAttack.cs` — on `PlayerCapsule`; subscribes to `PlayerInput.actions["Player/Attack"].performed`; coroutine-driven extend/detect/retract cycle; `RequestAttack()` is the NGO-ready entry point (Phase 2: `[ServerRpc]`)
- Inspector refs (set on PlayerCapsule in PlayerRig.prefab): `_tonguePivot` → `SnailBody/TonguePivot`, `_tongueTransform` → `SnailBody/TonguePivot/Tongue`
- Prefab: `Assets/App/Prefabs/Player/SnailBody.prefab` — `TonguePivot` (empty GO, identity rot) + `Tongue` (Capsule primitive, scale `0.04/0.08/0.04`, rot `90,0,0`, no CapsuleCollider, pink Tongue.mat, inactive at rest)
- Material: `Assets/App/My Materials/Tongue.mat` (URP Lit, pink/red)
- Hit detection: `Physics.OverlapSphere` at full extension — one call per swing; `GetComponentInParent<EnemyBase>()` handles child colliders
- Enemy layer TODO: `_hitLayers = Everything` until Layer 9 `Enemy` is created and assigned to enemy prefabs
- Docs: `../Docs/tongue-attack-system.md`

### Crafting System
- Scripts:
  - `Assets/Scripts/Utilities/IInteractable.cs` — interface (`InteractionLabel`, `Interact(GameObject)`) for any E-key world interactable; implemented by `BlueprintInstance` (Subtask 4); used by `ResourceInteractor`
  - `Assets/Scripts/Data/Ingredient.cs` — shared `[Serializable] struct Ingredient { ItemId, Quantity }` in `LushWorld.Data`; used by both RecipeDefinition and BuildingDefinition
  - `Assets/Scripts/Crafting/RecipeDefinition.cs` — ScriptableObject; fields: `RecipeId`, `DisplayName`, `Category` (enum), `Ingredients` (List<LushWorld.Data.Ingredient>), `OutputItemId`, `OutputQuantity`
  - `Assets/Scripts/Crafting/RecipeRegistry.cs` — ScriptableObject singleton; `List<RecipeDefinition>`, dictionary lookup via `TryGetRecipe(id)`
  - `Assets/Scripts/Crafting/CraftingSystem.cs` — MonoBehaviour on `PlayerCapsule`; static `LocalPlayer`; `RequestCraft(recipeId, qty)` consumes ingredients + calls `InventorySystem.GiveItem`; `GetMaxCraftable(recipe)` + `CountItem(itemId)` are public helpers; static events `OnCraftSuccess`, `OnCraftFailed`, `OnCraftingMenuToggleRequested`; **G** key → `ToggleCraftingMenu()` (called from `InventoryInputHandler.Update`)
  - `Assets/Scripts/UI/Crafting/CraftingUI.cs` — MonoBehaviour on `CraftingMenuUI.prefab` (nested in PlayerRig); subscribes to `CraftingSystem.OnCraftingMenuToggleRequested`; instantiates `CraftingRecipeRowUI` prefab per recipe; refreshes rows on slot changes via `InventoryData` events; on open: unlocks cursor, zeros `StarterAssetsInputs.look/move`, raises Canvas `sortingOrder=150`; spawns full-screen transparent backdrop Button on `Start()` — clicking outside the panel calls `CloseCraftingMenu()`
  - `Assets/Scripts/UI/Crafting/CraftingRecipeRowUI.cs` — MonoBehaviour on row prefab; shows recipe name, ingredient counts (have/need), max craftable; Craft button calls `CraftingSystem.LocalPlayer.RequestCraft`
- Modified: `Assets/Scripts/Inventory/InventoryInputHandler.cs` — C key → `CraftingSystem.LocalPlayer?.ToggleCraftingMenu()`
- Assets (user creates): `Assets/App/Crafting/RecipeRegistry.asset`; one `RecipeDefinition` asset per recipe under `Assets/App/Crafting/Recipes/`
- Prefabs: `Assets/App/Prefabs/CraftingMenuUI.prefab` (Canvas, Screen Space Overlay, **Scale With Screen Size 1920×1080 match=0.5**, sortingOrder=150) nested inside `PlayerRig.prefab`; Panel anchored 10%–90% width × 17.5%–82.5% height (centered, screen-proportional); Content top-stretched inside Panel with VerticalLayoutGroup; `Assets/App/Prefabs/UI/CraftingRowPrefab.prefab` — HorizontalLayoutGroup row, 60px height, auto-size TMP texts 14–28pt, Craft Button
- Inspector wiring on `PlayerCapsule`: `CraftingSystem._recipeRegistry` → `RecipeRegistry.asset`; `CraftingUI._recipeRegistry`, `_itemRegistry`, `_panel`, `_rowContainer`, `_rowPrefab`
- Docs: `../Docs/crafting-building-system.md`

### Building System (Subtask 5 of 5 next — wire prefabs + NavMesh setup)
- Scripts done:
  - `Assets/Scripts/Building/BuildingDefinition.cs` — SO per piece: PieceId, DisplayName, Icon, PlacedPrefab, Cost, MaxHealth, SnapSize
  - `Assets/Scripts/Building/BuildingRegistry.cs` — SO singleton; dictionary lookup by PieceId
  - `Assets/Scripts/Building/BuildingSystem.cs` — MonoBehaviour on `PlayerCapsule`; states: Idle | PlacingGhost; `EnterPlacementMode(def)`, `CancelPlacement()`, `RequestPlaceBlueprint(groundPos,rot)`, `ToggleBuildingMenu()`; ghost follows cursor on ground layer raycast, grid-snapped; swaps sharedMaterials to red on invalid; B key via InventoryInputHandler; Y-offset fix: ghost lifted by `extents.y - (center.y - groundY)` so bottom is flush with ground; `_ghostGroundPosition` passed to `RequestPlaceBlueprint` (not ghost.transform.position) for NGO-safe ground-truth position
  - `Assets/Scripts/Building/BlueprintInstance.cs` — MonoBehaviour + IInteractable on skeleton at spawn; `Init(def)` saves original materials, adds SphereCollider trigger; `Interact(player)` directly calls `RequestDeposit(GetNextNeededItemId(), 1)` — no panel opens; `GetNextNeededItemId()` returns first incomplete ingredient id; `RequestDeposit` removes items from inventory, tracks deposits, calls `TryComplete()`; `TryComplete()` restores materials, re-enables colliders, adds `BuildingPiece`, fires `OnCompleted`; `Demolish()` destroys GO; static event: `OnCompleted` only
  - `Assets/Scripts/Building/BuildingPiece.cs` — MonoBehaviour on completed buildings; `Init(def)` sets MaxHealth; `TakeDamage(float)`, `Demolish()` refunds 50% of Cost via `InventorySystem.GiveItem`
  - `Assets/Scripts/UI/Building/BuildingMenuUI.cs` — Canvas nested in PlayerRig; subscribes to `BuildingSystem.OnBuildingMenuToggleRequested`; cursor/backdrop pattern mirrors CraftingUI
  - `Assets/Scripts/UI/Building/BuildingMenuPieceRowUI.cs` — row prefab; icon, name, cost, Select → `BuildingSystem.EnterPlacementMode`
- Modified: `Assets/Scripts/Inventory/InventoryInputHandler.cs` — B key added
- Modified: `Assets/Scripts/Resource/ResourceInteractor.cs` — `FindNearestNode()` scans for `IInteractable`; `UpdatePrompt()` special-cases `BlueprintInstance`: shows all ingredients as "DisplayName: X/Y" per line + "[E] Add Material   [Hold R] Remove"; `TryPickupNearest()` calls `UpdatePrompt()` after `Interact()` to refresh counts; `HandleBlueprintRHold()` in Update() accumulates R hold and calls `bp.Demolish()` at 1.5 s
- Assets (user creates): `Assets/App/Building/BuildingRegistry.asset`; definitions in `Assets/App/Building/Definitions/`; materials in `Assets/App/Materials/Building/`
- Prefabs (user creates): `BuildingMenuUI.prefab`, `BuildingPieceRowPrefab.prefab` — nested in PlayerRig (BlueprintDepositUI/Row prefabs no longer needed — removed)
- Docs: `../Docs/crafting-building-system.md`

### Dev / Editor Tools
- Scripts: `Assets/Scripts/DevTools/DebugCursorToggle.cs`, `Assets/Scripts/Editor/ItemIconGenerator.cs`, `Assets/Scripts/Editor/StatsSetupTool.cs`
- Prefabs: none
- Docs: none
