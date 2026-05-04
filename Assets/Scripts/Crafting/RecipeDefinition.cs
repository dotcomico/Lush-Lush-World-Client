using System.Collections.Generic;
using LushWorld.Data;
using UnityEngine;

namespace LushWorld.Crafting
{
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "Lush World/Recipe Definition")]
    public class RecipeDefinition : ScriptableObject
    {
        public enum RecipeCategory { Tools, Food, Other }

        [field: SerializeField] public string RecipeId         { get; private set; }
        [field: SerializeField] public string DisplayName      { get; private set; }
        [field: SerializeField] public RecipeCategory Category { get; private set; }
        [field: SerializeField] public List<Ingredient> Ingredients { get; private set; } = new();
        [field: SerializeField] public string OutputItemId     { get; private set; }
        [field: SerializeField] public int    OutputQuantity   { get; private set; } = 1;
        [field: SerializeField] public Sprite Icon             { get; private set; }
    }
}
