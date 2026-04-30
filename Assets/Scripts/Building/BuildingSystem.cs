using System;
using System.Collections.Generic;
using LushWorld.Inventory;
using LushWorld.Utilities;

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
        public static event Action<bool> OnPlacementStateChanged;
        public static event Action       OnBlueprintPlaced;

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
        private Material           _ghostInvalidMaterial;
        private bool               _isValidPlacement = true;
        public static bool IsMenuOpen { get; private set; }
        private UnityEngine.Camera _cam;
        private Vector3            _ghostGroundPosition;
        private Quaternion         _placementBaseRotation;
        private float              _currentYRotation;
        private bool               _mobileRotating;

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

        // Called by the mobile Place button — mirrors left-click placement.
        public void MobilePlacePressed()
        {
            if (_state != PlacementState.PlacingGhost) return;
            if (!_isValidPlacement || _ghostInstance == null || !_ghostInstance.activeSelf) return;
            RequestPlaceBlueprint(_ghostGroundPosition, _ghostInstance.transform.rotation);
        }

        // Called by the mobile Rotate button on press (true) and release (false).
        public void MobileRotateHeld(bool isHeld)
        {
            _mobileRotating = _state == PlacementState.PlacingGhost && isHeld;
        }

        // Called by BuildingMenuPieceRowUI when the player selects a piece type.
        public void EnterPlacementMode(BuildingDefinition def)
        {
            if (def?.PlacedPrefab == null) return;
            CancelPlacement();
            CloseBuildingMenu();

            _activeDef     = def;
            _ghostInstance = Instantiate(def.PlacedPrefab);
            _ghostInstance.name = $"Ghost_{def.PieceId}";
            _placementBaseRotation = _ghostInstance.transform.rotation;
            _currentYRotation      = 0f;

            foreach (var col in _ghostInstance.GetComponentsInChildren<Collider>())
                col.enabled = false;

            foreach (var node in _ghostInstance.GetComponentsInChildren<LushWorld.Resource.ResourceNode>())
                Destroy(node);

            _ghostInvalidMaterial = new Material(_ghostValidMaterial);
            Color validColor = _ghostValidMaterial.GetColor("_BaseColor");
            _ghostInvalidMaterial.SetColor("_BaseColor", new Color(1f, 0.2f, 0.2f, validColor.a));

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
            OnPlacementStateChanged?.Invoke(true);
        }

        public void CancelPlacement()
        {
            _mobileRotating = false;
            if (_ghostInstance != null) Destroy(_ghostInstance);
            _ghostInstance = null;
            _rendererData.Clear();

            if (_ghostInvalidMaterial != null) { Destroy(_ghostInvalidMaterial); _ghostInvalidMaterial = null; }

            _activeDef = null;
            _state     = PlacementState.Idle;
            OnPlacementStateChanged?.Invoke(false);
        }

        // Phase 2 upgrade: [ServerRpc(RequireOwnership = true)]
        public void RequestPlaceBlueprint(Vector3 groundPosition, Quaternion rotation)
        {
            if (_activeDef?.PlacedPrefab == null) return;

            var blueprint = Instantiate(_activeDef.PlacedPrefab, groundPosition, rotation);
            blueprint.name = $"Blueprint_{_activeDef.PieceId}";

            var brs = blueprint.GetComponentsInChildren<MeshRenderer>();
            if (brs.Length > 0)
            {
                var b = brs[0].bounds;
                for (int i = 1; i < brs.Length; i++) b.Encapsulate(brs[i].bounds);
                float lift = b.extents.y - (b.center.y - groundPosition.y);
                blueprint.transform.position = groundPosition + new Vector3(0f, Mathf.Max(0f, lift), 0f);
            }

            foreach (var node in blueprint.GetComponentsInChildren<LushWorld.Resource.ResourceNode>())
                Destroy(node);

            blueprint.AddComponent<BlueprintInstance>().Init(_activeDef);
            OnBlueprintPlaced?.Invoke();

            foreach (var mr in blueprint.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = new Material[mr.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = _skeletonMaterial;
                mr.sharedMaterials = mats;
            }

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
            Ray ray;
            if (PlatformDetector.IsDesktop && Mouse.current != null)
            {
                Vector2 mp2 = Mouse.current.position.ReadValue();
                ray = _cam.ScreenPointToRay(new Vector3(mp2.x, mp2.y, 0f));
            }
            else
            {
                ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            }
#else
            Ray ray = PlatformDetector.IsMobile
                ? _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : _cam.ScreenPointToRay(Input.mousePosition);
#endif

            if (Physics.Raycast(ray, out var hit, _maxPlacementDistance, _groundLayer))
            {
                Vector3 snapped = SnapToGrid(hit.point, _activeDef.SnapSize);
                _ghostGroundPosition = snapped;

                Quaternion ghostRotation = _placementBaseRotation * Quaternion.Euler(0f, _currentYRotation, 0f);
                _ghostInstance.transform.SetPositionAndRotation(snapped, ghostRotation);

                var b = GetGhostBounds(snapped);
                float lift = b.extents.y - (b.center.y - snapped.y);
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
            bool rotateHeld = _mobileRotating || (Keyboard.current != null && Keyboard.current.rKey.isPressed);
            bool leftClick  = Mouse.current    != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool cancel     = (Mouse.current   != null && Mouse.current.rightButton.wasPressedThisFrame)
                           || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
#else
            bool rotateHeld = _mobileRotating || Input.GetKey(KeyCode.R);
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

        // Reconstructs a saved skeleton building. Does NOT fire OnBlueprintPlaced.
        public void SpawnBlueprintFromSave(BuildingDefinition def, Vector3 groundPosition, Quaternion rotation,
                                           Dictionary<string, int> deposits)
        {
            if (def?.PlacedPrefab == null) return;

            var go = Instantiate(def.PlacedPrefab, groundPosition, rotation);
            go.name = $"Blueprint_{def.PieceId}";

            var brs = go.GetComponentsInChildren<MeshRenderer>();
            if (brs.Length > 0)
            {
                var b = brs[0].bounds;
                for (int i = 1; i < brs.Length; i++) b.Encapsulate(brs[i].bounds);
                float lift = b.extents.y - (b.center.y - groundPosition.y);
                go.transform.position = groundPosition + new Vector3(0f, Mathf.Max(0f, lift), 0f);
            }

            foreach (var node in go.GetComponentsInChildren<LushWorld.Resource.ResourceNode>())
                Destroy(node);

            var bp = go.AddComponent<BlueprintInstance>();
            bp.Init(def);
            if (deposits != null) bp.SetDepositedFromSave(deposits);

            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = new Material[mr.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = _skeletonMaterial;
                mr.sharedMaterials = mats;
            }

            foreach (var col in go.GetComponentsInChildren<Collider>())
                if (!col.isTrigger) col.enabled = false;
        }
    }
}
