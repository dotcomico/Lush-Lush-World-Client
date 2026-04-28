using System.Text;
using LushWorld.Building;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI.Building
{
    // One row in the building menu. Instantiated at runtime by BuildingMenuUI for each BuildingDefinition.
    // Wire all [SerializeField] fields in the row prefab in the Unity Inspector.
    public class BuildingMenuPieceRowUI : MonoBehaviour
    {
        [SerializeField] private Image    _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private Button   _selectButton;

        private BuildingDefinition _def;
        private readonly StringBuilder _sb = new();

        public void Init(BuildingDefinition def)
        {
            _def = def;

            if (_icon != null)      { _icon.enabled = def.Icon != null; if (def.Icon != null) _icon.sprite = def.Icon; }
            if (_nameText != null)  _nameText.text = def.DisplayName;
            if (_costText != null)  _costText.text = BuildCostText(def);
            if (_selectButton != null) _selectButton.onClick.AddListener(OnSelectClicked);
        }

        private string BuildCostText(BuildingDefinition def)
        {
            if (def.Cost == null || def.Cost.Count == 0) return "Free";
            _sb.Clear();
            foreach (var ingredient in def.Cost)
            {
                if (_sb.Length > 0) _sb.Append("  ");
                _sb.Append($"{ingredient.Quantity}x {ingredient.ItemId}");
            }
            return _sb.ToString();
        }

        private void OnSelectClicked()
        {
            BuildingSystem.LocalPlayer?.EnterPlacementMode(_def);
        }
    }
}
