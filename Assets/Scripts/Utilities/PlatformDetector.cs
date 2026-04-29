using UnityEngine;

namespace LushWorld.Utilities
{
    /// <summary>
    /// Global platform detection utility. Use IsMobile wherever behaviour
    /// must differ between touch handheld (Android/iOS) and desktop (PC/Mac).
    ///
    /// Editor testing: toggle SimulateMobileInEditor at runtime to test both
    /// branches without a device. Note: changes after Awake won't retroactively
    /// affect components that cache the value — call their refresh methods if needed.
    /// </summary>
    public static class PlatformDetector
    {
#if UNITY_EDITOR
        // Set to true in Play Mode to simulate a mobile device in the Editor.
        public static bool SimulateMobileInEditor = false;
#endif

        // True when running on a touch handheld (phone/tablet).
        // In the Editor this reflects SimulateMobileInEditor.
        public static bool IsMobile
        {
            get
            {
#if UNITY_EDITOR
                return SimulateMobileInEditor;
#else
                return SystemInfo.deviceType == DeviceType.Handheld;
#endif
            }
        }

        // Convenience inverse — use for "desktop-only" conditions.
        public static bool IsDesktop => !IsMobile;
    }
}
