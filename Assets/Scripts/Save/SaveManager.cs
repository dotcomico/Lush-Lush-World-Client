using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using LushWorld.Building;
using LushWorld.Inventory;
using LushWorld.Player;
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
            BlueprintInstance.OnCompleted  += OnBuildingCompleted;
            BuildingSystem.OnBlueprintPlaced += OnBlueprintPlaced;
            HeartStone.OnHeartsChanged       += OnHeartsChanged;
            Application.quitting             += SaveGame;
        }

        private void OnDisable()
        {
            BlueprintInstance.OnCompleted  -= OnBuildingCompleted;
            BuildingSystem.OnBlueprintPlaced -= OnBlueprintPlaced;
            HeartStone.OnHeartsChanged       -= OnHeartsChanged;
            Application.quitting             -= SaveGame;
        }

        private IEnumerator Start()
        {
            yield return null; // wait one frame so all Awake() calls complete
            LoadSettings();
            if (File.Exists(_savePath)) LoadGame();
            StartCoroutine(AutoSaveLoop());
        }

        // ── Event handlers for immediate saves ───────────────────────────────

        private void OnBuildingCompleted(BlueprintInstance bp) { if (!_isLoading) SaveGame(); }
        private void OnBlueprintPlaced()                       { if (!_isLoading) SaveGame(); }
        private void OnHeartsChanged()                         { if (!_isLoading) SaveGame(); }

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
                    position  = stats.transform.root.position,
                    yRotation = stats.transform.root.eulerAngles.y,
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
                var root   = stats.transform.root;
                root.position = data.playerData.position;
                var euler  = root.eulerAngles;
                root.eulerAngles = new Vector3(euler.x, data.playerData.yRotation, euler.z);
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

            foreach (var node in go.GetComponentsInChildren<Resource.ResourceNode>())
                Destroy(node);

            go.AddComponent<BuildingPiece>().Init(def, pd.currentHealth);
        }
    }
}
