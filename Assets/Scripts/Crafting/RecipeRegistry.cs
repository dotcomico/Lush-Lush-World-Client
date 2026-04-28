using System.Collections.Generic;
using UnityEngine;

namespace LushWorld.Crafting
{
    // Singleton ScriptableObject asset. Holds all recipes; builds a fast dictionary on load.
    // Assign one instance via Inspector on CraftingSystem (PlayerCapsule).
    [CreateAssetMenu(fileName = "RecipeRegistry", menuName = "Lush World/Recipe Registry")]
    public class RecipeRegistry : ScriptableObject
    {
        [SerializeField] private List<RecipeDefinition> _recipes = new();

        public IReadOnlyList<RecipeDefinition> Recipes => _recipes;

        private Dictionary<string, RecipeDefinition> _lookup;

        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, RecipeDefinition>(_recipes.Count);
            foreach (var recipe in _recipes)
            {
                if (recipe == null || string.IsNullOrEmpty(recipe.RecipeId)) continue;
                if (!_lookup.TryAdd(recipe.RecipeId, recipe))
                    Debug.LogWarning($"[RecipeRegistry] Duplicate RecipeId: '{recipe.RecipeId}'", this);
            }
        }

        public bool TryGetRecipe(string id, out RecipeDefinition recipe)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(id, out recipe);
        }
    }
}
