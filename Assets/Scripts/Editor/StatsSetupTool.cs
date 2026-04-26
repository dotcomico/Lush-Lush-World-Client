using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using LushWorld.UI.Stats;

namespace LushWorld.Editor
{
    // One-shot editor tool: wires PlayerStats onto PlayerCapsule and builds the
    // health/hunger bars inside InventoryUI.prefab above the hotbar.
    // Run once via menu: Lush World > Setup > Add Player Stats & Bars
    public static class StatsSetupTool
    {
        private const string PlayerRigPath   = "Assets/App/Prefabs/PlayerRig.prefab";
        private const string InventoryUIPath = "Assets/App/Prefabs/InventoryUI.prefab";

        [MenuItem("Lush World/Setup/Add Player Stats & Bars")]
        public static void Run()
        {
            AddPlayerStatsToPrefab();
            AddStatsBarsToInventoryUI();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StatsSetupTool] Done — PlayerStats on PlayerCapsule, StatsUI bars above hotbar.");
        }

        // ── Step 1: PlayerStats component on PlayerCapsule ────────────────────

        private static void AddPlayerStatsToPrefab()
        {
            using var scope = new PrefabUtility.EditPrefabContentsScope(PlayerRigPath);
            var root = scope.prefabContentsRoot;

            var capsule = root.transform.Find("PlayerCapsule");
            if (capsule == null)
            {
                Debug.LogError("[StatsSetupTool] 'PlayerCapsule' not found inside PlayerRig.prefab");
                return;
            }

            if (capsule.GetComponent<LushWorld.Player.PlayerStats>() == null)
                capsule.gameObject.AddComponent<LushWorld.Player.PlayerStats>();
        }

        // ── Step 2: StatsRoot + health/hunger bars inside InventoryUI.prefab ─

        private static void AddStatsBarsToInventoryUI()
        {
            using var scope = new PrefabUtility.EditPrefabContentsScope(InventoryUIPath);
            var root = scope.prefabContentsRoot;

            // Canvas may be the root itself or a direct child
            var canvasTransform = root.GetComponent<Canvas>() != null
                ? root.transform
                : root.GetComponentInChildren<Canvas>(true)?.transform;

            if (canvasTransform == null)
            {
                Debug.LogError("[StatsSetupTool] No Canvas found inside InventoryUI.prefab");
                return;
            }

            if (canvasTransform.Find("StatsRoot") != null)
            {
                Debug.Log("[StatsSetupTool] StatsRoot already exists — skipping.");
                return;
            }

            // StatsRoot ─────────────────────────────────────────────────────────
            var statsRootGO  = new GameObject("StatsRoot");
            statsRootGO.transform.SetParent(canvasTransform, false);

            // Anchor bottom-center, sit above hotbar (adjust Y if hotbar moves)
            var rootRect = statsRootGO.AddComponent<RectTransform>();
            rootRect.anchorMin        = new Vector2(0.5f, 0f);
            rootRect.anchorMax        = new Vector2(0.5f, 0f);
            rootRect.pivot            = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 130f);
            rootRect.sizeDelta        = new Vector2(420f, 28f);

            var statsUI = statsRootGO.AddComponent<StatsUI>();

            // Health bar — left, red ─────────────────────────────────────────────
            var healthFill = CreateBar(statsRootGO.transform, "HealthBar",
                new Vector2(-107f, 0f), new Color(0.85f, 0.15f, 0.15f));

            // Hunger bar — right, orange ─────────────────────────────────────────
            var hungerFill = CreateBar(statsRootGO.transform, "HungerBar",
                new Vector2(107f, 0f), new Color(0.95f, 0.55f, 0.10f));

            // Wire private [SerializeField] Image refs ───────────────────────────
            var so = new SerializedObject(statsUI);
            so.FindProperty("_healthFill").objectReferenceValue = healthFill;
            so.FindProperty("_hungerFill").objectReferenceValue = hungerFill;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Bar builder ───────────────────────────────────────────────────────

        private static Image CreateBar(Transform parent, string barName, Vector2 position, Color fillColor)
        {
            // Dark background
            var bg     = new GameObject(barName);
            bg.transform.SetParent(parent, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchoredPosition = position;
            bgRect.sizeDelta        = new Vector2(190f, 24f);
            var bgImg  = bg.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            // Coloured fill (Filled / Horizontal so fillAmount drives the bar)
            var fill      = new GameObject("Fill");
            fill.transform.SetParent(bg.transform, false);
            var fillRect  = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            var fillImg   = fill.AddComponent<Image>();
            fillImg.color      = fillColor;
            fillImg.type       = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;

            return fillImg;
        }
    }
}
