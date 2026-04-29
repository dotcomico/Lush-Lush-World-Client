using Unity.Cinemachine;
using UnityEngine;

namespace LushWorld.Camera
{
    // Cinemachine pipeline extension that applies Perlin shake in the Finalize
    // stage — runs after all body/aim/noise, inside Cinemachine's own update.
    // Added programmatically by CameraViewController; do not add manually.
    [AddComponentMenu("")]
    public class CameraShakeExtension : CinemachineExtension
    {
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize) return;

            var shaker = CameraShaker.Instance;
            if (shaker == null || !shaker.IsActive) return;

            // Low frequency (8 Hz) = heavy earth-moving feel
            float t = Time.time * 8f;
            float x = (Mathf.PerlinNoise(t,        0.5f) - 0.5f) * 2f * shaker.CurrentAmplitude;
            float y = (Mathf.PerlinNoise(0.5f + t,  1.5f) - 0.5f) * 2f * shaker.CurrentAmplitude;

            // Apply in camera-local space so shake is always sideways + up/down on screen
            state.PositionCorrection += state.RawOrientation * new Vector3(x, y, 0f);
        }
    }
}
