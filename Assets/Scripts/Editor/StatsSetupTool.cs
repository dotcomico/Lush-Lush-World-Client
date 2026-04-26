using TMPro;
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

            var existing = canvasTransform.Find("StatsRoot");
            if (existing != null)
            {
                PatchTextLabels(existing);
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
            var healthFill  = CreateBar(statsRootGO.transform, "HealthBar",
                new Vector2(-107f, 0f), new Color(0.85f, 0.15f, 0.15f));
            var healthLabel = AddLabel(healthFill.transform.parent);

            // Hunger bar — right, orange ─────────────────────────────────────────
            var hungerFill  = CreateBar(statsRootGO.transform, "HungerBar",
                new Vector2(107f, 0f), new Color(0.95f, 0.55f, 0.10f));
            var hungerLabel = AddLabel(hungerFill.transform.parent);

            // Wire private [SerializeField] refs ─────────────────────────────────
            var so = new SerializedObject(statsUI);
            so.FindProperty("_healthFill").objectReferenceValue = healthFill;
            so.FindProperty("_hungerFill").objectReferenceValue = hungerFill;
            so.FindProperty("_healthText").objectReferenceValue = healthLabel;
            so.FindProperty("_hungerText").objectReferenceValue = hungerLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Text-label patcher (runs when StatsRoot already exists) ──────────

        private static void PatchTextLabels(Transform statsRoot)
        {
            var statsUI = statsRoot.GetComponent<StatsUI>();
            if (statsUI == null) return;

            var so             = new SerializedObject(statsUI);
            var healthTextProp = so.FindProperty("_healthText");
            var hungerTextProp = so.FindProperty("_hungerText");

            if (healthTextProp == null || hungerTextProp == null)
            {
                Debug.LogWarning("[StatsSetupTool] _healthText/_hungerText not found — recompile StatsUI first.");
                return;
            }

            if (healthTextProp.objectReferenceValue == null)
            {
                var bar = statsRoot.Find("HealthBar");
                if (bar != null) healthTextProp.objectReferenceValue = AddLabel(bar);
            }

            if (hungerTextProp.objectReferenceValue == null)
            {
                var bar = statsRoot.Find("HungerBar");
                if (bar != null) hungerTextProp.objectReferenceValue = AddLabel(bar);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[StatsSetupTool] Text labels patched into existing StatsRoot.");
        }

        private static TextMeshProUGUI AddLabel(Transform barRoot)
        {
            var existingGO = barRoot.Find("Label");
            if (existingGO != null) return existingGO.GetComponent<TextMeshProUGUI>();

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(barRoot, false);
            var rect       = labelGO.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp        = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.alignment  = TextAlignmentOptions.Center;
            tmp.fontSize   = 11f;
            tmp.color      = Color.white;
            tmp.text       = "100%";
            return tmp;
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
