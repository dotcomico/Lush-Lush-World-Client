using UnityEngine;

namespace LushWorld.Utilities
{
    // Pre-cached WaitForSeconds instances — reuse these instead of allocating inside loops.
    // For custom intervals, allocate once outside the loop with: var w = new WaitForSeconds(x);
    public static class CoroutineUtils
    {
        public static readonly WaitForSeconds Wait0_1  = new(0.1f);
        public static readonly WaitForSeconds Wait0_25 = new(0.25f);
        public static readonly WaitForSeconds Wait0_5  = new(0.5f);
        public static readonly WaitForSeconds Wait1    = new(1f);
        public static readonly WaitForSeconds Wait2    = new(2f);
        public static readonly WaitForSeconds Wait5    = new(5f);
        public static readonly WaitForSeconds Wait30   = new(30f);
    }
}
