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
        float FreeLookPitch
    )
    {
        public static readonly CameraState Default = new(0f, 15f, 25f, false, 0f, 0f);
    }
}
