using LushWorld.Utilities;
using StarterAssets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LushWorld.UI
{
    // Base class for any menu that needs a full-screen backdrop + cursor/input management.
    // Derived classes call InitBackdrop() in Start() and ApplyMenuToggle() in their toggle handler.
    public abstract class MenuUIBase : MonoBehaviour
    {
        protected GameObject _backdrop;

        // Creates the transparent full-screen backdrop button behind the panel.
        // Pass panelForOrdering to ensure the backdrop renders behind the panel.
        protected void InitBackdrop(string backdropName, UnityAction onBackdropClick, GameObject panelForOrdering = null)
        {
            _backdrop = new GameObject(backdropName, typeof(RectTransform));
            _backdrop.transform.SetParent(transform, false);
            _backdrop.layer = gameObject.layer;

            var rt = _backdrop.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // Tiny non-zero alpha: invisible to the eye but guarantees raycast detection.
            var img = _backdrop.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.004f);
            img.raycastTarget = true;

            var btn = _backdrop.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(onBackdropClick);

            // Backdrop renders BEHIND the panel (lower sibling index = drawn first).
            if (panelForOrdering != null)
                _backdrop.transform.SetSiblingIndex(panelForOrdering.transform.GetSiblingIndex());

            _backdrop.SetActive(false);
        }

        // Toggles panel + backdrop visibility and applies cursor/input lock state.
        protected void ApplyMenuToggle(bool isOpen, GameObject panel, StarterAssetsInputs inputBridge)
        {
            UIUtils.SetVisible(panel, isOpen);
            UIUtils.SetVisible(_backdrop, isOpen);

            if (isOpen)
                UnlockCursorAndInput(inputBridge);
            else
                RestoreCursorAndInput(inputBridge);
        }

        // Stop camera immediately: clear buffered look + unlock cursor.
        private void UnlockCursorAndInput(StarterAssetsInputs inputBridge)
        {
            if (inputBridge != null)
            {
                inputBridge.cursorLocked       = false;
                inputBridge.cursorInputForLook = false;
                inputBridge.look               = Vector2.zero;
                inputBridge.move               = Vector2.zero;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // Resume camera: re-lock cursor and re-enable look input.
        private void RestoreCursorAndInput(StarterAssetsInputs inputBridge)
        {
            if (inputBridge != null)
            {
                inputBridge.cursorLocked       = true;
                inputBridge.cursorInputForLook = true;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }
}
