using System;
using System.Collections.Generic;
using LushWorld.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LushWorld.Building
{
    // Sits on PlayerCapsule alongside CraftingSystem and InventorySystem.
    // States: Idle → PlacingGhost (on EnterPlacementMode) → Idle (on confirm or cancel).
    // Phase 2 upgrade: RequestPlaceBlueprint becomes [ServerRpc(RequireOwnership = true)].
    [RequireComponent(typeof(InventorySystem))]
    public class BuildingSystem : MonoBehaviour
    {
        public static BuildingSystem LocalPlayer { get; private set; }

        public static event Action<bool> OnBuildingMenuToggleRequested;

        [SerializeField] private Material  _ghostValidMaterial;
        [SerializeField] private Material  _skeletonMaterial;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private float     _maxPlacementDistance = 10f;
        [SerializeField] private float     _rotationSpeed        = 90f;

        private enum PlacementState { Idle, PlacingGhost }
        private PlacementState _state = PlacementState.Idle;

        private BuildingDefinition _activeDef;
        private GameObject         _ghostInstance;
        private Material           _ghostInvalidMaterial; // created once per placement session, destroyed on cancel
        private bool               _isValidPlacement = true;
        public static bool IsMenuOpen { get; private set; }
        private UnityEngine.Camera _cam;
        private Vector3            _ghostGroundPosition;
        private Quaternion         _placementBaseRotation;
        private float              _currentYRotation;

        // Per-renderer cached material arrays — swapped on validity change, never per-frame allocated.
        private readonly List<(MeshRenderer mr, Material[] valid, Material[] invalid)> _rendererData = new();

        private void Awake()
        {
            LocalPlayer = this;
            _cam        = UnityEngine.Camera.main;
        }

        private void OnDestroy()
        {
            if (LocalPlayer == this) LocalPlayer = null;
        }

        private void Update()
        {
            if (_state == PlacementState.PlacingGhost) UpdateGhost();
        }

        // ── Public API ──────────────────────────────────────────────────────────────────

        public void ToggleBuildingMenu()
        {
            // If ghost is active, B cancels placement instead of reopening the menu.
            if (_state == PlacementState.PlacingGhost) { CancelPlacement(); return; }
            IsMenuOpen = !IsMenuOpen;
            OnBuildingMenuToggleRequested?.Invoke(IsMenuOpen);
        }

        public void CloseBuildingMenu()
        {
            if (!IsMenuOpen) return;
            IsMenuOpen = false;
            OnBuildingMenuToggleRequested?.Invoke(false);
        }

        // Called by BuildingMenuPieceRowUI when the player selects a piece type.
        public void EnterPlacementMode(BuildingDefinition def)
        {
            if (def?.PlacedPrefab == null) return;
            CancelPlacement();   // clean up any previous ghost
            CloseBuildingMenu(); // close the menu panel

            _activeDef     = def;
            _ghostInstance = Instantiate(def.PlacedPrefab);
            _ghostInstance.name = $"Ghost_{def.PieceId}";
            _placementBaseRotation = _ghostInstance.transform.rotation;
            _currentYRotation      = 0f;

            // Disable physics on ghost — it is visual-only, must not interfere with overlap checks.
            foreach (var col in _ghostInstance.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Remove resource-pickup behaviour — building pieces must never be treated as world resources.
            foreach (var node in _ghostInstance.GetComponentsInChildren<LushWorld.Resource.ResourceNode>())
                Destroy(node);

            // Build invalid (red-tinted) material from valid ghost material — one allocation per session.
            _ghostInvalidMaterial = new Material(_ghostValidMaterial);
            Color validColor = _ghostValidMaterial.GetColor("_BaseColor");
            _ghostInvalidMaterial.SetColor("_BaseColor", new Color(1f, 0.2f, 0.2f, validColor.a));

            // Cache material arrays per renderer so we swap sharedMaterials (no GC) each validity change.
            _rendererData.Clear();
            foreach (var mr in _ghostInstance.GetComponentsInChildren<MeshRenderer>())
            {
                int slots = mr.sharedMaterials.Length;
                var validMats   = new Material[slots];
                var invalidMats = new Material[slots];
                for (int i = 0; i < slots; i++)
                {
                    validMats[i]   = _ghostValidMaterial;
                    invalidMats[i] = _ghostInvalidMaterial;
                }
                mr.sharedMaterials = validMats;
                _rendererData.Add((mr, validMats, invalidMats));
            }

            _isValidPlacement = true;
            _state = PlacementState.PlacingGhost;
        }

        public void CancelPlacement()
        {
            if (_ghostInstance != null) Destroy(_ghostInstance);
            _ghostInstance = null;
            _rendererData.Clear();

            if (_ghostInvalidMaterial != null) { Destroy(_ghostInvalidMaterial); _ghostInvalidMaterial = null; }

            _activeDef = null;
            _state     = PlacementState.Idle;
        }

        // Phase 2 upgrade: [ServerRpc(RequireOwnership = true)]
        // groundPosition is the snapped XZ position at ground level — this method applies the Y lift.
        public void RequestPlaceBlueprint(Vector3 groundPosition, Quaternion rotation)
        {
            if (_activeDef?.PlacedPrefab == null) return;

            var blueprint = Instantiate(_activeDef.PlacedPrefab, groundPosition, rotation);
            blueprint.name = $"Blueprint_{_activeDef.PieceId}";

            // Lift so the mesh bottom is flush with the ground (same robust formula as the ghost).
            var brs = blueprint.GetComponentsInChildren<MeshRenderer>();
            if (brs.Length > 0)
            {
                var b = brs[0].bounds;
                for (int i = 1; i < brs.Length; i++) b.Encapsulate(brs[i].bounds);
                float lift = b.extents.y - (b.center.y - groundPosition.y);
                blueprint.transform.position = groundPosition + new Vector3(0f, Mathf.Max(0f, lift), 0f);
            }

            // Remove resource-pickup behaviour before colliders are ever re-enabled by TryComplete.
            foreach (var node in blueprint.GetComponentsInChildren<LushWorld.Resource.ResourceNode>())
                Destroy(node);

            // Init BEFORE applying skeleton material so BlueprintInstance captures original materials.
            blueprint.AddComponent<BlueprintInstance>().Init(_activeDef);

            // Apply skeleton material (amber transparent) — piece awaits material deposit.
            foreach (var mr in blueprint.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = new Material[mr.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = _skeletonMaterial;
                mr.sharedMaterials = mats;
            }

            // Disable non-trigger colliders — skeleton is not a real structure; player walks through it.
            // The trigger added by BlueprintInstance.Init (isTrigger = true) is intentionally preserved.
            foreach (var col in blueprint.GetComponentsInChildren<Collider>())
                if (!col.isTrigger) col.enabled = false;

            CancelPlacement();
        }

        // ── Ghost update ────────────────────────────────────────────────────────────────

        private void UpdateGhost()
        {
            if (_cam == null) _cam = UnityEngine.Camera.main;
            if (_cam == null || _ghostInstance == null) return;

#if ENABLE_INPUT_SYSTEM
            Vector2 mp2 = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var mousePos = new Vector3(mp2.x, mp2.y, 0f);
#else
            var mousePos = Input.mousePosition;
#endif
            var ray = _cam.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out var hit, _maxPlacementDistance, _groundLayer))
            {
                Vector3 snapped = SnapToGrid(hit.point, _activeDef.SnapSize);
                _ghostGroundPosition = snapped;

                // Move to snapped so renderer bounds are in the correct coordinate space.
                Quaternion ghostRotation = _placementBaseRotation * Quaternion.Euler(0f, _currentYRotation, 0f);
                _ghostInstance.transform.SetPositionAndRotation(snapped, ghostRotation);

                // Lift so the mesh bottom is flush with the ground.
                // Robust formula: handles pivot-at-center and pivot-at-bottom equally.
                var b    = GetGhostBounds(snapped);
                float lift    = b.extents.y - (b.center.y - snapped.y);
                Vector3 placedPos = snapped + new Vector3(0f, Mathf.Max(0f, lift), 0f);
                _ghostInstance.transform.position = placedPos;

                _ghostInstance.SetActive(true);
                SetValidity(CheckPlacementValid(placedPos));
            }
            else
            {
                _ghostInstance.SetActive(false);
                SetValidity(false);
            }

            HandlePlacementInput();
        }

        private void SetValidity(bool valid)
        {
            if (valid == _isValidPlacement) return;
            _isValidPlacement = valid;
            foreach (var (mr, validMats, invalidMats) in _rendererData)
                mr.sharedMaterials = valid ? validMats : invalidMats;
        }

        private bool CheckPlacementValid(Vector3 position)
        {
            if (_obstacleLayer.value == 0) return true;
            Bounds bounds = GetGhostBounds(position);
            return !Physics.CheckBox(bounds.center, bounds.extents * 0.9f, Quaternion.identity, _obstacleLayer);
        }

        private Bounds GetGhostBounds(Vector3 fallback)
        {
            if (_rendererData.Count == 0)
                return new Bounds(fallback, Vector3.one * 0.5f);
            var b = _rendererData[0].mr.bounds;
            for (int i = 1; i < _rendererData.Count; i++) b.Encapsulate(_rendererData[i].mr.bounds);
            return b;
        }

        private void HandlePlacementInput()
        {
#if ENABLE_INPUT_SYSTEM
            bool rotateHeld = Keyboard.current != null && Keyboard.current.rKey.isPressed;
            bool leftClick  = Mouse.current    != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool cancel     = (Mouse.current   != null && Mouse.current.rightButton.wasPressedThisFrame)
                           || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
#else
            bool rotateHeld = Input.GetKey(KeyCode.R);
            bool leftClick  = Input.GetMouseButtonDown(0);
            bool cancel     = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
#endif
            if (rotateHeld) _currentYRotation += _rotationSpeed * Time.deltaTime;

            if (cancel) { CancelPlacement(); return; }

            if (leftClick
                && _isValidPlacement
                && _ghostInstance != null
                && _ghostInstance.activeSelf
                && !EventSystem.current.IsPointerOverGameObject())
            {
                RequestPlaceBlueprint(
                    _ghostGroundPosition,
                    _ghostInstance.transform.rotation);
            }
        }

        private static Vector3 SnapToGrid(Vector3 position, float snapSize)
        {
            if (snapSize <= 0f) return position;
            return new Vector3(
                Mathf.Round(position.x / snapSize) * snapSize,
                position.y,
                Mathf.Round(position.z / snapSize) * snapSize);
        }
    }
}
