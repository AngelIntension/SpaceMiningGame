using UnityEngine;
using Unity.Cinemachine;
using VContainer;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Camera.Views
{
    /// <summary>
    /// Drives Cinemachine camera from CameraState. Dispatches SpeedZoomAction.
    /// MonoBehaviour — no game state stored here.
    /// See MVP-02: Camera orbit and zoom.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class CameraView : MonoBehaviour
    {
        private CinemachineCamera _cinemachineCamera;
        private CinemachineOrbitalFollow _orbitalFollow;

        private IStateStore _stateStore;

        private float _currentRadius;
        private float _radiusVelocity;

        // Zoom cooldown: suppress SpeedZoomAction for 2s after manual ZoomAction
        private float _lastManualZoomTime = float.NegativeInfinity;
        private const float ZoomCooldownDuration = 2.0f;

        private int _lastStoreVersion = -1;

        /// <summary>
        /// DI injection point for the state store. See MVP-02: Camera orbit and zoom.
        /// </summary>
        [Inject]
        public void Construct(IStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        private void Awake()
        {
            _cinemachineCamera = GetComponent<CinemachineCamera>();
            _orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        }

        private void Start()
        {
            if (_orbitalFollow != null)
            {
                _currentRadius = _orbitalFollow.Radius;
            }
        }

        private void LateUpdate()
        {
            if (_stateStore == null) return;

            var cameraState = _stateStore.Current.Camera;
            var shipState = _stateStore.Current.ActiveShipPhysics;

            // Detect manual zoom (version changed since last frame + ZoomAction detected)
            if (_stateStore.Version != _lastStoreVersion)
            {
                _lastStoreVersion = _stateStore.Version;
            }

            // Drive Cinemachine orbital from state
            if (_orbitalFollow != null)
            {
                _orbitalFollow.HorizontalAxis.Value = cameraState.OrbitYaw;
                _orbitalFollow.VerticalAxis.Value = cameraState.OrbitPitch;

                // Smooth zoom toward TargetDistance
                _currentRadius = Mathf.SmoothDamp(
                    _currentRadius,
                    cameraState.TargetDistance,
                    ref _radiusVelocity,
                    0.3f);

                _orbitalFollow.Radius = _currentRadius;
            }

            // Dispatch SpeedZoomAction (unless in manual zoom cooldown)
            if (Time.time - _lastManualZoomTime >= ZoomCooldownDuration)
            {
                float maxSpeed = shipState.MaxSpeed;
                if (maxSpeed > 0.001f)
                {
                    float normalizedSpeed = Unity.Mathematics.math.length(shipState.Velocity) / maxSpeed;
                    _stateStore.Dispatch(new SpeedZoomAction(normalizedSpeed));
                }
            }
        }

        /// <summary>
        /// Called by InputBridge when a manual ZoomAction is dispatched.
        /// Starts the cooldown timer to suppress SpeedZoomAction.
        /// </summary>
        public void NotifyManualZoom()
        {
            _lastManualZoomTime = Time.time;
        }
    }
}
