using UnityEngine;
using LushWorld.Utilities;

namespace LushWorld.UI
{
    // Attach to UI_Canvas_StarterAssetsInputs_Joysticks.
    // Shows on mobile/tablet, hides on desktop. Detection delegates to
    // PlatformDetector.IsMobile — set PlatformDetector.SimulateMobileInEditor
    // to true in Play Mode to test mobile layout without a device.
    public class MobileCanvasVisibility : MonoBehaviour
    {
        private void Start()
        {
            gameObject.SetActive(PlatformDetector.IsMobile);
        }
    }
}
