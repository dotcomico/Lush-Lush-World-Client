using LushWorld.Building;
using LushWorld.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI
{
    // Attach to MobilePlacementPanel (parent of PlaceButton and RotateButton).
    // Hides permanently on desktop. Shows only while a ghost is being placed on mobile.
    // Buttons are wired automatically by child name — no Inspector wiring needed.
    public class MobilePlacementControls : MonoBehaviour
    {
        private void Awake()
        {
            if (!PlatformDetector.IsMobile)
            {
                gameObject.SetActive(false);
                return;
            }

            WireButton("PlaceButton",  () => BuildingSystem.LocalPlayer?.MobilePlacePressed());
            WireButton("RotateButton", () => BuildingSystem.LocalPlayer?.MobileRotateStep());

            gameObject.SetActive(false); // hidden until placement starts
        }

        private void OnEnable()  => BuildingSystem.OnPlacementStateChanged += OnPlacementStateChanged;
        private void OnDisable() => BuildingSystem.OnPlacementStateChanged -= OnPlacementStateChanged;

        private void OnPlacementStateChanged(bool isPlacing)
        {
            if (!PlatformDetector.IsMobile) return;
            gameObject.SetActive(isPlacing);
        }

        private void WireButton(string childName, UnityEngine.Events.UnityAction callback)
        {
            var t = transform.Find(childName);
            if (t == null) { Debug.LogWarning($"[MobilePlacementControls] Child '{childName}' not found."); return; }
            var btn = t.GetComponent<Button>();
            if (btn == null) { Debug.LogWarning($"[MobilePlacementControls] No Button on '{childName}'."); return; }
            btn.onClick.AddListener(callback);
        }
    }
}
