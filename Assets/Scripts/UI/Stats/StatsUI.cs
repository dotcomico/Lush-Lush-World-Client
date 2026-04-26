using LushWorld.Player;
using TMPro;
using UnityEngine;

namespace LushWorld.UI.Stats
{
    // Renders health and hunger bars by subscribing to PlayerStats static events.
    // Uses RectTransform anchor scaling for a sharp rectangular fill — no sprite needed.
    // Zero direct dependency on the PlayerStats MonoBehaviour — NGO-safe.
    public class StatsUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _healthFill;
        [SerializeField] private RectTransform _hungerFill;
        [SerializeField] private TMP_Text      _healthText;
        [SerializeField] private TMP_Text      _hungerText;

        private void OnEnable()
        {
            if (_healthFill == null || _hungerFill == null)
                Debug.LogWarning("[StatsUI] Fill references are null — assign the Fill RectTransforms in the Inspector.", this);

            PlayerStats.OnStatsReady    += HandleStatsReady;
            PlayerStats.OnHealthChanged += HandleHealthChanged;
            PlayerStats.OnHungerChanged += HandleHungerChanged;
        }

        private void Start()
        {
            if (PlayerStats.LocalPlayer != null)
            {
                SetHealth(PlayerStats.LocalPlayer.HealthNormalized);
                SetHunger(PlayerStats.LocalPlayer.HungerNormalized);
            }
        }

        private void OnDisable()
        {
            PlayerStats.OnStatsReady    -= HandleStatsReady;
            PlayerStats.OnHealthChanged -= HandleHealthChanged;
            PlayerStats.OnHungerChanged -= HandleHungerChanged;
        }

        private void HandleStatsReady(float health, float hunger)
        {
            SetHealth(health);
            SetHunger(hunger);
        }

        private void HandleHealthChanged(float normalized) => SetHealth(normalized);
        private void HandleHungerChanged(float normalized) => SetHunger(normalized);

        private void SetHealth(float normalized)
        {
            if (_healthFill != null) _healthFill.anchorMax = new Vector2(normalized, _healthFill.anchorMax.y);
            if (_healthText != null) _healthText.text = $"{Mathf.RoundToInt(normalized * 100)}%";
        }

        private void SetHunger(float normalized)
        {
            if (_hungerFill != null) _hungerFill.anchorMax = new Vector2(normalized, _hungerFill.anchorMax.y);
            if (_hungerText != null) _hungerText.text = $"{Mathf.RoundToInt(normalized * 100)}%";
        }
    }
}
