using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Input.Views
{
    /// <summary>
    /// Debug tool: press F9 to reduce hull integrity by 25%.
    /// Attach to any GameObject in the scene for testing repair functionality.
    /// </summary>
    public sealed class DebugHullDamage : MonoBehaviour
    {
        private IStateStore _stateStore;

        [Inject]
        public void Construct(IStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        private void Update()
        {
            if (_stateStore == null) return;

            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                float current = _stateStore.Current.ActiveShipPhysics.HullIntegrity;
                float damaged = Mathf.Max(0.05f, current - 0.25f);
                _stateStore.Dispatch(new RepairHullAction(damaged));
                Debug.Log($"[DebugHullDamage] Hull: {current:P0} → {damaged:P0}");
            }
        }
    }
}
