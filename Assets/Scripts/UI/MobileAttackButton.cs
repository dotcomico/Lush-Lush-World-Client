using LushWorld.Player;
using UnityEngine;

namespace LushWorld.UI
{
    // Attach to the Attack button GameObject in UI_Canvas_StarterAssetsInputs_Joysticks.
    // Wire Button.OnClick → OnAttackButtonPressed().
    // Pair with MobileCanvasVisibility on the same GO to hide it on desktop.
    public class MobileAttackButton : MonoBehaviour
    {
        public void OnAttackButtonPressed()
        {
            TongueAttack.LocalPlayer?.RequestAttack();
        }
    }
}
