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

**Not yet implemented:** crafting, building, farming, enemies/combat, day-night cycle, NPC/AI, dialogue, save/load, multiplayer networking.

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
- Prefabs: `Assets/App/Prefabs/PlayerRig.prefab` — top-level children: MainCamera, PlayerFollowCamera, PlayerCapsule (holds FirstPersonController + SlideController), UI_Canvas_StarterAssetsInputs_Joysticks, UI_EventSystem, CameraViewManager, ThirdPerson_VirtualCamera, Isometric_VirtualCamera, InventoryUI, SettingsUI
- Docs: `../Docs/shell-slide-system.md`

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

### Settings UI
- Scripts: `Assets/Scripts/UI/Settings/SettingsUIController.cs`
- Prefabs: `Assets/App/Prefabs/SettingsUI.prefab`
- Docs: `../Docs/settings-ui.md`
- Tech debt: `../Docs/TECH_DEBT.md` (3 items flagged)

### Resource Pickup
- Scripts: `Assets/Scripts/Resource/TerrainResourceManager.cs`, `Assets/Scripts/Resource/ResourceNode.cs`, `Assets/Scripts/Resource/ResourceInteractor.cs`
- Prefabs: `Assets/App/Prefabs/Rocks/` (variants), `Assets/App/Prefabs/Mushrooms/` (variants), `Assets/App/Prefabs/Sticks/` (variants)
- Docs: `Assets/Docs/terrain-tree-pickup-system.md`, `../Docs/adding-pickup-items.md`

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

### Dev / Editor Tools
- Scripts: `Assets/Scripts/DevTools/DebugCursorToggle.cs`, `Assets/Scripts/Editor/ItemIconGenerator.cs`
- Prefabs: none
- Docs: none
