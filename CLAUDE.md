# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

Implemented: first-person movement (walk/sprint/jump), terrain, URP lighting, fog, glTF model import (snail character with modular body+shell).

Not yet implemented: attack, interact, crouch, NPC/AI, dialogue, inventory, save/load, audio management, any server-side logic.

## Conventions

- New gameplay scripts go under `Assets/Scripts/` (create the folder — it doesn't exist yet)
- Do not put game logic inside `Assets/StarterAssets/` or `Assets/Polytope Studio/` — those are third-party assets
- Use `StarterAssetsInputs` for all input reads; do not call `Input.GetKey` directly
- URP-only shaders and materials; avoid standard/legacy shader references
