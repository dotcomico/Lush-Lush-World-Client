using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using StarterAssets;
using LushWorld.Save;

namespace LushWorld.UI.Mobile
{
    // Scene singleton. Place on a persistent GO inside PlayerRig.
    // Manages HUD edit mode: shows a dimmer overlay, elevates mobile UI canvases
    // above the overlay, and presents a top-center control panel for the selected element.
    // Called from SettingsUIController ("Customize Mobile HUD" button, mobile only).
    public class MobileLayoutCustomizer : MonoBehaviour
    {
        public static MobileLayoutCustomizer Instance { get; private set; }

        private static readonly List<MobileUIElement> _registered = new();

        private bool    _editing;
        private Canvas  _overlayCanvas;
        private Canvas  _panelCanvas;

        private MobileUIElement _selected;
        private Slider          _sizeSlider;
        private Slider          _alphaSlider;
        private TextMeshProUGUI _sizeValLbl;
        private TextMeshProUGUI _alphaValLbl;
        private GameObject      _alphaRow;

        private readonly Dictionary<Canvas, int> _savedSortOrders = new();
        private readonly List<GameObject> _forcedActiveObjects = new();

        private StarterAssetsInputs _input;
        private PlayerInput         _gameInput;

        // ── Palette (matches SettingsUIController) ─────────────────────────────
        static readonly Color C_Bg     = new Color(0.080f, 0.080f, 0.098f, 0.97f);
        static readonly Color C_White  = new Color(0.940f, 0.940f, 0.960f, 1.00f);
        static readonly Color C_Gray   = new Color(0.520f, 0.520f, 0.560f, 1.00f);
        static readonly Color C_Accent = new Color(0.240f, 0.540f, 1.000f, 1.00f);
        static readonly Color C_Track  = new Color(0.200f, 0.200f, 0.240f, 1.00f);
        static readonly Color C_BtnBg  = new Color(0.170f, 0.170f, 0.210f, 1.00f);
        static readonly Color C_Green  = new Color(0.170f, 0.520f, 0.280f, 1.00f);
        static readonly Color C_Red    = new Color(0.550f, 0.130f, 0.110f, 1.00f);
        static readonly Color C_Dimmer = new Color(0.000f, 0.000f, 0.000f, 0.70f);

        // ── Registration (called by MobileUIElement.Awake / OnDestroy) ────────

        public static void Register(MobileUIElement el)
        {
            if (!_registered.Contains(el)) _registered.Add(el);
        }

        public static void Unregister(MobileUIElement el) => _registered.Remove(el);

        // Clear stale references between Editor play sessions
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ClearOnLoad() => _registered.Clear();

        // ── Lifecycle ──────────────────────────────────────────────────────────

        public static bool IsEditing => Instance != null && Instance._editing;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _input     = FindFirstObjectByType<StarterAssetsInputs>();
            _gameInput = FindFirstObjectByType<PlayerInput>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            MobileUIElement.OnSelected -= HandleElementSelected;
        }

        // Null out player input every frame while in edit mode so gameplay is blocked
        void Update()
        {
            if (!_editing || _input == null) return;
            _input.move   = Vector2.zero;
            _input.look   = Vector2.zero;
            _input.jump   = false;
            _input.sprint = false;
        }

        // ── Edit mode entry / exit ─────────────────────────────────────────────

        public void EnterEditMode()
        {
            if (_editing) return;
            _editing = true;

            MobileUIElement.OnSelected += HandleElementSelected;

            // Force-activate any GOs hidden via SetActive (e.g. MobileCanvasVisibility on desktop,
            // or platform-gated panels). Walk up to the Canvas boundary so we don't activate
            // unrelated scene GOs.
            foreach (var el in _registered)
            {
                var t = el.transform;
                while (t != null)
                {
                    if (!t.gameObject.activeSelf)
                    {
                        _forcedActiveObjects.Add(t.gameObject);
                        t.gameObject.SetActive(true);
                    }
                    if (t.GetComponent<Canvas>() != null) break;
                    t = t.parent;
                }
            }

            // Block PlayerInput action events (tongue attack, eating, etc.)
            if (_gameInput != null) _gameInput.enabled = false;

            // Raise each unique canvas above the overlay (overlay is at 500)
            foreach (var el in _registered)
            {
                var c = el.GetComponentInParent<Canvas>();
                if (c == null || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                if (_savedSortOrders.ContainsKey(c)) continue;
                _savedSortOrders[c] = c.sortingOrder;
                c.sortingOrder = 550;
            }

            BuildOverlay();       // sort 500 — dark dimmer, blocks 3D physics raycasts
            BuildControlPanel();  // sort 600 — top-center sliders + done button

            foreach (var el in _registered) el.EnterEditMode();

            if (_registered.Count > 0) HandleElementSelected(_registered[0]);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        public void ExitEditMode()
        {
            if (!_editing) return;
            _editing = false;

            MobileUIElement.OnSelected -= HandleElementSelected;

            foreach (var el in _registered) el.ExitEditMode();

            foreach (var kvp in _savedSortOrders)
                if (kvp.Key != null) kvp.Key.sortingOrder = kvp.Value;
            _savedSortOrders.Clear();

            // Restore GOs that were force-activated on entry
            foreach (var go in _forcedActiveObjects)
                if (go != null) go.SetActive(false);
            _forcedActiveObjects.Clear();

            // Re-enable player input action events
            if (_gameInput != null) _gameInput.enabled = true;

            if (_overlayCanvas != null) { Destroy(_overlayCanvas.gameObject); _overlayCanvas = null; }
            if (_panelCanvas   != null) { Destroy(_panelCanvas.gameObject);   _panelCanvas   = null; }
            _selected = null;

            SaveManager.Instance?.SaveSettings();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        public void ResetAll()
        {
            foreach (var el in _registered) el.ResetToDefault();
            if (_selected != null) RefreshPanel();
        }

        // ── Layout data (called from SettingsUIController) ─────────────────────

        public List<MobileElementLayoutData> GetAllLayoutData()
        {
            var list = new List<MobileElementLayoutData>();
            foreach (var el in _registered) list.Add(el.GetCurrentLayout());
            return list;
        }

        public void ApplySavedLayout(List<MobileElementLayoutData> layout)
        {
            if (layout == null) return;
            foreach (var d in layout)
                foreach (var el in _registered)
                    if (el.ElementId == d.elementId) { el.ApplyLayout(d); break; }
        }

        // ── Selection ──────────────────────────────────────────────────────────

        void HandleElementSelected(MobileUIElement el)
        {
            if (_selected != null) _selected.SetSelected(false);
            _selected = el;
            if (_selected != null) _selected.SetSelected(true);
            RefreshPanel();
        }

        void RefreshPanel()
        {
            if (_selected == null || _sizeSlider == null) return;

            _sizeSlider.SetValueWithoutNotify(_selected.CurrentScale);
            _sizeValLbl.text = _selected.CurrentScale.ToString("F2") + "×";

            bool supportsAlpha = _selected.SupportsAlpha;
            _alphaRow?.SetActive(supportsAlpha);
            if (supportsAlpha && _alphaSlider != null)
            {
                _alphaSlider.SetValueWithoutNotify(_selected.CurrentAlpha);
                _alphaValLbl.text = Mathf.RoundToInt(_selected.CurrentAlpha * 100) + "%";
            }
        }

        // ── Overlay ────────────────────────────────────────────────────────────

        void BuildOverlay()
        {
            var go = new GameObject("MobileHUD_Overlay",
                typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler));
            _overlayCanvas = go.GetComponent<Canvas>();
            _overlayCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = 500;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight   = 0.5f;

            var dimmer = MakeGO("Dimmer", go.transform);
            var dRT    = dimmer.GetComponent<RectTransform>();
            dRT.anchorMin = Vector2.zero;
            dRT.anchorMax = Vector2.one;
            dRT.offsetMin = dRT.offsetMax = Vector2.zero;
            var img = dimmer.AddComponent<Image>();
            img.color         = C_Dimmer;
            img.raycastTarget = true; // blocks physics raycasts below overlay
        }

        // ── Control panel ──────────────────────────────────────────────────────

        void BuildControlPanel()
        {
            var go = new GameObject("MobileHUD_Panel",
                typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler));
            _panelCanvas = go.GetComponent<Canvas>();
            _panelCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _panelCanvas.sortingOrder = 600;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight   = 0.5f;

            // Panel anchored top-center, hangs down
            var panel  = MakeGO("Panel", go.transform);
            var pRT    = panel.GetComponent<RectTransform>();
            pRT.anchorMin        = new Vector2(0.5f, 1f);
            pRT.anchorMax        = new Vector2(0.5f, 1f);
            pRT.pivot            = new Vector2(0.5f, 1f);
            pRT.sizeDelta        = new Vector2(480f, 0f);
            pRT.anchoredPosition = new Vector2(0f, -8f);
            panel.AddComponent<Image>().color = C_Bg;

            var vg = panel.AddComponent<VerticalLayoutGroup>();
            vg.padding            = new RectOffset(18, 18, 14, 16);
            vg.spacing            = 10;
            vg.childControlWidth  = true;  vg.childForceExpandWidth  = true;
            vg.childControlHeight = false; vg.childForceExpandHeight = false;

            panel.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            // Title
            Lbl(panel.transform, "CUSTOMIZE MOBILE HUD", 17, C_Accent,
                FontStyles.Bold, TextAlignmentOptions.Center, prefH: 28);

            // Size row
            var sizeRow = HRow(panel.transform, 36);
            Lbl(sizeRow.transform, "Size", 16, C_Gray,
                align: TextAlignmentOptions.Left, prefW: 74);
            _sizeSlider = MakeSlider(sizeRow.transform, 0.5f, 2f, 1f, v =>
            {
                _selected?.SetScale(v);
                if (_sizeValLbl != null) _sizeValLbl.text = v.ToString("F2") + "×";
            });
            LE(_sizeSlider.gameObject, flexW: 1);
            _sizeValLbl = Lbl(sizeRow.transform, "1.00×", 15, C_White,
                align: TextAlignmentOptions.Right, prefW: 64);

            // Opacity row (hidden for elements that share an external CanvasGroup)
            _alphaRow = HRow(panel.transform, 36);
            Lbl(_alphaRow.transform, "Opacity", 16, C_Gray,
                align: TextAlignmentOptions.Left, prefW: 74);
            _alphaSlider = MakeSlider(_alphaRow.transform, 0.1f, 1f, 1f, v =>
            {
                if (_selected == null || !_selected.SupportsAlpha) return;
                _selected.SetAlpha(v);
                if (_alphaValLbl != null) _alphaValLbl.text = Mathf.RoundToInt(v * 100) + "%";
            });
            LE(_alphaSlider.gameObject, flexW: 1);
            _alphaValLbl = Lbl(_alphaRow.transform, "100%", 15, C_White,
                align: TextAlignmentOptions.Right, prefW: 64);

            // Reset selected element
            var resetBtn = Btn(panel.transform, "Reset Element", C_BtnBg, C_White, 16, 40, () =>
            {
                _selected?.ResetToDefault();
                RefreshPanel();
            });
            LE(resetBtn.gameObject, prefH: 40, flexW: 1);

            // Bottom row: Reset All | Done
            var bottom = HRow(panel.transform, 46);
            var btnResetAll = Btn(bottom.transform, "Reset All", C_Red, C_White, 15, 46, ResetAll);
            LE(btnResetAll.gameObject, flexW: 1);

            var gap = MakeGO("Gap", bottom.transform);
            LE(gap, prefW: 10, flexW: 0);

            var btnDone = Btn(bottom.transform, "Done  ✓", C_Green, C_White, 15, 46, ExitEditMode);
            LE(btnDone.gameObject, flexW: 1);
        }

        // ── UI factory helpers ─────────────────────────────────────────────────

        static GameObject MakeGO(string name, Transform parent)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            return g;
        }

        static void LE(GameObject go, float prefW = 0, float flexW = 1, float prefH = 0)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (prefW > 0) { le.preferredWidth = prefW; le.flexibleWidth = 0; }
            else             le.flexibleWidth = flexW;
            if (prefH > 0)   le.preferredHeight = prefH;
        }

        static TextMeshProUGUI Lbl(Transform parent, string text, int size, Color color,
            FontStyles style = FontStyles.Normal,
            TextAlignmentOptions align = TextAlignmentOptions.Left,
            float prefW = 0, float prefH = 0)
        {
            var go = MakeGO("T", parent);
            var t  = go.AddComponent<TextMeshProUGUI>();
            t.text         = text;
            t.fontSize     = size;
            t.color        = color;
            t.fontStyle    = style;
            t.alignment    = align;
            t.overflowMode = TextOverflowModes.Overflow;
            LE(go, prefW: prefW, flexW: prefW > 0 ? 0 : 1, prefH: prefH);
            return t;
        }

        static GameObject HRow(Transform parent, float h)
        {
            var go = MakeGO("Row", parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, h);
            var hg = go.AddComponent<HorizontalLayoutGroup>();
            hg.spacing            = 6;
            hg.childAlignment     = TextAnchor.MiddleLeft;
            hg.childControlWidth  = true;  hg.childForceExpandWidth  = false;
            hg.childControlHeight = true;  hg.childForceExpandHeight = true;
            LE(go, prefH: h, flexW: 1);
            return go;
        }

        static Slider MakeSlider(Transform parent, float min, float max, float value,
            UnityEngine.Events.UnityAction<float> onChange)
        {
            var go = MakeGO("Sl", parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 28);

            var bgGO = MakeGO("Bg", go.transform);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0.30f); bgRT.anchorMax = new Vector2(1f, 0.70f);
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            bgGO.AddComponent<Image>().color = C_Track;

            var faGO = MakeGO("FA", go.transform);
            var faRT = faGO.GetComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0f, 0.30f); faRT.anchorMax = new Vector2(1f, 0.70f);
            faRT.offsetMin = new Vector2(5f, 0f); faRT.offsetMax = new Vector2(-15f, 0f);

            var fGO = MakeGO("F", faGO.transform);
            var fRT = fGO.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = new Vector2(0f, 1f);
            fRT.offsetMin = fRT.offsetMax = Vector2.zero;
            fGO.AddComponent<Image>().color = C_Accent;

            var haGO = MakeGO("HA", go.transform);
            var haRT = haGO.GetComponent<RectTransform>();
            haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
            haRT.offsetMin = new Vector2(10f, 0f); haRT.offsetMax = new Vector2(-10f, 0f);

            var hGO  = MakeGO("H", haGO.transform);
            var hRT  = hGO.GetComponent<RectTransform>();
            hRT.anchorMin = hRT.anchorMax = new Vector2(0f, 0.5f);
            hRT.sizeDelta = new Vector2(20f, 20f);
            var hImg = hGO.AddComponent<Image>();
            hImg.color = C_White;

            var sl = go.AddComponent<Slider>();
            sl.fillRect      = fRT;
            sl.handleRect    = hRT;
            sl.targetGraphic = hImg;
            sl.direction     = Slider.Direction.LeftToRight;
            sl.minValue      = min;
            sl.maxValue      = max;
            sl.value         = value;
            sl.onValueChanged.AddListener(onChange);
            return sl;
        }

        static Button Btn(Transform parent, string label, Color bg, Color textColor,
            int fontSize, float height, System.Action onClick)
        {
            var go  = MakeGO("Btn_" + label, parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn   = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cols = btn.colors;
            cols.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            cols.pressedColor     = new Color(0.7f, 0.7f, 0.7f, 1f);
            btn.colors = cols;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var lbl = MakeGO("L", go.transform);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text      = label;
            t.fontSize  = fontSize;
            t.color     = textColor;
            t.alignment = TextAlignmentOptions.Center;
            return btn;
        }
    }
}
