using LushWorld.Resource;
using UnityEngine;

namespace LushWorld.UI
{
    // Attach to the mobile pickup button GameObject.
    // Wire Button.OnClick → OnPickupButtonPressed().
    // Future: add Image reference here to swap icon to the item's sprite.
    public class MobilePickupButton : MonoBehaviour
    {
        [SerializeField] private ResourceInteractor interactor;

        public void OnPickupButtonPressed()
        {
            interactor?.TryPickupNearest();
        }
    }
}
