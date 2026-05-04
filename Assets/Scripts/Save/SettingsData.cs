using System.Collections.Generic;

namespace LushWorld.Save
{
    [System.Serializable]
    public class GameSettings
    {
        public int   cameraMode      = 0;   // 0=FP, 1=TP, 2=ISO
        public int   slideMode       = 0;   // 0=None, 1=Medium, 2=Cinematic
        public float tpDistance      = 5f;
        public float fpFov           = 80f;
        public float lookSensitivity = 1f;

        public List<MobileElementLayoutData> mobileLayout = new();

        public static GameSettings Default => new GameSettings();
    }
}
