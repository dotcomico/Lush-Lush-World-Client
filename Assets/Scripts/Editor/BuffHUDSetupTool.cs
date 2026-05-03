using LushWorld.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.Editor
{
    // One-shot editor tool: builds the Gummy Rush buff countdown panel inside InventoryUI.prefab.
    // Run once via menu: Lush World > Setup > Add Buff HUD
    public static class BuffHUDSetupTool
    {
        private const string InventoryUIPath = "Assets/App/Prefabs/InventoryUI.prefab";

        // Panel dimensions and anchor (top-right corner, above hotbar area)
        private static readonly Vector2 PanelSize         = new(110f, 72f);
        private static readonly Vector2 PanelAnchoredPos  = new(-66f, -50f); // offset from top-right corner
        private static readonly Color   PanelBgColor      = new(0.08f, 0.05f, 0.12f, 0.82f); // dark purple
        private static readonly Color   GummyPink         = new(1f,   0.45f, 0.70f, 1f);

        [MenuItem("Lush World/Setup/Add Buff HUD")]
        public static void Run()
        {
            using var scope = new PrefabUtility.EditPrefabContentsScope(InventoryUIPath);
            var root = scope.prefabContentsRoot;

            var canvasTransform = root.GetComponent<Canvas>() != null
                ? root.transform
                : root.GetComponentInChildren<Canvas>(true)?.transform;

            if (canvasTransform == null)
            {
                Debug.LogError("[BuffHUDSetupTool] No Canvas found inside InventoryUI.prefab");
                return;
            }

            // Idempotent — skip if already present
            if (canvasTransform.Find("BuffHUD") != null)
            {
                Debug.Log("[BuffHUDSetupTool] BuffHUD already exists — nothing to do.");
                return;
            }

            // ── Root panel ───────────────────────────────────────────────────────
            var panelGO   = new GameObject("BuffHUD");
            panelGO.transform.SetParent(canvasTransform, false);

            var panelRect            = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin      = new Vector2(1f, 1f); // top-right
            panelRect.anchorMax      = new Vector2(1f, 1f);
            panelRect.pivot          = new Vector2(1f, 1f);
            panelRect.anchoredPosition = PanelAnchoredPos;
            panelRect.sizeDelta      = PanelSize;

            var panelImg   = panelGO.AddComponent<Image>();
            panelImg.color = PanelBgColor;

            // ── Candy-color top bar (visual accent) ──────────────────────────────
            var accentGO   = new GameObject("AccentBar");
            accentGO.transform.SetParent(panelGO.transform, false);
            var accentRect           = accentGO.AddComponent<RectTransform>();
            accentRect.anchorMin     = new Vector2(0f, 1f);
            accentRect.anchorMax     = new Vector2(1f, 1f);
            accentRect.pivot         = new Vector2(0.5f, 1f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta     = new Vector2(0f, 4f);
            var accentImg    = accentGO.AddComponent<Image>();
            accentImg.color  = GummyPink;

            // ── Label: "GUMMY RUSH" ───────────────────────────────────────────────
            var labelGO   = new GameObject("TitleText");
            labelGO.transform.SetParent(panelGO.transform, false);
            var labelRect             = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin       = new Vector2(0f, 0.55f);
            labelRect.anchorMax       = new Vector2(1f, 1f);
            labelRect.offsetMin       = new Vector2(4f, 0f);
            labelRect.offsetMax       = new Vector2(-4f, -6f);
            var labelTMP              = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text             = "GUMMY RUSH";
            labelTMP.fontSize         = 10f;
            labelTMP.fontStyle        = FontStyles.Bold;
            labelTMP.alignment        = TextAlignmentOptions.Center;
            labelTMP.color            = GummyPink;

            // ── Timer countdown text ─────────────────────────────────────────────
            var timerGO   = new GameObject("TimerText");
            timerGO.transform.SetParent(panelGO.transform, false);
            var timerRect             = timerGO.AddComponent<RectTransform>();
            timerRect.anchorMin       = new Vector2(0f, 0f);
            timerRect.anchorMax       = new Vector2(1f, 0.55f);
            timerRect.offsetMin       = new Vector2(4f, 4f);
            timerRect.offsetMax       = new Vector2(-4f, 0f);
            var timerTMP              = timerGO.AddComponent<TextMeshProUGUI>();
            timerTMP.text             = "30s";
            timerTMP.fontSize         = 22f;
            timerTMP.fontStyle        = FontStyles.Bold;
            timerTMP.alignment        = TextAlignmentOptions.Center;
            timerTMP.color            = Color.white;

            // ── BuffHUD component — wire refs via SerializedObject ────────────────
            var buffHud = panelGO.AddComponent<BuffHUD>();
            var so      = new SerializedObject(buffHud);
            so.FindProperty("_panel").objectReferenceValue      = panelGO;
            so.FindProperty("_timerText").objectReferenceValue  = timerTMP;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Hide at start — BuffHUD.Awake will also call SetActive(false),
            // but setting it here keeps the prefab clean in the Editor
            panelGO.SetActive(false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BuffHUDSetupTool] BuffHUD panel created inside InventoryUI.prefab.");
        }
    }
}
