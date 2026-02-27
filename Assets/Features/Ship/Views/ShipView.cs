using UnityEngine;
using Unity.Mathematics;
using VContainer;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Ship.Views
{
    /// <summary>
    /// Applies ShipState position/rotation to this GameObject's Transform.
    /// MonoBehaviour — no game state stored here.
    /// See MVP-01: 6DOF Newtonian flight.
    /// </summary>
    public sealed class ShipView : MonoBehaviour
    {
        private IStateStore _stateStore;
        private int _lastVersion = -1;

        /// <summary>
        /// DI injection point for the state store. See MVP-01: 6DOF Newtonian flight.
        /// </summary>
        [Inject]
        public void Construct(IStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        private void LateUpdate()
        {
            if (_stateStore == null) return;
            if (_stateStore.Version == _lastVersion) return;

            _lastVersion = _stateStore.Version;

            var shipState = _stateStore.Current.ActiveShipPhysics;
            transform.position = new Vector3(shipState.Position.x, shipState.Position.y, shipState.Position.z);
            transform.rotation = new Quaternion(
                shipState.Rotation.value.x,
                shipState.Rotation.value.y,
                shipState.Rotation.value.z,
                shipState.Rotation.value.w);
        }
    }
}
