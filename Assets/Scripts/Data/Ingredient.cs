using System;

namespace LushWorld.Data
{
    // Shared ingredient record used by both RecipeDefinition (crafting) and BuildingDefinition (building).
    [Serializable]
    public struct Ingredient
    {
        public string ItemId;
        public int Quantity;
    }
}
