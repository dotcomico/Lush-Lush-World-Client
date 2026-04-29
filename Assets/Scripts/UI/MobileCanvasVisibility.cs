using UnityEngine;

namespace LushWorld.UI
{
    // Attach to UI_Canvas_StarterAssetsInputs_Joysticks.
    // Hides the canvas on Desktop (PC/Mac/Linux) and shows it on mobile/tablet.
    // Uses SystemInfo.deviceType so it works correctly in built apps.
    // In the Editor, the canvas is hidden by default (Editor == Desktop);
    // tick _forceVisibleInEditor to test mobile layout without a device.
    public class MobileCanvasVisibility : MonoBehaviour
    {
        [SerializeField] private bool _forceVisibleInEditor;

        private void Awake()
        {
            bool isTouchDevice = SystemInfo.deviceType == DeviceType.Handheld;

#if UNITY_EDITOR
            isTouchDevice = _forceVisibleInEditor;
#endif

            gameObject.SetActive(isTouchDevice);
        }
    }
}
