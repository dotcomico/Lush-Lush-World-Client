using UnityEditor;
using UnityEngine;
using LushWorld.Utilities;

namespace LushWorld.Editor
{
    /// <summary>
    /// Lush World > Simulate Mobile — toggles mobile UI in Play Mode.
    /// State persists across sessions via EditorPrefs and is applied automatically
    /// before the first scene loads via RuntimeInitializeOnLoadMethod.
    /// </summary>
    public static class MobileSimulationToggle
    {
        private const string MenuPath = "Lush World/Simulate Mobile";
        private const string PrefKey  = "LushWorld_SimulateMobile";

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            bool next = !EditorPrefs.GetBool(PrefKey, false);
            EditorPrefs.SetBool(PrefKey, next);
            PlatformDetector.SimulateMobileInEditor = next;
            Menu.SetChecked(MenuPath, next);
            Debug.Log($"[PlatformDetector] SimulateMobileInEditor = {next}  (re-enter Play Mode to apply)");
        }

        [MenuItem(MenuPath, validate = true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey, false));
            return true;
        }

        // Runs before any Awake/Start when Play Mode starts — applies the saved pref
        // so MobileCanvasVisibility.Start() already sees the correct IsMobile value.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnPlay()
        {
            PlatformDetector.SimulateMobileInEditor = EditorPrefs.GetBool(PrefKey, false);
        }
    }
}
