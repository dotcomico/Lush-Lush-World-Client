using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using LushWorld.Building;
using LushWorld.Inventory;
using LushWorld.Player;
using LushWorld.Resource;
using LushWorld.World;
using LushWorld.UI;

namespace LushWorld.Save
{
    // Scene singleton. Place on a persistent GameObject in the game scene.
    // Handles all save/load/reset operations and runs the 30-second auto-save loop.
    // Inspector requirement: assign _buildingRegistry (BuildingRegistry.asset) in the Inspector.
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [SerializeField] private BuildingRegistry _buildingRegistry;

        private string _saveDir;
        private string _savePath;
        private string _settingsPath;
        private bool   _isLoading;

        // Baseline of all scene-placed ResourceNode positions captured before any terrain nodes spawn.
        // Used at save time to detect which scene items (e.g. hearts) have been collected.
        private List<Vector3> _sceneNodeBaseline;

        // Tracked so we can subscribe/unsubscribe to its slot-changed events for per-change saves.
        private InventoryData _trackedInventory;
        // Blocks per-slot SaveGame() calls during bulk inventory operations (e.g. ClearAll on death).
        private bool _suppressSaves;
        // > 0 while a debounced save is pending; value is the Time.time at which to fire it.
        private float _deferredSaveTime = -1f;
        private const float StatsSaveDebounce = 5f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _saveDir      = Path.Combine(Application.persistentDataPath, "LushWorld");
            _savePath     = Path.Combine(_saveDir, "save.json");
            _settingsPath = Path.Combine(_saveDir, "settings.json");
            Directory.CreateDirectory(_saveDir);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            BlueprintInstance.OnCompleted        += OnBuildingCompleted;
            BuildingSystem.OnBlueprintPlaced     += OnBlueprintPlaced;
            HeartStone.OnHeartsChanged           += OnHeartsChanged;
            PlayerStats.OnPlayerDied             += OnPlayerDied;
            PlayerStats.OnHealthChanged          += OnStatChanged;
            PlayerStats.OnHungerChanged          += OnStatChanged;
            InventorySystem.OnInventoryReady     += OnInventoryReady;
            InventorySystem.OnInventoryDestroyed += UntrackInventory;
        }

        private void OnDisable()
        {
            BlueprintInstance.OnCompleted        -= OnBuildingCompleted;
            BuildingSystem.OnBlueprintPlaced     -= OnBlueprintPlaced;
            HeartStone.OnHeartsChanged           -= OnHeartsChanged;
            PlayerStats.OnPlayerDied             -= OnPlayerDied;
            PlayerStats.OnHealthChanged          -= OnStatChanged;
            PlayerStats.OnHungerChanged          -= OnStatChanged;
            InventorySystem.OnInventoryReady     -= OnInventoryReady;
            InventorySystem.OnInventoryDestroyed -= UntrackInventory;
            UntrackInventory();
        }

        // Fires before OnDisable in both the Editor (Stop) and builds — reliable quit-save.
        private void OnApplicationQuit() { SaveGame(); }

        private void Update()
        {
            if (_deferredSaveTime > 0f && Time.time >= _deferredSaveTime && !_isLoading)
            {
                _deferredSaveTime = -1f;
                SaveGame();
            }
        }

        private IEnumerator Start()
        {
            // Capture scene-placed ResourceNode positions BEFORE yield so no terrain nodes
            // have spawned yet (terrain spawning starts on the first Update call).
            CaptureSceneNodeBaseline();

            yield return null; // wait one frame so all Awake() calls complete
            LoadSettings();
            if (File.Exists(_savePath)) LoadGame();
            StartCoroutine(AutoSaveLoop());
        }

        // ── Event handlers for immediate saves ───────────────────────────────

        private void OnBuildingCompleted(BlueprintInstance bp) { if (!_isLoading) SaveGame(); }
        private void OnBlueprintPlaced()                       { if (!_isLoading) SaveGame(); }
        private void OnHeartsChanged()                         { if (!_isLoading) SaveGame(); }

        private void OnPlayerDied()
        {
            if (_isLoading) return;
            // Suppress per-slot saves while wiping inventory; one explicit save follows.
            _suppressSaves = true;
            InventorySystem.LocalPlayer?.Data.ClearAll();
            PlayerStats.LocalPlayer?.Respawn();
            _suppressSaves = false;
            SaveGame();
        }

        private void OnInventoryReady(InventoryData data)
        {
            UntrackInventory();
            _trackedInventory = data;
            data.OnHotbarSlotChanged   += OnInventorySlotChanged;
            data.OnBackpackSlotChanged += OnInventorySlotChanged;
        }

        private void UntrackInventory()
        {
            if (_trackedInventory == null) return;
            _trackedInventory.OnHotbarSlotChanged   -= OnInventorySlotChanged;
            _trackedInventory.OnBackpackSlotChanged -= OnInventorySlotChanged;
            _trackedInventory = null;
        }

        private void OnInventorySlotChanged(int _, ItemStack __)
        {
            if (!_isLoading && !_suppressSaves) SaveGame();
        }

        private void OnStatChanged(float _)
        {
            if (!_isLoading) _deferredSaveTime = Time.time + StatsSaveDebounce;
        }

        // ── Auto-save loop ────────────────────────────────────────────────────

        private IEnumerator AutoSaveLoop()
        {
            var wait = new WaitForSeconds(30f);
            while (true)
            {
                yield return wait;
                if (!_isLoading) SaveGame();
            }
        }

        // ── Save ──────────────────────────────────────────────────────────────

        public void SaveGame()
        {
            var data = new GameSaveData();

            // ── Player ───────────────────────────────────────────────────────
            var stats = PlayerStats.LocalPlayer;
            if (stats != null)
            {
                data.playerData = new PlayerSaveData
                {
                    position  = stats.transform.position,
                    yRotation = stats.transform.eulerAngles.y,
                    health    = stats.Health,
                    hunger    = stats.Hunger,
                };
            }

            // ── Inventory ────────────────────────────────────────────────────
            var inv = InventorySystem.LocalPlayer?.Data;
            if (inv != null)
            {
                var id = new InventorySaveData
                {
                    hotbar      = new ItemStack[InventoryData.HotbarSize],
                    backpack    = new ItemStack[InventoryData.BackpackSize],
                    selectedSlot = inv.SelectedHotbarSlot,
                };
                for (int i = 0; i < InventoryData.HotbarSize;   i++) id.hotbar[i]   = inv.GetHotbarSlot(i);
                for (int i = 0; i < InventoryData.BackpackSize; i++) id.backpack[i] = inv.GetBackpackSlot(i);
                data.inventoryData = id;
            }

            // ── Skeleton buildings ───────────────────────────────────────────
            data.blueprints = new List<BlueprintSaveData>();
            foreach (var bp in FindObjectsByType<BlueprintInstance>(FindObjectsSortMode.None))
            {
                // TryComplete() adds BuildingPiece before Destroy(this) is deferred — skip to avoid
                // writing the same GO as both a blueprint and a completed building in the same save.
                if (bp.GetComponent<BuildingPiece>() != null) continue;

                var bpData = new BlueprintSaveData
                {
                    pieceId  = bp.Def.PieceId,
                    position = bp.transform.position,
                    rotation = bp.transform.rotation,
                    deposits = new List<DepositedItem>(),
                };
                foreach (var ing in bp.Def.Cost)
                    bpData.deposits.Add(new DepositedItem { key = ing.ItemId, value = bp.GetDeposited(ing.ItemId) });
                data.blueprints.Add(bpData);
            }

            // ── Completed buildings ──────────────────────────────────────────
            data.buildings = new List<BuildingPieceSaveData>();
            foreach (var piece in FindObjectsByType<BuildingPiece>(FindObjectsSortMode.None))
            {
                data.buildings.Add(new BuildingPieceSaveData
                {
                    pieceId       = piece.Def.PieceId,
                    position      = piece.transform.position,
                    rotation      = piece.transform.rotation,
                    currentHealth = piece.Health,
                });
            }

            // ── HeartStone ───────────────────────────────────────────────────
            data.heartsPlaced = HeartStone.Instance != null ? HeartStone.Instance.PlacedCount : 0;

            // ── Harvested terrain resource spots ─────────────────────────────
            var trm = FindFirstObjectByType<TerrainResourceManager>();
            data.harvestedSpotPositions = trm != null ? trm.GetHarvestedPositions() : new List<Vector3>();

            // ── Collected scene-placed items (e.g. hearts on the ground) ─────
            data.collectedSceneNodePositions = GetCollectedSceneNodePositions();

            // ── World ────────────────────────────────────────────────────────
            var dnc = FindFirstObjectByType<DayNightCycle>();
            data.worldData = new WorldSaveData { timeOfDay = dnc != null ? dnc.timeOfDay : 0.25f };

            data.saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            File.WriteAllText(_savePath, JsonUtility.ToJson(data));
        }

        // ── Load ──────────────────────────────────────────────────────────────

        public void LoadGame()
        {
            if (!File.Exists(_savePath)) return;

            GameSaveData data;
            try { data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(_savePath)); }
            catch (Exception e) { Debug.LogWarning($"[SaveManager] Failed to load save: {e.Message}"); return; }

            _isLoading = true;
            try { ApplyLoadedData(data); }
            finally { _isLoading = false; }
        }

        private void ApplyLoadedData(GameSaveData data)
        {
            // ── Player position ──────────────────────────────────────────────
            var stats = PlayerStats.LocalPlayer;
            if (stats != null && data.playerData != null)
            {
                // Disable CC before teleporting — setting transform.position while it's active
                // is unreliable; the CC must be toggled so it re-syncs its internal capsule.
                var cc = stats.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                stats.transform.position = data.playerData.position;
                if (cc != null) cc.enabled = true;

                var euler = stats.transform.eulerAngles;
                stats.transform.eulerAngles = new Vector3(euler.x, data.playerData.yRotation, euler.z);
                stats.LoadState(data.playerData.health, data.playerData.hunger);
            }

            // ── Inventory ────────────────────────────────────────────────────
            if (data.inventoryData != null && InventorySystem.LocalPlayer != null)
                InventorySystem.LocalPlayer.Data.LoadState(
                    data.inventoryData.hotbar,
                    data.inventoryData.backpack,
                    data.inventoryData.selectedSlot);

            // ── HeartStone ───────────────────────────────────────────────────
            HeartStone.Instance?.LoadHearts(data.heartsPlaced);
            // Restore end-game visuals without replaying the animation if player already won.
            if (HeartStone.Instance != null && HeartStone.Instance.IsComplete)
                HeartEndSequencer.Instance?.SnapToEndState();

            // ── Harvested terrain resource spots ─────────────────────────────
            FindFirstObjectByType<TerrainResourceManager>()
                ?.ApplyHarvestedPositions(data.harvestedSpotPositions);

            // ── Collected scene-placed items — destroy nodes already picked up ─
            RestoreCollectedSceneNodes(data.collectedSceneNodePositions);

            // ── Skeleton buildings ───────────────────────────────────────────
            if (_buildingRegistry != null && data.blueprints != null)
                foreach (var bd in data.blueprints)
                    SpawnBlueprintFromSave(bd);

            // ── Completed buildings ──────────────────────────────────────────
            if (_buildingRegistry != null && data.buildings != null)
                foreach (var pd in data.buildings)
                    SpawnBuildingPieceFromSave(pd);

            // ── Day / Night ──────────────────────────────────────────────────
            FindFirstObjectByType<DayNightCycle>()?.LoadTimeOfDay(data.worldData?.timeOfDay ?? 0.25f);
        }

        // ── Settings ──────────────────────────────────────────────────────────

        public void SaveSettings()
        {
            var settings = FindFirstObjectByType<SettingsUIController>()?.GetCurrentSettings();
            if (settings == null) return;
            File.WriteAllText(_settingsPath, JsonUtility.ToJson(settings));
        }

        public void LoadSettings()
        {
            if (!File.Exists(_settingsPath)) return;
            try
            {
                var settings = JsonUtility.FromJson<GameSettings>(File.ReadAllText(_settingsPath));
                FindFirstObjectByType<SettingsUIController>()?.ApplySettings(settings);
            }
            catch (Exception e) { Debug.LogWarning($"[SaveManager] Failed to load settings: {e.Message}"); }
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        public void ResetGame()
        {
            if (File.Exists(_savePath)) File.Delete(_savePath);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ResetSettings()
        {
            if (File.Exists(_settingsPath)) File.Delete(_settingsPath);
            FindFirstObjectByType<SettingsUIController>()?.ApplySettings(GameSettings.Default);
        }

        // ── Scene node tracking ───────────────────────────────────────────────

        private void CaptureSceneNodeBaseline()
        {
            _sceneNodeBaseline = new List<Vector3>();
            foreach (var node in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
                _sceneNodeBaseline.Add(node.transform.position);
        }

        private List<Vector3> GetCollectedSceneNodePositions()
        {
            if (_sceneNodeBaseline == null || _sceneNodeBaseline.Count == 0)
                return new List<Vector3>();

            // Build a set of positions for all currently-alive ResourceNodes.
            var liveNodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            const float toleranceSq = 0.01f;

            var result = new List<Vector3>();
            foreach (var baselinePos in _sceneNodeBaseline)
            {
                bool alive = false;
                foreach (var node in liveNodes)
                    if (Vector3.SqrMagnitude(node.transform.position - baselinePos) < toleranceSq)
                    { alive = true; break; }
                if (!alive) result.Add(baselinePos);
            }
            return result;
        }

        private void RestoreCollectedSceneNodes(List<Vector3> collectedPositions)
        {
            if (collectedPositions == null || collectedPositions.Count == 0) return;

            const float toleranceSq = 0.01f;
            foreach (var node in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
                foreach (var pos in collectedPositions)
                    if (Vector3.SqrMagnitude(node.transform.position - pos) < toleranceSq)
                    {
                        Destroy(node.gameObject);
                        break;
                    }
        }

        // ── Building spawn helpers ────────────────────────────────────────────

        private void SpawnBlueprintFromSave(BlueprintSaveData bd)
        {
            if (!_buildingRegistry.TryGetPiece(bd.pieceId, out var def)) return;

            var deposits = new Dictionary<string, int>();
            if (bd.deposits != null)
                foreach (var d in bd.deposits)
                    deposits[d.key] = d.value;

            BuildingSystem.LocalPlayer?.SpawnBlueprintFromSave(def, bd.position, bd.rotation, deposits);
        }

        private void SpawnBuildingPieceFromSave(BuildingPieceSaveData pd)
        {
            if (!_buildingRegistry.TryGetPiece(pd.pieceId, out var def)) return;

            var go = Instantiate(def.PlacedPrefab, pd.position, pd.rotation);
            go.name = $"Building_{def.PieceId}";

            foreach (var node in go.GetComponentsInChildren<ResourceNode>())
                Destroy(node);

            go.AddComponent<BuildingPiece>().Init(def, pd.currentHealth);
        }
    }
}
