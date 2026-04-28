using System.Collections.Generic;
using LushWorld.Data;
using UnityEngine;

namespace LushWorld.Building
{
    [CreateAssetMenu(fileName = "NewBuildingDefinition", menuName = "Lush World/Building Definition")]
    public class BuildingDefinition : ScriptableObject
    {
        [field: SerializeField] public string PieceId         { get; private set; }
        [field: SerializeField] public string DisplayName     { get; private set; }
        [field: SerializeField] public Sprite Icon            { get; private set; }
        [field: SerializeField] public GameObject PlacedPrefab { get; private set; }
        [field: SerializeField] public List<Ingredient> Cost  { get; private set; } = new();
        [field: SerializeField] public float MaxHealth        { get; private set; } = 100f;
        [field: SerializeField] public float SnapSize         { get; private set; } = 0.5f;
    }
}
