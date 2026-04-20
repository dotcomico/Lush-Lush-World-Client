using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using LushWorld.Player;

namespace LushWorld.UI
{
    public class SettingsUIController : MonoBehaviour
    {
        [Header("Player Reference (auto-found if empty)")]
        [SerializeField] private SlideController _slideController;

        [Header("UI Elements (auto-found if empty)")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Dropdown _slideCameraDropdown;

        private void Awake()
        {
            if (_settingsPanel == null)
                _settingsPanel = transform.Find("SettingsPanel")?.gameObject;

            if (_slideCameraDropdown == null)
                _slideCameraDropdown = transform
                    .Find("SettingsPanel/SlideCameraDropdown")
                    ?.GetComponent<Dropdown>();

            if (_slideController == null)
                _slideController = FindFirstObjectByType<SlideController>();

            var gearBtn = transform.Find("GearButton")?.GetComponent<Button>();
            if (gearBtn != null)
                gearBtn.onClick.AddListener(TogglePanel);

            var closeBtn = transform.Find("SettingsPanel/CloseButton")?.GetComponent<Button>();
            if (closeBtn != null)
                closeBtn.onClick.AddListener(TogglePanel);

            if (_settingsPanel == null)
                Debug.LogError("[SettingsUI] SettingsPanel child not found. Check hierarchy.");
            if (_slideCameraDropdown == null)
                Debug.LogError("[SettingsUI] SlideCameraDropdown not found. Check hierarchy.");
        }

        private void Start()
        {
            if (_settingsPanel == null || _slideCameraDropdown == null) return;

            _settingsPanel.SetActive(false);

            _slideCameraDropdown.ClearOptions();
            _slideCameraDropdown.AddOptions(new List<string> { "None", "Medium", "Cinematic" });

            SyncDropdown();
            _slideCameraDropdown.onValueChanged.AddListener(OnSlideCameraChanged);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                TogglePanel();
#else
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePanel();
#endif
        }

        public void TogglePanel()
        {
            if (_settingsPanel == null) return;
            bool opening = !_settingsPanel.activeSelf;
            _settingsPanel.SetActive(opening);
            if (opening) SyncDropdown();
        }

        private void SyncDropdown()
        {
            if (_slideController != null && _slideCameraDropdown != null)
                _slideCameraDropdown.value = (int)_slideController.CameraMode;
        }

        private void OnSlideCameraChanged(int index)
        {
            if (_slideController != null)
                _slideController.CameraMode = (SlideCameraMode)index;
        }
    }
}
