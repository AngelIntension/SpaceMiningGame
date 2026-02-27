using System;
using UnityEngine;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Camera.Systems
{
    /// <summary>
    /// Pure reducer for camera state. No side effects, deterministic.
    /// See MVP-02: Camera orbit and zoom.
    /// Signature: (CameraState, ICameraAction) -> CameraState
    /// </summary>
    public static class CameraReducer
    {
        private const float MinPitch = -80f;
        private const float MaxPitch = 80f;
        private const float MinDistance = 5f;
        private const float MaxDistance = 50f;
        private const float MinZoomDistance = 10f;
        private const float MaxZoomDistance = 40f;

        /// <summary>
        /// Reduce camera state by applying a camera action. See MVP-02: Camera orbit and zoom.
        /// </summary>
        public static CameraState Reduce(CameraState state, ICameraAction action)
            => action switch
            {
                OrbitAction a => ApplyOrbit(state, a),
                ZoomAction a => ApplyZoom(state, a),
                SpeedZoomAction a => ApplySpeedZoom(state, a),
                ToggleFreeLookAction => ApplyToggleFreeLook(state),
                FreeLookAction a => ApplyFreeLook(state, a),
                _ => state
            };

        private static CameraState ApplyOrbit(CameraState state, OrbitAction action)
        {
            var newYaw = state.OrbitYaw + action.DeltaYaw;
            var newPitch = Mathf.Clamp(state.OrbitPitch + action.DeltaPitch, MinPitch, MaxPitch);
            return state with { OrbitYaw = newYaw, OrbitPitch = newPitch };
        }

        private static CameraState ApplyZoom(CameraState state, ZoomAction action)
        {
            var newDistance = Mathf.Clamp(state.TargetDistance + action.Delta, MinDistance, MaxDistance);
            return state with { TargetDistance = newDistance };
        }

        private static CameraState ApplySpeedZoom(CameraState state, SpeedZoomAction action)
        {
            // Lerp between MinZoomDistance (fast) and MaxZoomDistance (slow)
            // NormalizedSpeed 0 -> MaxZoomDistance, NormalizedSpeed 1 -> MinZoomDistance
            var speed = Mathf.Clamp01(action.NormalizedSpeed);
            var targetDist = Mathf.Lerp(MaxZoomDistance, MinZoomDistance, speed);
            var newDistance = Mathf.Clamp(targetDist, MinDistance, MaxDistance);
            return state with { TargetDistance = newDistance };
        }

        private static CameraState ApplyToggleFreeLook(CameraState state)
        {
            var newActive = !state.FreeLookActive;
            // Reset offsets when toggling (either direction per data-model.md)
            return state with
            {
                FreeLookActive = newActive,
                FreeLookYaw = 0f,
                FreeLookPitch = 0f
            };
        }

        private static CameraState ApplyFreeLook(CameraState state, FreeLookAction action)
        {
            if (!state.FreeLookActive)
                return state;

            var newYaw = state.FreeLookYaw + action.DeltaYaw;
            var newPitch = Mathf.Clamp(state.FreeLookPitch + action.DeltaPitch, MinPitch, MaxPitch);
            return state with { FreeLookYaw = newYaw, FreeLookPitch = newPitch };
        }
    }
}
