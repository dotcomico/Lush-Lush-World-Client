using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LushWorld.DevTools
{
    // Emergency cursor toggle for UI testing. Press F1 or LeftCtrl+C to flip lock state.
    // Active only in Editor and Development Builds — stripped from release.
    // Attach to the PlayerRig or any persistent scene object; wire inputBridge in Inspector.
    public class DebugCursorToggle : MonoBehaviour
    {
        [SerializeField] private StarterAssetsInputs inputBridge;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;

            bool triggered = kb.f1Key.wasPressedThisFrame
                          || (kb.leftCtrlKey.isPressed && kb.cKey.wasPressedThisFrame);

            if (triggered)
                ToggleCursorLock();
#endif
        }

        private void ToggleCursorLock()
        {
            bool shouldUnlock = Cursor.lockState != CursorLockMode.None;

            if (inputBridge != null)
            {
                inputBridge.cursorLocked        = !shouldUnlock;
                inputBridge.cursorInputForLook  = !shouldUnlock;
            }

            Cursor.lockState = shouldUnlock ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible   = shouldUnlock;

            Debug.Log($"[DebugCursorToggle] Cursor {(shouldUnlock ? "unlocked" : "locked")}");
        }
#endif
    }
}
