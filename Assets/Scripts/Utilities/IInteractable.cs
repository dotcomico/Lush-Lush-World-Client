using UnityEngine;

namespace LushWorld.Utilities
{
    // Implemented by any world object the player can interact with via the E key.
    // ResourceInteractor detects this interface in range and calls Interact on input.
    // Phase 2: Interact becomes an [ServerRpc] call routed through the interactable's NetworkBehaviour.
    public interface IInteractable
    {
        string InteractionLabel { get; }
        void Interact(GameObject player);
    }
}
