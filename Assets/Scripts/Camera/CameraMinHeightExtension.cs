using Unity.Cinemachine;
using UnityEngine;

namespace LushWorld.Camera
{
    /// <summary>
    /// Cinemachine extension that prevents the TP camera from being pulled below
    /// a minimum height above the Follow target.
    ///
    /// Why this exists: Cinemachine3rdPersonFollow collision detection
    /// (ResolveCollisions) sphere-casts from the shoulder to the desired camera
    /// position and moves the camera toward the shoulder when it hits geometry.
    /// When the snail stands on a mushroom or stick the object's collider intercepts
    /// the cast and pushes the camera below the player, producing an upward-looking
    /// view. This extension corrects the Y after collision resolution.
    /// </summary>
    [AddComponentMenu("")]   // hidden from Add Component menu — wired in CameraViewController
    public class CameraMinHeightExtension : CinemachineExtension
    {
        [Tooltip("Camera must stay at least this many units above the Follow target. " +
                 "Increase if the camera still clips into the snail body; decrease if it floats too high.")]
        public float MinHeightAboveTarget = 1.0f;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Body) return;

            var follow = vcam.Follow;
            if (follow == null) return;

            float minY = follow.position.y + MinHeightAboveTarget;
            if (state.RawPosition.y < minY)
            {
                var pos = state.RawPosition;
                pos.y = minY;
                state.RawPosition = pos;
            }
        }
    }
}
