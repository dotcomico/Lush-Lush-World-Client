using System.Collections;
using LushWorld.Inventory;
using LushWorld.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StarterAssets;

namespace LushWorld.Player
{
    public class PlayerDeathHandler : MonoBehaviour
    {
        [Header("Death Screen Timing")]
        [SerializeField] private float _fadeInDuration  = 0.5f;
        [SerializeField] private float _holdDuration    = 2.5f;
        [SerializeField] private float _fadeOutDuration = 0.5f;

        [Header("Respawn")]
        [SerializeField] private float _respawnHeightOffset = 2f;

        private SlideController       _slide;
        private InventorySystem       _inventory;
        private FirstPersonController _fpc;
        private CharacterController   _cc;

        private void Awake()
        {
            _slide     = GetComponentInChildren<SlideController>();
            _inventory = GetComponentInChildren<InventorySystem>();
            _fpc       = GetComponentInChildren<FirstPersonController>();
            _cc        = GetComponentInChildren<CharacterController>();
        }

        private void OnEnable()  => PlayerStats.OnPlayerDied += OnDied;
        private void OnDisable() => PlayerStats.OnPlayerDied -= OnDied;

        private void OnDied() => StartCoroutine(DeathSequence());

        private IEnumerator DeathSequence()
        {
            if (_fpc != null) _fpc.enabled = false;

            _inventory?.RequestDeathScatterDrop();
            _slide?.ForceEnterSlide();

            var (canvasGO, group) = CreateDeathUI();
            yield return FadeCanvas(group, 0f, 1f, _fadeInDuration);
            yield return new WaitForSeconds(_holdDuration);
            yield return FadeCanvas(group, 1f, 0f, _fadeOutDuration);
            Destroy(canvasGO);

            _slide?.ForceExitSlide();

            Vector3 respawnPos = HeartStone.Instance != null
                ? HeartStone.Instance.transform.position + Vector3.up * _respawnHeightOffset
                : Vector3.up * _respawnHeightOffset;

            // Shift the whole root by the delta between where the CC is now and where
            // it needs to go — works whether CC is on the root or a child object.
            Transform playerRoot = _cc != null ? _cc.transform.root : transform;
            Vector3   ccPos      = _cc != null ? _cc.transform.position : transform.position;

            if (_cc != null) _cc.enabled = false;
            playerRoot.position += respawnPos - ccPos;
            if (_cc != null) _cc.enabled = true;

            PlayerStats.LocalPlayer?.Respawn();
            if (_fpc != null) _fpc.enabled = true;
        }

        private (GameObject go, CanvasGroup group) CreateDeathUI()
        {
            var canvasGO = new GameObject("DeathCanvas");

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGO.AddComponent<GraphicRaycaster>();

            var group = canvasGO.AddComponent<CanvasGroup>();
            group.alpha          = 0f;
            group.blocksRaycasts = false;

            var bgGO   = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            var textGO   = new GameObject("YouDiedText");
            textGO.transform.SetParent(canvasGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp      = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = "YOU DIED";
            tmp.fontSize  = 120;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = new Color(0.85f, 0.05f, 0.05f, 1f);

            return (canvasGO, group);
        }

        private IEnumerator FadeCanvas(CanvasGroup group, float from, float to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
