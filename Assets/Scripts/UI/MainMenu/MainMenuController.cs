using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace LushWorld.UI.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        private const string GameSceneName = "WorldTestScene";
        private const float TitleFontSize = 72f;
        private const float SubtitleFontSize = 24f;
        private const float StartButtonWidth = 300f;
        private const float StartButtonHeight = 80f;

        private static readonly Color BackgroundColor  = new Color(0.102f, 0.102f, 0.180f, 1f);
        private static readonly Color TitleColor       = Color.white;
        private static readonly Color SubtitleColor    = new Color(0.70f, 0.70f, 0.70f, 1f);
        private static readonly Color StartNormal      = new Color(0.227f, 0.631f, 0.286f, 1f);
        private static readonly Color StartHighlight   = new Color(0.300f, 0.750f, 0.380f, 1f);
        private static readonly Color StartPressed     = new Color(0.150f, 0.500f, 0.220f, 1f);
        private static readonly Color QuitNormal       = new Color(0.30f, 0.30f, 0.30f, 1f);
        private static readonly Color QuitHighlight    = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color QuitPressed      = new Color(0.20f, 0.20f, 0.20f, 1f);

        [SerializeField] private Texture2D heartSnailTexture;

        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            Canvas canvas = CreateCanvas();
            CreateBackground(canvas);
            CreateTitleSection(canvas);
            CreateHeartSnailImage(canvas);
            CreateStartButton(canvas);
            CreateQuitButton(canvas);
        }

        // ── Canvas ───────────────────────────────────────────────────────────

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("MainMenuCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode     = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        // ── Background ───────────────────────────────────────────────────────

        private static void CreateBackground(Canvas canvas)
        {
            var go  = new GameObject("Background");
            go.transform.SetParent(canvas.transform, false);
            var img = go.AddComponent<Image>();
            img.color        = BackgroundColor;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin      = Vector2.zero;
            rt.anchorMax      = Vector2.one;
            rt.sizeDelta      = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        // ── Title ─────────────────────────────────────────────────────────────

        private static void CreateTitleSection(Canvas canvas)
        {
            MakeText(canvas.transform, "Title",
                text:     "LUSH LUSH WORLD",
                fontSize: TitleFontSize,
                style:    FontStyles.Bold,
                color:    TitleColor,
                anchorMin: new Vector2(0.1f, 0.72f),
                anchorMax: new Vector2(0.9f, 0.90f));

            MakeText(canvas.transform, "Subtitle",
                text:     "A Snail Survival Adventure",
                fontSize: SubtitleFontSize,
                style:    FontStyles.Italic,
                color:    SubtitleColor,
                anchorMin: new Vector2(0.1f, 0.37f),
                anchorMax: new Vector2(0.9f, 0.47f));
        }

        // ── HeartSnail Image ─────────────────────────────────────────────────

        private void CreateHeartSnailImage(Canvas canvas)
        {
            var go = new GameObject("HeartSnailImage");
            go.transform.SetParent(canvas.transform, false);

            var img = go.AddComponent<RawImage>();
            img.texture = heartSnailTexture;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(200f, 200f);
            rt.anchoredPosition = new Vector2(0f, 60f);

            if (heartSnailTexture != null)
            {
                var fitter = go.AddComponent<AspectRatioFitter>();
                fitter.aspectMode  = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = (float)heartSnailTexture.width / heartSnailTexture.height;
            }
        }

        // ── Buttons ──────────────────────────────────────────────────────────

        private void CreateStartButton(Canvas canvas)
        {
            var btn = MakeButton(canvas.transform, "StartButton",
                normalColor:     StartNormal,
                highlightColor:  StartHighlight,
                pressedColor:    StartPressed,
                labelText:       "START",
                labelFontSize:   32f,
                labelStyle:      FontStyles.Bold,
                labelColor:      Color.white);

            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin      = new Vector2(0.5f, 0.5f);
            rt.anchorMax      = new Vector2(0.5f, 0.5f);
            rt.pivot          = new Vector2(0.5f, 0.5f);
            rt.sizeDelta      = new Vector2(StartButtonWidth, StartButtonHeight);
            rt.anchoredPosition = new Vector2(0f, -190f);

            btn.onClick.AddListener(OnStartClicked);
        }

        private void CreateQuitButton(Canvas canvas)
        {
            var btn = MakeButton(canvas.transform, "QuitButton",
                normalColor:     QuitNormal,
                highlightColor:  QuitHighlight,
                pressedColor:    QuitPressed,
                labelText:       "QUIT",
                labelFontSize:   20f,
                labelStyle:      FontStyles.Normal,
                labelColor:      new Color(0.8f, 0.8f, 0.8f, 1f));

            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin      = new Vector2(1f, 0f);
            rt.anchorMax      = new Vector2(1f, 0f);
            rt.pivot          = new Vector2(1f, 0f);
            rt.sizeDelta      = new Vector2(150f, 50f);
            rt.anchoredPosition = new Vector2(-20f, 20f);

            btn.onClick.AddListener(OnQuitClicked);
        }

        // ── Actions ───────────────────────────────────────────────────────────

        private static void OnStartClicked()
        {
            SceneManager.LoadScene(GameSceneName);
        }

        private static void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        private static TextMeshProUGUI MakeText(
            Transform parent, string goName,
            string text, float fontSize, FontStyles style, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = color;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin      = anchorMin;
            rt.anchorMax      = anchorMax;
            rt.sizeDelta      = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return tmp;
        }

        private static Button MakeButton(
            Transform parent, string goName,
            Color normalColor, Color highlightColor, Color pressedColor,
            string labelText, float labelFontSize, FontStyles labelStyle, Color labelColor)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = normalColor;

            var btn    = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = normalColor;
            colors.highlightedColor = highlightColor;
            colors.pressedColor     = pressedColor;
            colors.selectedColor    = normalColor;
            btn.colors = colors;

            var labelGo  = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text      = labelText;
            label.fontSize  = labelFontSize;
            label.fontStyle = labelStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.color     = labelColor;

            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin       = Vector2.zero;
            labelRt.anchorMax       = Vector2.one;
            labelRt.sizeDelta       = Vector2.zero;
            labelRt.anchoredPosition = Vector2.zero;

            return btn;
        }
    }
}
