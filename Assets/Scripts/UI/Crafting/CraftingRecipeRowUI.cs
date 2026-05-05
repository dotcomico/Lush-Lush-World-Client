using System.Collections.Generic;
using LushWorld.Crafting;
using LushWorld.Inventory;
using LushWorld.UI.Crafting;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI
{
    // One row in the crafting sidebar. Owned by CraftingSidebarUI; instantiated per RecipeDefinition.
    // Wire all [SerializeField] fields via the CraftingRowPrefab in the Unity Inspector.
    public class CraftingRecipeRowUI : MonoBehaviour
    {
        [SerializeField] private Image     _icon;
        [SerializeField] private Transform  _ingredientContainer;
        [SerializeField] private GameObject _ingredientIconPrefab;
        [SerializeField] private Button    _craftButton;

        private RecipeDefinition _recipe;
        private ItemRegistry     _itemRegistry;
        private readonly List<IngredientIconUI> _ingredientIcons = new();

        public void Init(RecipeDefinition recipe, ItemRegistry itemRegistry)
        {
            _recipe       = recipe;
            _itemRegistry = itemRegistry;

            if (_icon != null) { _icon.enabled = recipe.Icon != null; if (recipe.Icon != null) _icon.sprite = recipe.Icon; }
            if (_craftButton != null) _craftButton.onClick.AddListener(OnCraftClicked);

            SpawnIngredientIcons();
            Refresh();
        }

        public void Refresh()
        {
            if (_recipe == null) return;

            int maxCraftable = CraftingSystem.LocalPlayer != null
                ? CraftingSystem.LocalPlayer.GetMaxCraftable(_recipe)
                : 0;

            for (int i = 0; i < _ingredientIcons.Count; i++)
            {
                var ingredient = _recipe.Ingredients[i];
                int have = CraftingSystem.LocalPlayer != null
                    ? CraftingSystem.LocalPlayer.CountItem(ingredient.ItemId)
                    : 0;
                _ingredientIcons[i].Refresh(have, ingredient.Quantity);
            }

            if (_craftButton != null)
                _craftButton.interactable = maxCraftable > 0;
        }

        private void SpawnIngredientIcons()
        {
            if (_ingredientContainer == null || _ingredientIconPrefab == null || _recipe == null) return;

            foreach (var ingredient in _recipe.Ingredients)
            {
                var go = Instantiate(_ingredientIconPrefab, _ingredientContainer);
                var ui = go.GetComponent<IngredientIconUI>();
                if (ui == null) continue;

                Sprite icon = null;
                if (_itemRegistry != null && _itemRegistry.TryGetById(ingredient.ItemId, out var def))
                    icon = def.Icon;
                ui.Init(icon);
                _ingredientIcons.Add(ui);
            }
        }

        private void OnCraftClicked()
        {
            CraftingSystem.LocalPlayer?.RequestCraft(_recipe.RecipeId, 1);
        }
    }
}
