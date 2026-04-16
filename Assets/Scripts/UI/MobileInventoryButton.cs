using LushWorld.Inventory;
using UnityEngine;

namespace LushWorld.UI
{
    // Attach to a mobile UI Button GameObject.
    // Wire the Button's OnClick to this component's OnBackpackButtonPressed().
    public class MobileInventoryButton : MonoBehaviour
    {
        public void OnBackpackButtonPressed()
        {
            InventorySystem.LocalPlayer?.RequestToggleBackpack();
        }
    }
}
