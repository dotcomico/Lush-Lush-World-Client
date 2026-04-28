using System.Text;
using LushWorld.Crafting;
using LushWorld.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI
{
    // One row in the crafting menu. Instantiated at runtime by CraftingUI for each RecipeDefinition.
    // Wire all [SerializeField] fields in the row prefab in the Unity Inspector.
    public class CraftingRecipeRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _ingredientsText;
        [SerializeField] private TMP_Text _maxCraftableText;
        [SerializeField] private Button   _craftButton;

        private RecipeDefinition _recipe;
        private ItemRegistry     _itemRegistry;
        private readonly StringBuilder _sb = new();

        public void Init(RecipeDefinition recipe, ItemRegistry itemRegistry)
        {
            _recipe       = recipe;
            _itemRegistry = itemRegistry;

            if (_nameText != null)
                _nameText.text = recipe.DisplayName;

            if (_craftButton != null)
                _craftButton.onClick.AddListener(OnCraftClicked);

            Refresh();
        }

        public void Refresh()
        {
            if (_recipe == null) return;

            int maxCraftable = CraftingSystem.LocalPlayer != null
                ? CraftingSystem.LocalPlayer.GetMaxCraftable(_recipe)
                : 0;

            if (_ingredientsText != null)
            {
                _sb.Clear();
                foreach (var ingredient in _recipe.Ingredients)
                {
                    string displayName = ingredient.ItemId;
                    if (_itemRegistry != null && _itemRegistry.TryGetById(ingredient.ItemId, out var def))
                        displayName = def.DisplayName;

                    int have = CraftingSystem.LocalPlayer != null
                        ? CraftingSystem.LocalPlayer.CountItem(ingredient.ItemId)
                        : 0;

                    if (_sb.Length > 0) _sb.Append("  ");
                    _sb.Append($"{displayName}: {have}/{ingredient.Quantity}");
                }
                _ingredientsText.text = _sb.ToString();
            }

            if (_maxCraftableText != null)
                _maxCraftableText.text = $"Can craft: {maxCraftable}";

            if (_craftButton != null)
                _craftButton.interactable = maxCraftable > 0;
        }

        private void OnCraftClicked()
        {
            CraftingSystem.LocalPlayer?.RequestCraft(_recipe.RecipeId, 1);
        }
    }
}
