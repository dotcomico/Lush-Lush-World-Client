using LushWorld.Building;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI.Building
{
    // One row in the deposit panel: shows ingredient name, X/Y deposited counter, and a Deposit button.
    public class BlueprintDepositRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private Button   _depositButton;

        private BlueprintInstance _blueprint;
        private string _itemId;
        private int    _required;

        public void Init(BlueprintInstance blueprint, string itemId, int required, string displayName)
        {
            _blueprint = blueprint;
            _itemId    = itemId;
            _required  = required;

            if (_nameText != null) _nameText.text = displayName;
            _depositButton?.onClick.AddListener(OnDepositClicked);
            Refresh();
        }

        public void Refresh()
        {
            int deposited = _blueprint != null ? _blueprint.GetDeposited(_itemId) : 0;
            if (_countText     != null) _countText.text         = $"{deposited}/{_required}";
            if (_depositButton != null) _depositButton.interactable = deposited < _required;
        }

        private void OnDepositClicked()
        {
            _blueprint?.RequestDeposit(_itemId, 1);
            Refresh();
        }
    }
}
