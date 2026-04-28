using System;
using System.Collections.Generic;
using UnityEngine;

namespace LushWorld.Crafting
{
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "Lush World/Recipe Definition")]
    public class RecipeDefinition : ScriptableObject
    {
        public enum RecipeCategory { Tools, Food, Other }

        [Serializable]
        public struct Ingredient
        {
            public string ItemId;
            public int Quantity;
        }

        [field: SerializeField] public string RecipeId       { get; private set; }
        [field: SerializeField] public string DisplayName    { get; private set; }
        [field: SerializeField] public RecipeCategory Category { get; private set; }
        [field: SerializeField] public List<Ingredient> Ingredients { get; private set; } = new();
        [field: SerializeField] public string OutputItemId   { get; private set; }
        [field: SerializeField] public int OutputQuantity    { get; private set; } = 1;
    }
}
