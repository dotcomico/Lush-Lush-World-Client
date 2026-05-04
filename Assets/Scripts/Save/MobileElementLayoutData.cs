using System;
using UnityEngine;

namespace LushWorld.Save
{
    [Serializable]
    public class MobileElementLayoutData
    {
        public string  elementId;
        public Vector2 anchoredPosition;
        public float   scale = 1f;
        public float   alpha = 1f;
    }
}
