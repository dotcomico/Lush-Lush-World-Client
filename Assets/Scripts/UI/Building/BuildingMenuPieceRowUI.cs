using System.Collections.Generic;
using LushWorld.Building;
using LushWorld.Inventory;
using LushWorld.UI.Crafting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI.Building
{
    // One row in the building menu. Instantiated at runtime by BuildingMenuUI for each BuildingDefinition.
    // Icon-based cost display mirrors CraftingRecipeRowUI — reuses IngredientIconUI and IngredientIconPrefab.
    public class BuildingMenuPieceRowUI : MonoBehaviour
    {
        [SerializeField] private Image      _icon;
        [SerializeField] private TMP_Text   _nameText;
        [SerializeField] private Transform  _ingredientContainer;
        [SerializeField] private GameObject _ingredientIconPrefab;
        [SerializeField] private Button     _selectButton;

        private BuildingDefinition              _def;
        private ItemRegistry                    _itemRegistry;
        private readonly List<IngredientIconUI> _ingredientIcons = new();

        public void Init(BuildingDefinition def, ItemRegistry itemRegistry)
        {
            _def          = def;
            _itemRegistry = itemRegistry;

            if (_icon != null)     { _icon.enabled = def.Icon != null; if (def.Icon != null) _icon.sprite = def.Icon; }
            if (_nameText != null) _nameText.text = def.DisplayName;
            if (_selectButton != null) _selectButton.onClick.AddListener(OnSelectClicked);

            SpawnIngredientIcons();
            Refresh();
        }

        public void Refresh()
        {
            if (_def == null) return;

            for (int i = 0; i < _ingredientIcons.Count; i++)
            {
                var ingredient = _def.Cost[i];
                int have = InventorySystem.LocalPlayer?.CountItem(ingredient.ItemId) ?? 0;
                _ingredientIcons[i].Refresh(have, ingredient.Quantity);
            }
        }

        private void SpawnIngredientIcons()
        {
            if (_ingredientContainer == null || _ingredientIconPrefab == null || _def?.Cost == null) return;

            foreach (var ingredient in _def.Cost)
            {
                var go = Instantiate(_ingredientIconPrefab, _ingredientContainer);
                var ui = go.GetComponent<IngredientIconUI>();
                if (ui == null) continue;

                Sprite icon = null;
                if (_itemRegistry != null && _itemRegistry.TryGetById(ingredient.ItemId, out var itemDef))
                    icon = itemDef.Icon;
                ui.Init(icon);
                _ingredientIcons.Add(ui);
            }
        }

        private void OnSelectClicked() => BuildingSystem.LocalPlayer?.EnterPlacementMode(_def);
    }
}
