using LushWorld.Player;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI.Stats
{
    // Renders health and hunger bars by subscribing to PlayerStats static events.
    // Zero direct dependency on the PlayerStats MonoBehaviour — NGO-safe.
    public class StatsUI : MonoBehaviour
    {
        [SerializeField] private Image _healthFill;
        [SerializeField] private Image _hungerFill;

        private void OnEnable()
        {
            PlayerStats.OnStatsReady    += HandleStatsReady;
            PlayerStats.OnHealthChanged += HandleHealthChanged;
            PlayerStats.OnHungerChanged += HandleHungerChanged;
        }

        // Fallback: if PlayerStats.Start() fired before our OnEnable()
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
            if (_healthFill != null) _healthFill.fillAmount = normalized;
        }

        private void SetHunger(float normalized)
        {
            if (_hungerFill != null) _hungerFill.fillAmount = normalized;
        }
    }
}
