using LushWorld.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI.Stats
{
    // Renders health and hunger bars by subscribing to PlayerStats static events.
    // Zero direct dependency on the PlayerStats MonoBehaviour — NGO-safe.
    public class StatsUI : MonoBehaviour
    {
        [SerializeField] private Image    _healthFill;
        [SerializeField] private Image    _hungerFill;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private TMP_Text _hungerText;

        private void OnEnable()
        {
            if (_healthFill == null || _hungerFill == null)
                Debug.LogWarning("[StatsUI] Bar references are null — run 'Lush World > Setup > Add Player Stats & Bars'", this);

            PlayerStats.OnStatsReady    += HandleStatsReady;
            PlayerStats.OnHealthChanged += HandleHealthChanged;
            PlayerStats.OnHungerChanged += HandleHungerChanged;
        }

        // Only needed when this object is spawned AFTER PlayerStats.Start() has already fired
        // (e.g., UI prefab loaded at runtime). OnEnable + OnStatsReady handles the normal case.
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
            if (_healthText != null) _healthText.text = $"{Mathf.RoundToInt(normalized * 100)}%";
        }

        private void SetHunger(float normalized)
        {
            if (_hungerFill != null) _hungerFill.fillAmount = normalized;
            if (_hungerText != null) _hungerText.text = $"{Mathf.RoundToInt(normalized * 100)}%";
        }
    }
}
