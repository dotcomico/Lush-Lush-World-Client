using LushWorld.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI
{
    // Shows an active buff countdown in the HUD.
    // Attach to a child GO inside InventoryUI with an Image panel + TMP timer text assigned.
    public class BuffHUD : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Image _iconImage;        // optional — leave empty for now
        [SerializeField] private Color _gummyColor = new Color(1f, 0.45f, 0.7f); // candy pink

        private bool _buffActive;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnEnable()
        {
            PlayerBuffSystem.OnBuffStarted += HandleBuffStarted;
            PlayerBuffSystem.OnBuffTick += HandleBuffTick;
            PlayerBuffSystem.OnBuffEnded += HandleBuffEnded;
        }

        private void OnDisable()
        {
            PlayerBuffSystem.OnBuffStarted -= HandleBuffStarted;
            PlayerBuffSystem.OnBuffTick -= HandleBuffTick;
            PlayerBuffSystem.OnBuffEnded -= HandleBuffEnded;
        }

        private void Update()
        {
            if (!_buffActive) return;
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.04f;
            transform.localScale = Vector3.one * pulse;
        }

        private void HandleBuffStarted(string buffId, float duration)
        {
            if (_panel != null) _panel.SetActive(true);
            _buffActive = true;
            SetTimerText(duration);
            if (_iconImage != null) _iconImage.color = _gummyColor;
        }

        private void HandleBuffTick(string buffId, float remaining)
        {
            SetTimerText(remaining);
        }

        private void HandleBuffEnded(string buffId)
        {
            _buffActive = false;
            transform.localScale = Vector3.one;
            if (_panel != null) _panel.SetActive(false);
        }

        private void SetTimerText(float seconds)
        {
            if (_timerText != null)
                _timerText.text = $"{Mathf.CeilToInt(seconds)}s";
        }
    }
}
