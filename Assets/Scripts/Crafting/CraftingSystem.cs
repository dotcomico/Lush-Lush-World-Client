using System;
using LushWorld.Inventory;
using UnityEngine;

namespace LushWorld.Crafting
{
    // Sits on PlayerCapsule alongside InventorySystem.
    // All state-changing methods use Request*() — Phase 2 upgrade: become [ServerRpc(RequireOwnership = true)].
    [RequireComponent(typeof(InventorySystem))]
    public class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem LocalPlayer { get; private set; }

        public static event Action<string> OnCraftSuccess;
        public static event Action<string> OnCraftFailed;
        public static event Action<bool>   OnCraftingMenuToggleRequested;

        [SerializeField] private RecipeRegistry _recipeRegistry;

        private InventorySystem _inventory;
        private bool _menuOpen;

        private void Awake()
        {
            _inventory = GetComponent<InventorySystem>();
        }

        private void Start()
        {
            LocalPlayer = this;
        }

        private void OnDestroy()
        {
            if (LocalPlayer == this) LocalPlayer = null;
        }

        public void ToggleCraftingMenu()
        {
            _menuOpen = !_menuOpen;
            OnCraftingMenuToggleRequested?.Invoke(_menuOpen);
        }

        public void CloseCraftingMenu()
        {
            if (!_menuOpen) return;
            _menuOpen = false;
            OnCraftingMenuToggleRequested?.Invoke(false);
        }

        // Phase 2 upgrade: [ServerRpc(RequireOwnership = true)]
        public void RequestCraft(string recipeId, int quantity)
        {
            if (_recipeRegistry == null || !_recipeRegistry.TryGetRecipe(recipeId, out var recipe))
            {
                Debug.LogWarning($"[CraftingSystem] Unknown recipe: {recipeId}");
                return;
            }

            if (!ValidateCraft(recipe, quantity, out string error))
            {
                if (error != null) OnCraftFailed?.Invoke(error);
                return;
            }

            ConsumeIngredients(recipe, quantity);
            InventorySystem.GiveItem(recipe.OutputItemId, recipe.OutputQuantity * quantity);
            OnCraftSuccess?.Invoke(recipeId);
        }

        private bool ValidateCraft(RecipeDefinition recipe, int qty, out string error)
        {
            error = null;
            if (qty <= 0) return false;
            if (GetMaxCraftable(recipe) < qty) { error = "Not enough materials"; return false; }
            return true;
        }

        public int GetMaxCraftable(RecipeDefinition recipe)
        {
            if (recipe?.Ingredients == null || recipe.Ingredients.Count == 0) return 0;
            int max = int.MaxValue;
            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient.Quantity <= 0) continue;
                int canMake = CountItem(ingredient.ItemId) / ingredient.Quantity;
                if (canMake < max) max = canMake;
            }
            return max == int.MaxValue ? 0 : max;
        }

        public int CountItem(string itemId)
        {
            if (_inventory == null) return 0;
            return CountItemInSection(itemId, true) + CountItemInSection(itemId, false);
        }

        private int CountItemInSection(string itemId, bool isHotbar)
        {
            var data  = _inventory.Data;
            int size  = isHotbar ? InventoryData.HotbarSize : InventoryData.BackpackSize;
            int count = 0;
            for (int i = 0; i < size; i++)
            {
                var slot = data.GetSlot(i, isHotbar);
                if (!slot.IsEmpty && slot.ItemId == itemId) count += slot.Quantity;
            }
            return count;
        }

        private void ConsumeIngredients(RecipeDefinition recipe, int quantity)
        {
            foreach (var ingredient in recipe.Ingredients)
                RemoveItem(ingredient.ItemId, ingredient.Quantity * quantity);
        }

        private void RemoveItem(string itemId, int qty)
        {
            RemoveItemFromSection(itemId, ref qty, true);
            RemoveItemFromSection(itemId, ref qty, false);
        }

        private void RemoveItemFromSection(string itemId, ref int remaining, bool isHotbar)
        {
            var data = _inventory.Data;
            int size = isHotbar ? InventoryData.HotbarSize : InventoryData.BackpackSize;
            for (int i = 0; i < size && remaining > 0; i++)
            {
                var slot = data.GetSlot(i, isHotbar);
                if (slot.IsEmpty || slot.ItemId != itemId) continue;
                int toRemove = Mathf.Min(remaining, slot.Quantity);
                _inventory.RequestRemoveItem(i, isHotbar, toRemove);
                remaining -= toRemove;
            }
        }
    }
}
