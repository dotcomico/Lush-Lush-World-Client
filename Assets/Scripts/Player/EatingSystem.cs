using LushWorld.Inventory;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LushWorld.Player
{
    // Handles the eat interaction: hold right mouse button (cursor locked) for EatDuration seconds.
    // Reads FoodValue from ItemDefinition — only items with FoodValue > 0 can be eaten.
    // Phase 2 upgrade: mirror RequestRemoveItem / ConsumeFood as [ServerRpc] calls.
    [RequireComponent(typeof(InventorySystem))]
    public class EatingSystem : MonoBehaviour
    {
        private const float EatDuration = 1.5f;

        [SerializeField] private ItemRegistry _itemRegistry;

        private InventorySystem _inventory;
        private HeldItemView _heldItemView;

        private bool _isEating;
        private float _eatProgress;
        private string _cachedItemId;
        private float _cachedFoodValue;

        private void Awake()
        {
            _inventory = GetComponent<InventorySystem>();
            _heldItemView = GetComponent<HeldItemView>();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            bool rightHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
            bool rightHeld = Input.GetMouseButton(1);
#endif
            bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;

            if (_isEating)
            {
                // Cancel if button released, cursor unlocked (menu opened), or active item changed
                if (!rightHeld || !cursorLocked || !ActiveItemMatchesCache())
                {
                    CancelEating();
                    return;
                }

                _eatProgress += Time.deltaTime;
                if (_eatProgress >= EatDuration)
                    FinishEating();
            }
            else if (rightHeld && cursorLocked)
            {
                TryBeginEating();
            }
        }

        private bool ActiveItemMatchesCache()
        {
            var active = _inventory.Data.ActiveItem;
            return !active.IsEmpty && active.ItemId == _cachedItemId;
        }

        private void TryBeginEating()
        {
            var activeStack = _inventory.Data.ActiveItem;
            if (activeStack.IsEmpty) return;

            if (_itemRegistry == null || !_itemRegistry.TryGetById(activeStack.ItemId, out ItemDefinition def)) return;
            if (def.FoodValue <= 0f) return;

            _isEating = true;
            _eatProgress = 0f;
            _cachedItemId = activeStack.ItemId;
            _cachedFoodValue = def.FoodValue;

            _heldItemView?.StartEatingAnimation();
        }

        private void CancelEating()
        {
            _isEating = false;
            _eatProgress = 0f;
            _cachedItemId = null;
            _cachedFoodValue = 0f;
            _heldItemView?.StopEatingAnimation();
        }

        private void FinishEating()
        {
            _isEating = false;
            _heldItemView?.StopEatingAnimation();

            PlayerStats.ConsumeFood(_cachedFoodValue);
            _inventory.RequestRemoveItem(_inventory.Data.SelectedHotbarSlot, isHotbar: true, qty: 1);

            _cachedItemId = null;
            _cachedFoodValue = 0f;
            _eatProgress = 0f;
        }
    }
}
