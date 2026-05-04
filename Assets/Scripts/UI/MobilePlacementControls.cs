using LushWorld.Building;
using LushWorld.UI.Mobile;
using LushWorld.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LushWorld.UI
{
    // Attach to MobilePlacementPanel (parent of PlaceButton and RotateButton).
    // Uses CanvasGroup for show/hide so the component stays enabled and subscribed.
    // PlaceButton: wires UIVirtualButton.buttonClickOutputEvent (tap to place).
    // RotateButton: attaches a PointerHoldRelay at runtime — fires true while finger is down,
    //               false on release — no Inspector wiring required.
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

            WireButton("PlaceButton", () => BuildingSystem.LocalPlayer?.MobilePlacePressed());
            WireHoldRelay("RotateButton", held => BuildingSystem.LocalPlayer?.MobileRotateHeld(held));
        }

        private void OnEnable()  => BuildingSystem.OnPlacementStateChanged += SetVisible;
        private void OnDisable() => BuildingSystem.OnPlacementStateChanged -= SetVisible;

        private void SetVisible(bool show)
        {
            if (_group == null) return;
            if (!show && MobileLayoutCustomizer.IsEditing) return;
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

            var vBtn = t.GetComponent<UIVirtualButton>();
            if (vBtn != null) { vBtn.buttonClickOutputEvent.AddListener(callback); return; }

            var btn = t.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) { btn.onClick.AddListener(callback); return; }

            Debug.LogWarning($"[MobilePlacementControls] No button component found on '{childName}'.");
        }

        // Adds a lightweight PointerHoldRelay to the child at runtime.
        // This bypasses UIVirtualButton event wiring and hooks directly into Unity's pointer system.
        private void WireHoldRelay(string childName, System.Action<bool> callback)
        {
            var t = transform.Find(childName);
            if (t == null)
            {
                Debug.LogWarning($"[MobilePlacementControls] Child '{childName}' not found.");
                return;
            }

            var relay = t.gameObject.AddComponent<PointerHoldRelay>();
            relay.OnHeldChanged = callback;
        }
    }

    // Added at runtime to the RotateButton — reports finger down (true) and up (false).
    // Lives in this file; never needs to appear in the Inspector.
    internal class PointerHoldRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action<bool> OnHeldChanged;

        public void OnPointerDown(PointerEventData _) => OnHeldChanged?.Invoke(true);
        public void OnPointerUp(PointerEventData _)   => OnHeldChanged?.Invoke(false);
    }
}
