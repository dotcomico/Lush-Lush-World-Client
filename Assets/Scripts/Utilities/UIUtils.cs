using System.Collections.Generic;
using UnityEngine;

namespace LushWorld.Utilities
{
    public static class UIUtils
    {
        // Safe SetActive — silently skips null references.
        public static void SetVisible(GameObject go, bool visible)
        {
            if (go != null) go.SetActive(visible);
        }

        // Toggle the current active state.
        public static void Toggle(GameObject go)
        {
            if (go != null) go.SetActive(!go.activeSelf);
        }

        // Bulk show/hide — set multiple UI groups in one call.
        public static void SetAllVisible(IEnumerable<GameObject> objects, bool visible)
        {
            foreach (var go in objects)
                SetVisible(go, visible);
        }
    }
}
