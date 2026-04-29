using LushWorld.Building;
using UnityEngine;

namespace LushWorld.UI
{
    // Attach to the BuildingButton GameObject inside InventoryUI.
    // Wire Button.OnClick → OnBuildingButtonPressed().
    // Pair with MobileCanvasVisibility on the same GO to hide it on desktop.
    public class MobileBuildingButton : MonoBehaviour
    {
        public void OnBuildingButtonPressed()
        {
            BuildingSystem.LocalPlayer?.ToggleBuildingMenu();
        }
    }
}
