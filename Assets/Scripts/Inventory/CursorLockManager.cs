using StarterAssets;
using UnityEngine;

namespace LushWorld.Inventory
{
    // Responds to backpack open/close by unlocking the cursor and suppressing
    // camera look input. Uses StarterAssetsInputs.cursorLocked so that
    // OnApplicationFocus() in StarterAssetsInputs also respects the inventory state.
    public class CursorLockManager : MonoBehaviour
    {
        [SerializeField] private StarterAssetsInputs inputBridge;

        private void OnEnable()
        {
            InventorySystem.OnBackpackToggleRequested += HandleBackpackToggle;
        }

        private void OnDisable()
        {
            InventorySystem.OnBackpackToggleRequested -= HandleBackpackToggle;
        }

        private void HandleBackpackToggle(bool isOpen)
        {
            if (isOpen)
            {
                inputBridge.cursorLocked = false;
                inputBridge.cursorInputForLook = false;
                inputBridge.look = Vector2.zero;
                inputBridge.move = Vector2.zero;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                inputBridge.cursorLocked = true;
                inputBridge.cursorInputForLook = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
