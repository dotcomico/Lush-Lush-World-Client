using TMPro;
using UnityEngine;

namespace LushWorld.World
{
    // Attached to the quantity label child on a dropped world item.
    // Billboards the label so it always faces the main camera.
    [RequireComponent(typeof(TextMeshPro))]
    public class ItemDropLabel : MonoBehaviour
    {
        private void LateUpdate()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam != null)
                transform.rotation = cam.transform.rotation;
        }
    }
}
