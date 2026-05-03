using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Unity.Cinemachine;
using LushWorld.Save;
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
    [RequireComponent(typeof(UnityEngine.UI.GraphicRaycaster))]
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
        public static bool IsOpen { get; private set; }
        private int   _camModeIdx;
        private int   _slideModeIdx;
        private Slider _tpDistSlider;
        private Slider _fpFovSlider;
        private Slider _sensSlider;
        private bool   _resetGamePending;
        private float  _resetGamePendingTime;
        private TextMeshProUGUI _resetGameBtnText;

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
            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var _canvas = GetComponent<Canvas>();
            if (_canvas != null) _canvas.sortingOrder = 200;

            // Scale UI with screen resolution, matching the Building/Crafting menus.
            var scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            DiscoverSceneReferences();
            SetupCinemachineRefs();
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
            if (_resetGamePending && Time.time - _resetGamePendingTime > 3f)
            {
                _resetGamePending = false;
                if (_resetGameBtnText != null) _resetGameBtnText.text = "Delete Save Data";
            }
        }

        public void TogglePanel()
        {
            bool opening = !_panel.activeSelf;
            IsOpen = opening;
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

        // ── Sync (game state -> UI) ───────────────────────────────────────────

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
            LushWorld.Save.SaveManager.Instance?.SaveSettings();
        }

        void StepSlideMode(int dir)
        {
            _slideModeIdx = (_slideModeIdx + dir + 3) % 3;
            if (_slideModeLabel != null) _slideModeLabel.text = SlideModeNames[_slideModeIdx];
            if (_slide != null) _slide.CameraMode = (SlideCameraMode)_slideModeIdx;
            LushWorld.Save.SaveManager.Instance?.SaveSettings();
        }

        void OnTPDist(float v)
        {
            if (_tpFollow != null) _tpFollow.CameraDistance = v;
            if (_tpDistLabel != null) _tpDistLabel.text = v.ToString("F1") + " m";
            LushWorld.Save.SaveManager.Instance?.SaveSettings();
        }

        void OnFPFov(float v)
        {
            if (_fpVCam != null)
            {
                var lens = _fpVCam.m_Lens;
                lens.FieldOfView = v;
                _fpVCam.m_Lens = lens;
            }
            if (_fpFovLabel != null) _fpFovLabel.text = v.ToString("F0") + "°";
            LushWorld.Save.SaveManager.Instance?.SaveSettings();
        }

        void OnSensitivity(float v)
        {
            if (_fpc != null) _fpc.RotationSpeed = v;
            if (_orbit != null)
            {
                _orbit.HorizontalSensitivity = v;
                _orbit.VerticalSensitivity   = v;
            }
            if (_sensLabel != null) _sensLabel.text = v.ToString("F2") + "×";
            LushWorld.Save.SaveManager.Instance?.SaveSettings();
        }

        private void DiscoverSceneReferences()
        {
            _slide       = FindFirstObjectByType<SlideController>();
            _camView     = FindFirstObjectByType<CameraViewController>();
            _fpc         = FindFirstObjectByType<FirstPersonController>();
            _orbit       = FindFirstObjectByType<ThirdPersonOrbitController>();
            _inputBridge = FindFirstObjectByType<StarterAssetsInputs>();
        }

        private void SetupCinemachineRefs()
        {
            if (_camView == null) return;

            if (_camView.FirstPersonCameraGO != null)
                _fpVCam = _camView.FirstPersonCameraGO.GetComponent<CinemachineVirtualCamera>();

            if (_camView.ThirdPersonCameraGO != null)
            {
                var tpVCam = _camView.ThirdPersonCameraGO.GetComponent<CinemachineVirtualCamera>();
                if (tpVCam != null)
                    _tpFollow = tpVCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            }
        }

        // ── Panel builder ─────────────────────────────────────────────────────

        void BuildPanel()
        {
            var old = transform.Find("SettingsPanel");
            if (old != null) DestroyImmediate(old.gameObject);

            _panel = Go("SettingsPanel", transform);
            Stretch(RT(_panel));

            // Full-screen dimmer backdrop; clicking it closes the panel.
            // ScrollBlocker ensures scroll wheel events over the dimmer don't leak to the game.
            var dimmer = Go("Dimmer", _panel.transform);
            Stretch(RT(dimmer));
            dimmer.AddComponent<Image>().color = C_Dimmer;
            var dimmerBtn = dimmer.AddComponent<Button>();
            dimmerBtn.transition = Selectable.Transition.None;
            dimmerBtn.onClick.AddListener(TogglePanel);
            dimmer.AddComponent<ScrollBlocker>();

            // Card: fills 80% width x 88% height, centered, scales with CanvasScaler.
            var card = Go("Card", _panel.transform);
            var cardRT = RT(card);
            cardRT.anchorMin        = new Vector2(0.10f, 0.06f);
            cardRT.anchorMax        = new Vector2(0.90f, 0.94f);
            cardRT.sizeDelta        = Vector2.zero;
            cardRT.anchoredPosition = Vector2.zero;
            card.AddComponent<Image>().color = C_Bg;

            // ── Fixed title strip pinned to top of card ───────────────────────
            const float TitleH = 100f; // 24 top-pad + 76 row
            var titleStrip = Go("TitleStrip", card.transform);
            var tsRT = RT(titleStrip);
            tsRT.anchorMin        = new Vector2(0f, 1f);
            tsRT.anchorMax        = new Vector2(1f, 1f);
            tsRT.pivot            = new Vector2(0.5f, 1f);
            tsRT.sizeDelta        = new Vector2(0f, TitleH);
            tsRT.anchoredPosition = Vector2.zero;
            var tsVG = titleStrip.AddComponent<VerticalLayoutGroup>();
            tsVG.padding            = new RectOffset(34, 34, 24, 0);
            tsVG.spacing            = 0;
            tsVG.childControlWidth  = true;  tsVG.childForceExpandWidth  = true;
            tsVG.childControlHeight = false; tsVG.childForceExpandHeight = false;

            var titleRow = HRow(titleStrip.transform, 76, 6);
            Lbl(titleRow.transform, "SETTINGS", 32, C_White, FontStyles.Bold,
                TextAlignmentOptions.Left, flexW: 1);
            var closeBtn = IconBtn(titleRow.transform, "X", 52, 52, C_BtnBg, C_Gray, 24);
            closeBtn.onClick.AddListener(TogglePanel);
            LE(closeBtn.gameObject, prefW: 52);

            // 1-px divider below the title strip
            var divider = Go("Divider", card.transform);
            var divRT   = RT(divider);
            divRT.anchorMin        = new Vector2(0f, 1f);
            divRT.anchorMax        = new Vector2(1f, 1f);
            divRT.pivot            = new Vector2(0.5f, 1f);
            divRT.sizeDelta        = new Vector2(0f, 1f);
            divRT.anchoredPosition = new Vector2(0f, -TitleH);
            divider.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);

            // ── ScrollRect fills the card below the title strip ───────────────
            // This means any number of settings rows will scroll rather than overflow.
            var scrollGO = Go("Scroll", card.transform);
            var scrollRT = RT(scrollGO);
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = new Vector2(0f, -(TitleH + 1f));

            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal        = false;
            scroll.vertical          = true;
            scroll.scrollSensitivity = 30f;
            scroll.movementType      = ScrollRect.MovementType.Clamped;
            scroll.inertia           = true;
            scroll.decelerationRate  = 0.135f;

            var viewport = Go("Viewport", scrollGO.transform);
            Stretch(RT(viewport));
            viewport.AddComponent<RectMask2D>();
            // Transparent image makes the entire viewport area a raycast target,
            // so scroll events register even over empty space between rows.
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = Color.clear;
            scroll.viewport = RT(viewport);

            // Content grows automatically via ContentSizeFitter.
            var content = Go("Content", viewport.transform);
            var contentRT = RT(content);
            contentRT.anchorMin        = new Vector2(0f, 1f);
            contentRT.anchorMax        = new Vector2(1f, 1f);
            contentRT.pivot            = new Vector2(0.5f, 1f);
            contentRT.sizeDelta        = Vector2.zero;
            contentRT.anchoredPosition = Vector2.zero;

            var vg = content.AddComponent<VerticalLayoutGroup>();
            vg.padding = new RectOffset(34, 34, 16, 30);
            vg.spacing = 10;
            vg.childControlWidth  = true;  vg.childForceExpandWidth  = true;
            vg.childControlHeight = false; vg.childForceExpandHeight = false;

            content.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRT;

            // ── Section: CAMERA VIEW ─────────────────────────────────────────
            Gap(content.transform, 4);
            SectionHdr(content.transform, "CAMERA VIEW");

            var cmRow = SettingRow(content.transform, "Camera Mode");
            _camModeLabel = Cycler(cmRow.transform, CamModeNames, 0, StepCamMode);

            var slRow = SettingRow(content.transform, "Slide Effect");
            _slideModeLabel = Cycler(slRow.transform, SlideModeNames, 0, StepSlideMode);
            HintLbl(content.transform, "First person only — no effect in TP or ISO");

            // ── Section: CAMERA TUNING ───────────────────────────────────────
            Gap(content.transform, 8);
            SectionHdr(content.transform, "CAMERA TUNING");

            float initDist = _tpFollow != null ? _tpFollow.CameraDistance : 5f;
            var tdRow = SettingRow(content.transform, "TP Distance");
            _tpDistLabel = SliderRow(tdRow.transform, 2f, 10f, initDist, OnTPDist,
                initDist.ToString("F1") + " m");
            _tpDistSlider = tdRow.GetComponentInChildren<Slider>();
            HintLbl(content.transform, "Third person only");

            float initFov = _fpVCam != null ? _fpVCam.m_Lens.FieldOfView : 80f;
            var fovRow = SettingRow(content.transform, "FP Field of View");
            _fpFovLabel = SliderRow(fovRow.transform, 60f, 110f, initFov, OnFPFov,
                initFov.ToString("F0") + "°");
            _fpFovSlider = fovRow.GetComponentInChildren<Slider>();
            HintLbl(content.transform, "First person only");

            // ── Section: CONTROLS ────────────────────────────────────────────
            Gap(content.transform, 8);
            SectionHdr(content.transform, "CONTROLS");

            float initSens = _fpc != null ? _fpc.RotationSpeed : 1f;
            var sensRow = SettingRow(content.transform, "Look Sensitivity");
            _sensLabel = SliderRow(sensRow.transform, 0.2f, 3f, initSens, OnSensitivity,
                initSens.ToString("F2") + "×");
            _sensSlider = sensRow.GetComponentInChildren<Slider>();

            // ── Section: GAME ────────────────────────────────────────────────
            Gap(content.transform, 8);
            Separator(content.transform);
            Gap(content.transform, 4);
            SectionHdr(content.transform, "GAME");

            var resetSettingsBtn = ActionBtn(content.transform, "Reset Settings", C_BtnBg, C_White);
            resetSettingsBtn.onClick.AddListener(OnResetSettingsClicked);

            var resetGameBtn = ActionBtn(content.transform, "Delete Save Data",
                new Color(0.55f, 0.15f, 0.12f, 1f), C_White);
            _resetGameBtnText = resetGameBtn.GetComponentInChildren<TextMeshProUGUI>();
            resetGameBtn.onClick.AddListener(OnResetGameClicked);

            HintLbl(content.transform,
                "Delete Save Data: click twice to confirm  •  wipes world progress, keeps settings");
        }

        // ── Save integration ──────────────────────────────────────────────────

        public GameSettings GetCurrentSettings() => new GameSettings
        {
            cameraMode      = _camView  != null ? (int)_camView.CurrentMode      : 0,
            slideMode       = _slide    != null ? (int)_slide.CameraMode         : 0,
            tpDistance      = _tpFollow != null ? _tpFollow.CameraDistance       : 5f,
            fpFov           = _fpVCam   != null ? _fpVCam.m_Lens.FieldOfView     : 80f,
            lookSensitivity = _fpc      != null ? _fpc.RotationSpeed             : 1f,
        };

        public void ApplySettings(GameSettings s)
        {
            ApplyCameraViewSettings(s);
            ApplyCameraTuningSettings(s);
            ApplyControlsSettings(s);
        }

        private void ApplyCameraViewSettings(GameSettings s)
        {
            _camModeIdx = Mathf.Clamp(s.cameraMode, 0, CamModeNames.Length - 1);
            if (_camModeLabel != null) _camModeLabel.text = CamModeNames[_camModeIdx];
            _camView?.SetMode((LushWorld.Camera.CameraMode)_camModeIdx);

            _slideModeIdx = Mathf.Clamp(s.slideMode, 0, SlideModeNames.Length - 1);
            if (_slideModeLabel != null) _slideModeLabel.text = SlideModeNames[_slideModeIdx];
            if (_slide != null) _slide.CameraMode = (SlideCameraMode)_slideModeIdx;
        }

        private void ApplyCameraTuningSettings(GameSettings s)
        {
            // Setting slider.value fires onValueChanged which updates the underlying system and label.
            if (_tpDistSlider != null) _tpDistSlider.value = s.tpDistance;
            if (_fpFovSlider  != null) _fpFovSlider.value  = s.fpFov;
        }

        private void ApplyControlsSettings(GameSettings s)
        {
            if (_sensSlider != null) _sensSlider.value = s.lookSensitivity;
        }

        void OnResetSettingsClicked() =>
            LushWorld.Save.SaveManager.Instance?.ResetSettings();

        void OnResetGameClicked()
        {
            if (!_resetGamePending)
            {
                _resetGamePending     = true;
                _resetGamePendingTime = Time.time;
                if (_resetGameBtnText != null) _resetGameBtnText.text = "CONFIRM?";
            }
            else
            {
                LushWorld.Save.SaveManager.Instance?.ResetGame();
            }
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
            var row = HRow(parent, 58, 12);
            Lbl(row.transform, label, 20, C_Gray, FontStyles.Normal,
                TextAlignmentOptions.Left, prefW: 270);
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
            RT(go).sizeDelta = new Vector2(0, 46);
            go.AddComponent<Image>().color = C_Section;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 46; le.flexibleWidth = 1;

            var inner = Go("Lbl", go.transform);
            var rt = RT(inner);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16, 6); rt.offsetMax = new Vector2(0, -6);
            var t = inner.AddComponent<TextMeshProUGUI>();
            t.text = title; t.fontSize = 17; t.color = C_Accent;
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

        static void Gap(Transform parent, float h)
        {
            var go = Go("Gap", parent);
            RT(go).sizeDelta = new Vector2(0, h);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = h; le.flexibleWidth = 1;
        }

        static void HintLbl(Transform parent, string text)
        {
            var go = Go("Hint", parent);
            RT(go).sizeDelta = new Vector2(0, 26);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 26; le.flexibleWidth = 1;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = 14;
            t.color = new Color(0.38f, 0.38f, 0.42f, 1f);
            t.fontStyle = FontStyles.Italic;
            t.alignment = TextAlignmentOptions.Right;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.enableWordWrapping = false;
        }

        // Value cycler (◄ label ►) — returns the center label for later sync
        static TextMeshProUGUI Cycler(Transform parent, string[] opts, int startIdx,
                                       System.Action<int> step)
        {
            var left = IconBtn(parent, "◄", 44, 44, C_BtnBg, C_White, 20);
            left.onClick.AddListener(() => step(-1));

            var valGO = Go("Val", parent);
            RT(valGO).sizeDelta = new Vector2(220, 44);
            var t = valGO.AddComponent<TextMeshProUGUI>();
            t.text = opts[startIdx]; t.fontSize = 22; t.color = C_White;
            t.alignment = TextAlignmentOptions.Center;
            t.overflowMode = TextOverflowModes.Overflow;
            LE(valGO, flexW: 1);

            var right = IconBtn(parent, "►", 44, 44, C_BtnBg, C_White, 20);
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
            RT(valGO).sizeDelta = new Vector2(90, 36);
            var t = valGO.AddComponent<TextMeshProUGUI>();
            t.text = initText; t.fontSize = 20; t.color = C_White;
            t.alignment = TextAlignmentOptions.Right;
            t.overflowMode = TextOverflowModes.Overflow;
            LE(valGO, prefW: 90);
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

        static Button ActionBtn(Transform parent, string label, Color bg, Color textColor, int fontSize = 20)
        {
            var go = Go("ActBtn_" + label, parent);
            RT(go).sizeDelta = new Vector2(0, 54);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cols = btn.colors;
            cols.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cols.pressedColor     = new Color(0.70f, 0.70f, 0.70f, 1f);
            btn.colors = cols;
            LE(go, flexW: 1);

            var lbl = Go("L", go.transform);
            var rt = RT(lbl);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = label; t.fontSize = fontSize; t.color = textColor;
            t.alignment = TextAlignmentOptions.Center;
            t.overflowMode = TextOverflowModes.Overflow;

            return btn;
        }

        // Consumes scroll events so they don't reach the game camera behind the dimmer.
        private class ScrollBlocker : MonoBehaviour, IScrollHandler
        {
            public void OnScroll(PointerEventData _) { }
        }
    }
}
