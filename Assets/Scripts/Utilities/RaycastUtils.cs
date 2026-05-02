using UnityEngine;

namespace LushWorld.Utilities
{
    public static class RaycastUtils
    {
        // Raycast from screen-centre camera ray — standard pattern for placement/interaction.
        public static bool TryRaycastFromCamera(UnityEngine.Camera cam, float maxDist, LayerMask mask, out RaycastHit hit)
        {
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            return Physics.Raycast(ray, out hit, maxDist, mask);
        }

        // Raycast from a specific screen point — mouse-driven (e.g. drop-position detection).
        public static bool TryRaycastFromScreenPoint(UnityEngine.Camera cam, Vector2 screenPos, float maxDist, LayerMask mask, out RaycastHit hit)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            return Physics.Raycast(ray, out hit, maxDist, mask);
        }

        // Downward raycast from a world position — ground detection under a ghost or spawn point.
        public static bool TryRaycastDown(Vector3 origin, float maxDist, LayerMask mask, out RaycastHit hit)
            => Physics.Raycast(origin + Vector3.up * 0.1f, Vector3.down, out hit, maxDist, mask);
    }
}
