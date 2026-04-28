using System.Collections.Generic;
using UnityEngine;

namespace LushWorld.Building
{
    // Singleton ScriptableObject asset. Holds all building piece definitions; builds a fast dictionary on load.
    // Assign one instance via Inspector on BuildingSystem (PlayerCapsule).
    [CreateAssetMenu(fileName = "BuildingRegistry", menuName = "Lush World/Building Registry")]
    public class BuildingRegistry : ScriptableObject
    {
        [SerializeField] private List<BuildingDefinition> _pieces = new();

        public IReadOnlyList<BuildingDefinition> Pieces => _pieces;

        private Dictionary<string, BuildingDefinition> _lookup;

        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, BuildingDefinition>(_pieces.Count);
            foreach (var piece in _pieces)
            {
                if (piece == null || string.IsNullOrEmpty(piece.PieceId)) continue;
                if (!_lookup.TryAdd(piece.PieceId, piece))
                    Debug.LogWarning($"[BuildingRegistry] Duplicate PieceId: '{piece.PieceId}'", this);
            }
        }

        public bool TryGetPiece(string id, out BuildingDefinition piece)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(id, out piece);
        }
    }
}
