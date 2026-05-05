using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI.Crafting
{
    // One ingredient slot displayed inside a crafting row (icon + have/need badge).
    // Instantiated at runtime by CraftingRecipeRowUI for each ingredient in a recipe.
    public class IngredientIconUI : MonoBehaviour
    {
        [SerializeField] private Image    _icon;
        [SerializeField] private TMP_Text _quantityText;

        static readonly Color ColorEnough = new Color(0.2f, 0.85f, 0.2f);
        static readonly Color ColorShort  = new Color(0.85f, 0.2f, 0.2f);

        public void Init(Sprite icon)
        {
            _icon.enabled = icon != null;
            if (icon) _icon.sprite = icon;
        }

        public void Refresh(int have, int need)
        {
            _quantityText.text  = $"{have}/{need}";
            _quantityText.color = have >= need ? ColorEnough : ColorShort;
        }
    }
}
