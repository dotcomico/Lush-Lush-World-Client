using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using LushWorld.Camera;
using LushWorld.Player;
using StarterAssets;

namespace LushWorld.UI
{
    /// <summary>
    /// Builds and manages the in-game Settings panel entirely in code.
    /// Requires: GearButton child (optional) to open; Escape key always toggles.
    /// Auto-discovers SlideController, CameraViewController, FirstPersonController,
    /// ThirdPersonOrbitController from the scene.
    /// </summary>
    public class SettingsUIController : MonoBehaviour
    {
        // ── Discovered scene references ───────────────────────────────────────
        private SlideController            _slide;
        private CameraViewController       _camView;
        private FirstPersonController      _fpc;
        private ThirdPersonOrbitController _orbit;
        private CinemachineVirtualCamera   _fpVCam;
        private Cinemachine3rdPersonFollow _tpFollow;
        private StarterAssetsInputs        _inputBridge;

        // ── Live UI references ────────────────────────────────────────────────
        private GameObject       _panel;
        private TextMeshProUGUI  _camModeLabel;
        private TextMeshProUGUI  _slideModeLabel;
        private TextMeshProUGUI  _tpDistLabel;
        private TextMeshProUGUI  _fpFovLabel;
        private TextMeshProUGUI  _sensLabel;

        // ── State ─────────────────────────────────────────────────────────────
        private int _camModeIdx;
        private int _slideModeIdx;

        // ── Palette (dark gaming theme) ───────────────────────────────────────
        static readonly Color C_Bg        = new Color(0.080f, 0.080f, 0.098f, 0.97f);
        static readonly Color C_Section   = new Color(0.112f, 0.112f, 0.145f, 1.00f);
        static readonly Color C_Accent    = new Color(0.240f, 0.540f, 1.000f, 1.00f);
        static readonly Color C_White     = new Color(0.940f, 0.940f, 0.960f, 1.00f);
        static readonly Color C_Gray      = new Color(0.520f, 0.520f, 0.560f, 1.00f);
        static readonly Color C_Track     = new Color(0.200f, 0.200f, 0.240f, 1.00f);
        static readonly Color C_BtnBg     = new Color(0.170f, 0.170f, 0.210f, 1.00f);
        static readonly Color C_Dimmer    = new Color(0.000f, 0.000f, 0.000f, 0.55f);

        static readonly string[] CamModeNames   = { "First Person", "Third Person", "Isometric" };
        static readonly string[] SlideModeNames = { "None", "Medium", "Cinematic" };

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _slide        = FindFirstObjectByType<SlideController>();
            _camView      = FindFirstObjectByType<CameraViewController>();
            _fpc          = FindFirstObjectByType<FirstPersonController>();
            _orbit        = FindFirstObjectByType<ThirdPersonOrbitController>();
            _inputBridge  = FindFirstObjectByType<StarterAssetsInputs>();

            if (_camView != null)
            {
                if (_camView.FirstPersonCameraGO != null)
                    _fpVCam = _camView.FirstPersonCameraGO.GetComponent<CinemachineVirtualCamera>();

                if (_camView.ThirdPersonCameraGO != null)
                {
                    var tpVCam = _camView.ThirdPersonCameraGO.GetComponent<CinemachineVirtualCamera>();
                    if (tpVCam != null)
                        _tpFollow = tpVCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                }
            }

            BuildPanel();

            var gearBtn = transform.Find("GearButton")?.GetComponent<Button>();
            if (gearBtn != null) gearBtn.onClick.AddListener(TogglePanel);
        }

        private void Start()
        {
            _panel.SetActive(false);
            SyncAll();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                TogglePanel();
#else
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePanel();
#endif
        }

        public void TogglePanel()
        {
            bool opening = !_panel.activeSelf;
            _panel.SetActive(opening);

            if (opening)
            {
                // Free the cursor so the player can click settings controls
                if (_inputBridge != null) { _inputBridge.cursorLocked = false; _inputBridge.cursorInputForLook = false; }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
                SyncAll();
            }
            else
            {
                // Re-lock cursor so camera look resumes
                if (_inputBridge != null) { _inputBridge.cursorLocked = true; _inputBridge.cursorInputForLook = true; }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        // ── Sync (game state → UI) ────────────────────────────────────────────

        private void SyncAll()
        {
            if (_camView != null)
            {
                _camModeIdx = (int)_camView.CurrentMode;
                if (_camModeLabel != null) _camModeLabel.text = CamModeNames[_camModeIdx];
            }
            if (_slide != null)
            {
                _slideModeIdx = (int)_slide.CameraMode;
                if (_slideModeLabel != null) _slideModeLabel.text = SlideModeNames[_slideModeIdx];
            }
        }

        // ── Setting handlers ──────────────────────────────────────────────────

        void StepCamMode(int dir)
        {
            _camModeIdx = (_camModeIdx + dir + 3) % 3;
            if (_camModeLabel != null) _camModeLabel.text = CamModeNames[_camModeIdx];
            _camView?.SetMode((LushWorld.Camera.CameraMode)_camModeIdx);
        }

        void StepSlideMode(int dir)
        {
            _slideModeIdx = (_slideModeIdx + dir + 3) % 3;
            if (_slideModeLabel != null) _slideModeLabel.text = SlideModeNames[_slideModeIdx];
            if (_slide != null) _slide.CameraMode = (SlideCameraMode)_slideModeIdx;
        }

        void OnTPDist(float v)
        {
            if (_tpFollow != null) _tpFollow.CameraDistance = v;
            if (_tpDistLabel != null) _tpDistLabel.text = v.ToString("F1") + " m";
        }

        void OnFPFov(float v)
        {
            if (_fpVCam != null)
            {
                var lens = _fpVCam.m_Lens;
                lens.FieldOfView = v;
                _fpVCam.m_Lens = lens;
            }
            if (_fpFovLabel != null) _fpFovLabel.text = v.ToString("F0") + "\u00b0";
        }

        void OnSensitivity(float v)
        {
            if (_fpc != null) _fpc.RotationSpeed = v;
            if (_orbit != null)
            {
                _orbit.HorizontalSensitivity = v;
                _orbit.VerticalSensitivity   = v;
            }
            if (_sensLabel != null) _sensLabel.text = v.ToString("F2") + "\u00d7";
        }

        // ── Panel builder ─────────────────────────────────────────────────────

        void BuildPanel()
        {
            // Replace any stale scene panel left over from the editor
            var old = transform.Find("SettingsPanel");
            if (old != null) DestroyImmediate(old.gameObject);

            _panel = Go("SettingsPanel", transform);

            // Full-screen dimmer (blocks click-through to game world)
            Stretch(RT(_panel));
            _panel.AddComponent<Image>().color = C_Dimmer;

            // ── Card ─────────────────────────────────────────────────────────
            var card = Go("Card", _panel.transform);
            var cardRT = RT(card);
            cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(740, 490);
            cardRT.anchoredPosition = Vector2.zero;
            card.AddComponent<Image>().color = C_Bg;

            var vg = card.AddComponent<VerticalLayoutGroup>();
            vg.padding = new RectOffset(34, 34, 22, 22);
            vg.spacing = 9;
            vg.childControlWidth  = true;  vg.childForceExpandWidth  = true;
            vg.childControlHeight = false; vg.childForceExpandHeight = false;

            // ── Title row ────────────────────────────────────────────────────
            var titleRow = HRow(card.transform, 46, 6);
            Lbl(titleRow.transform, "SETTINGS", 20, C_White, FontStyles.Bold,
                TextAlignmentOptions.Left, flexW: 1);
            var closeBtn = IconBtn(titleRow.transform, "\u2715", 38, 38, C_BtnBg, C_Gray, 16);
            closeBtn.onClick.AddListener(TogglePanel);
            LE(closeBtn.gameObject, prefW: 38);

            Separator(card.transform);

            // ── Section: CAMERA VIEW ─────────────────────────────────────────
            SectionHdr(card.transform, "CAMERA VIEW");

            var cmRow = SettingRow(card.transform, "Camera Mode");
            _camModeLabel = Cycler(cmRow.transform, CamModeNames, 0, StepCamMode);

            var slRow = SettingRow(card.transform, "Slide Effect");
            _slideModeLabel = Cycler(slRow.transform, SlideModeNames, 0, StepSlideMode);

            // ── Section: CAMERA TUNING ───────────────────────────────────────
            SectionHdr(card.transform, "CAMERA TUNING");

            float initDist = _tpFollow != null ? _tpFollow.CameraDistance : 5f;
            var tdRow = SettingRow(card.transform, "TP Distance");
            _tpDistLabel = SliderRow(tdRow.transform, 2f, 10f, initDist, OnTPDist,
                initDist.ToString("F1") + " m");

            float initFov = _fpVCam != null ? _fpVCam.m_Lens.FieldOfView : 80f;
            var fovRow = SettingRow(card.transform, "FP Field of View");
            _fpFovLabel = SliderRow(fovRow.transform, 60f, 110f, initFov, OnFPFov,
                initFov.ToString("F0") + "\u00b0");

            // ── Section: CONTROLS ────────────────────────────────────────────
            SectionHdr(card.transform, "CONTROLS");

            float initSens = _fpc != null ? _fpc.RotationSpeed : 1f;
            var sensRow = SettingRow(card.transform, "Look Sensitivity");
            _sensLabel = SliderRow(sensRow.transform, 0.2f, 3f, initSens, OnSensitivity,
                initSens.ToString("F2") + "\u00d7");
        }

        // ── UI factory methods ────────────────────────────────────────────────

        static GameObject Go(string name, Transform parent)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            return g;
        }

        static RectTransform RT(GameObject go) => go.GetComponent<RectTransform>();

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static LayoutElement LE(GameObject go, float prefW = 0, float flexW = 1, float prefH = 0)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (prefW > 0) { le.preferredWidth = prefW; le.flexibleWidth = 0; }
            else             le.flexibleWidth = flexW;
            if (prefH > 0)   le.preferredHeight = prefH;
            return le;
        }

        static GameObject HRow(Transform parent, float h, float spacing = 4)
        {
            var go = Go("Row", parent);
            RT(go).sizeDelta = new Vector2(0, h);
            var hg = go.AddComponent<HorizontalLayoutGroup>();
            hg.spacing = spacing;
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childControlWidth  = false; hg.childForceExpandWidth  = false;
            hg.childControlHeight = true;  hg.childForceExpandHeight = true;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = h; le.flexibleWidth = 1;
            return go;
        }

        static GameObject SettingRow(Transform parent, string label)
        {
            var row = HRow(parent, 38, 10);
            Lbl(row.transform, label, 13, C_Gray, FontStyles.Normal,
                TextAlignmentOptions.Left, prefW: 210);
            return row;
        }

        static TextMeshProUGUI Lbl(Transform parent, string text, int size, Color color,
                                    FontStyles style = FontStyles.Normal,
                                    TextAlignmentOptions align = TextAlignmentOptions.Left,
                                    float prefW = 0, float flexW = 1)
        {
            var go = Go("T_" + text, parent);
            RT(go).sizeDelta = new Vector2(prefW > 0 ? prefW : 120, 28);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color;
            t.alignment = align; t.fontStyle = style;
            t.overflowMode = TextOverflowModes.Overflow;
            if (prefW > 0) LE(go, prefW: prefW);
            else           LE(go, flexW: flexW);
            return t;
        }

        static void SectionHdr(Transform parent, string title)
        {
            var go = Go("Sec_" + title, parent);
            RT(go).sizeDelta = new Vector2(0, 28);
            go.AddComponent<Image>().color = C_Section;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 28; le.flexibleWidth = 1;

            var inner = Go("Lbl", go.transform);
            var rt = RT(inner);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12, 0); rt.offsetMax = Vector2.zero;
            var t = inner.AddComponent<TextMeshProUGUI>();
            t.text = title; t.fontSize = 10; t.color = C_Accent;
            t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Left;
        }

        static void Separator(Transform parent)
        {
            var go = Go("Sep", parent);
            RT(go).sizeDelta = new Vector2(0, 1);
            go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 1; le.flexibleWidth = 1;
        }

        // ◄ Value ► cycler — returns the center value label for later sync
        static TextMeshProUGUI Cycler(Transform parent, string[] opts, int startIdx,
                                       System.Action<int> step)
        {
            var left = IconBtn(parent, "\u25c4", 30, 30, C_BtnBg, C_White, 13);
            left.onClick.AddListener(() => step(-1));

            var valGO = Go("Val", parent);
            RT(valGO).sizeDelta = new Vector2(170, 30);
            var t = valGO.AddComponent<TextMeshProUGUI>();
            t.text = opts[startIdx]; t.fontSize = 14; t.color = C_White;
            t.alignment = TextAlignmentOptions.Center;
            t.overflowMode = TextOverflowModes.Overflow;
            LE(valGO, flexW: 1);

            var right = IconBtn(parent, "\u25ba", 30, 30, C_BtnBg, C_White, 13);
            right.onClick.AddListener(() => step(1));

            return t;
        }

        // Slider + right-aligned value readout — returns the readout label
        static TextMeshProUGUI SliderRow(Transform parent, float min, float max, float val,
                                          UnityEngine.Events.UnityAction<float> onChange,
                                          string initText)
        {
            var slider = BuildSlider(parent, min, max, val);
            slider.onValueChanged.AddListener(onChange);
            LE(slider.gameObject, flexW: 1);

            var valGO = Go("Val", parent);
            RT(valGO).sizeDelta = new Vector2(70, 28);
            var t = valGO.AddComponent<TextMeshProUGUI>();
            t.text = initText; t.fontSize = 13; t.color = C_White;
            t.alignment = TextAlignmentOptions.Right;
            t.overflowMode = TextOverflowModes.Overflow;
            LE(valGO, prefW: 70);
            return t;
        }

        static Button IconBtn(Transform parent, string icon, float w, float h,
                               Color bg, Color textColor, int fontSize)
        {
            var go = Go("Btn", parent);
            RT(go).sizeDelta = new Vector2(w, h);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cols = btn.colors;
            cols.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cols.pressedColor     = new Color(0.70f, 0.70f, 0.70f, 1f);
            btn.colors = cols;
            LE(go, prefW: w);

            var lbl = Go("L", go.transform);
            var rt = RT(lbl);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = icon; t.fontSize = fontSize; t.color = textColor;
            t.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        // Builds a functional Unity Slider from scratch
        static Slider BuildSlider(Transform parent, float min, float max, float value)
        {
            var go = Go("Slider", parent);
            RT(go).sizeDelta = new Vector2(300, 28);

            // Background track
            var bg = Go("Bg", go.transform);
            var bgRT = RT(bg);
            bgRT.anchorMin = new Vector2(0f, 0.30f);
            bgRT.anchorMax = new Vector2(1f, 0.70f);
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = C_Track;

            // Fill area + fill
            var fillArea = Go("FillArea", go.transform);
            var faRT = RT(fillArea);
            faRT.anchorMin = new Vector2(0f, 0.30f);
            faRT.anchorMax = new Vector2(1f, 0.70f);
            faRT.offsetMin = new Vector2(5f,   0f);
            faRT.offsetMax = new Vector2(-15f, 0f);

            var fill   = Go("Fill", fillArea.transform);
            var fillRT = RT(fill);
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = C_Accent;

            // Handle slide area + handle
            var hsa   = Go("HandleArea", go.transform);
            var hsaRT = RT(hsa);
            hsaRT.anchorMin = Vector2.zero;
            hsaRT.anchorMax = Vector2.one;
            hsaRT.offsetMin = new Vector2(10f,   0f);
            hsaRT.offsetMax = new Vector2(-10f, 0f);

            var handle    = Go("Handle", hsa.transform);
            var handleRT  = RT(handle);
            handleRT.anchorMin = handleRT.anchorMax = new Vector2(0f, 0.5f);
            handleRT.sizeDelta = new Vector2(20f, 20f);
            var handleImg  = handle.AddComponent<Image>();
            handleImg.color = C_White;

            var slider = go.AddComponent<Slider>();
            slider.fillRect      = fillRT;
            slider.handleRect    = handleRT;
            slider.targetGraphic = handleImg;
            slider.direction     = Slider.Direction.LeftToRight;
            slider.minValue      = min;
            slider.maxValue      = max;
            slider.value         = value;

            return slider;
        }
    }
}
