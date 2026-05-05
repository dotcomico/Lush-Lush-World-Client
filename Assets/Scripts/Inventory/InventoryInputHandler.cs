using LushWorld.Building;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LushWorld.Inventory
{
    // Sits on the Player GameObject alongside StarterAssetsInputs.
    // Uses the same PlayerInput SendMessage pattern — Unity calls On<ActionName>(InputValue)
    // automatically when PlayerInput.notificationBehavior = SendMessage.
    //
    // Phase 2 upgrade: add [RequireComponent(typeof(NetworkBehaviour))] check,
    // return early if !IsOwner so non-owned players don't process input.
    [RequireComponent(typeof(InventorySystem))]
    public class InventoryInputHandler : MonoBehaviour
    {
        private InventorySystem _inventory;

        private void Awake()
        {
            _inventory = GetComponent<InventorySystem>();
        }

#if ENABLE_INPUT_SYSTEM
        public void OnHotbarSlot1(InputValue value) { if (value.isPressed) _inventory.RequestSelectSlot(0); }
        public void OnHotbarSlot2(InputValue value) { if (value.isPressed) _inventory.RequestSelectSlot(1); }
        public void OnHotbarSlot3(InputValue value) { if (value.isPressed) _inventory.RequestSelectSlot(2); }
        public void OnHotbarSlot4(InputValue value) { if (value.isPressed) _inventory.RequestSelectSlot(3); }
        public void OnHotbarSlot5(InputValue value) { if (value.isPressed) _inventory.RequestSelectSlot(4); }
        public void OnHotbarSlot6(InputValue value) { if (value.isPressed) _inventory.RequestSelectSlot(5); }
        public void OnHotbarSlot7(InputValue value) { if (value.isPressed) _inventory.RequestSelectSlot(6); }
        public void OnHotbarSlot8(InputValue value) { if (value.isPressed) _inventory.RequestSelectSlot(7); }

        public void OnHotbarScroll(InputValue value)
        {
            if (BuildingSystem.IsMenuOpen) return;
            var delta = value.Get<Vector2>();
            if (delta.y == 0f) return;
            _inventory.RequestCycleSlot(delta.y > 0f ? -1 : 1);
        }

        public void OnOpenInventory(InputValue value)
        {
            if (value.isPressed) _inventory.RequestToggleBackpack();
        }
#endif

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame)
                    _inventory.RequestDropActiveItem();
                if (Keyboard.current.gKey.wasPressedThisFrame)
                    _inventory.RequestToggleBackpack();
                if (Keyboard.current.bKey.wasPressedThisFrame)
                    BuildingSystem.LocalPlayer?.ToggleBuildingMenu();
            }
#else
            if (Input.GetKeyDown(KeyCode.Q))
                _inventory.RequestDropActiveItem();
            if (Input.GetKeyDown(KeyCode.G))
                _inventory.RequestToggleBackpack();
            if (Input.GetKeyDown(KeyCode.B))
                BuildingSystem.LocalPlayer?.ToggleBuildingMenu();
#endif
        }
    }
}
