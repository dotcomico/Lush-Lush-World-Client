using UnityEngine;

namespace LushWorld.Utilities
{
    public static class DistanceUtils
    {
        // sqrMagnitude avoids sqrt — faster than Vector3.Distance for all proximity checks.
        public static bool IsWithinRadius(Vector3 a, Vector3 b, float radius)
            => (a - b).sqrMagnitude <= radius * radius;

        // Returns the closest Transform within radius, or null if none qualify.
        public static Transform GetClosestWithinRadius(Vector3 origin, Transform[] candidates, float radius)
        {
            Transform closest = null;
            float bestSqr = radius * radius;
            foreach (var t in candidates)
            {
                if (t == null) continue;
                float sqr = (origin - t.position).sqrMagnitude;
                if (sqr <= bestSqr) { bestSqr = sqr; closest = t; }
            }
            return closest;
        }
    }
}
