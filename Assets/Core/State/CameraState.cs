namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Immutable camera state driven by CameraReducer.
    /// See MVP-02: Camera orbit and zoom.
    /// </summary>
    public sealed record CameraState(
        float OrbitYaw,
        float OrbitPitch,
        float TargetDistance,
        bool FreeLookActive,
        float FreeLookYaw,
        float FreeLookPitch,
        float MinPitch = -80f,
        float MaxPitch = 80f,
        float MinDistance = 5f,
        float MaxDistance = 50f,
        float MinZoomDistance = 10f,
        float MaxZoomDistance = 40f
    )
    {
        public static readonly CameraState Default = new(0f, 15f, 25f, false, 0f, 0f);
    }
}
