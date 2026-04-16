using UnityEngine;

namespace LushWorld.Inventory
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Lush World/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [field: SerializeField] public string ItemId { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public int MaxStackSize { get; private set; } = 64;
        [field: SerializeField] public bool IsDroppable { get; private set; } = true;
        [field: SerializeField] public GameObject WorldPrefab { get; private set; }
    }
}
