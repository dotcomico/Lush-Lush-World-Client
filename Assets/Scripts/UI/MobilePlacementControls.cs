using LushWorld.Building;
using LushWorld.Utilities;
using UnityEngine;

namespace LushWorld.UI
{
    // Attach to MobilePlacementPanel (parent of PlaceButton and RotateButton).
    // Uses CanvasGroup for show/hide so the component stays enabled and subscribed.
    // Wires UIVirtualButton.buttonClickOutputEvent — the mobile button architecture used in this project.
    public class MobilePlacementControls : MonoBehaviour
    {
        private CanvasGroup _group;

        private void Awake()
        {
            if (!PlatformDetector.IsMobile)
            {
                gameObject.SetActive(false);
                return;
            }

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

            SetVisible(false);

            WireButton("PlaceButton",       () => BuildingSystem.LocalPlayer?.MobilePlacePressed());
            WireButtonHeld("RotateButton", held => BuildingSystem.LocalPlayer?.MobileRotateHeld(held));
        }

        // Subscribe/unsubscribe on the component lifecycle, not the GameObject lifecycle.
        // This keeps the subscription alive even when CanvasGroup hides the panel.
        private void OnEnable()  => BuildingSystem.OnPlacementStateChanged += SetVisible;
        private void OnDisable() => BuildingSystem.OnPlacementStateChanged -= SetVisible;

        private void SetVisible(bool show)
        {
            if (_group == null) return;
            _group.alpha          = show ? 1f : 0f;
            _group.interactable   = show;
            _group.blocksRaycasts = show;
        }

        private void WireButton(string childName, UnityEngine.Events.UnityAction callback)
        {
            var t = transform.Find(childName);
            if (t == null)
            {
                Debug.LogWarning($"[MobilePlacementControls] Child '{childName}' not found.");
                return;
            }

            // UIVirtualButton is the touch-input component used by this project's mobile buttons.
            var vBtn = t.GetComponent<UIVirtualButton>();
            if (vBtn != null)
            {
                vBtn.buttonClickOutputEvent.AddListener(callback);
                return;
            }

            // Fallback for any plain UI Button.
            var btn = t.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(callback);
                return;
            }

            Debug.LogWarning($"[MobilePlacementControls] No UIVirtualButton or Button found on '{childName}'.");
        }

        private void WireButtonHeld(string childName, UnityEngine.Events.UnityAction<bool> callback)
        {
            var t = transform.Find(childName);
            if (t == null)
            {
                Debug.LogWarning($"[MobilePlacementControls] Child '{childName}' not found.");
                return;
            }

            var vBtn = t.GetComponent<UIVirtualButton>();
            if (vBtn != null)
            {
                vBtn.buttonStateOutputEvent.AddListener(callback);
                return;
            }

            Debug.LogWarning($"[MobilePlacementControls] No UIVirtualButton found on '{childName}' for hold wiring.");
        }
    }
}
