using LushWorld.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LushWorld.UI
{
    // Invisible touch area placed over the held-item view (bottom-right of screen).
    // Long-press drives EatingSystem.MobileEatHeld the same way right-click does on desktop.
    // Pair this component with MobileCanvasVisibility on the same GameObject to hide on desktop.
    public class MobileEatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            EatingSystem.LocalPlayer?.MobileEatHeld(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            EatingSystem.LocalPlayer?.MobileEatHeld(false);
        }

        private void OnDisable()
        {
            // Release the hold if the button is hidden mid-press (e.g. menu opened).
            EatingSystem.LocalPlayer?.MobileEatHeld(false);
        }
    }
}
