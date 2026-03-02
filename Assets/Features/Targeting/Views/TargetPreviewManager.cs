using UnityEngine;
using UnityEngine.Rendering;
using VContainer;
using VoidHarvest.Core.Extensions;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Targeting.Data;
using VoidHarvest.Features.Mining.Data;
using Unity.Entities;
using Unity.Transforms;

namespace VoidHarvest.Features.Targeting.Views
{
    /// <summary>
    /// Manages preview slots for target card viewports. Each locked target gets
    /// an isolated clone + camera + RenderTexture on the "TargetPreview" layer.
    /// See Spec 007: In-Flight Targeting (FR-028).
    /// </summary>
    public sealed class TargetPreviewManager : MonoBehaviour
    {
        private struct PreviewSlot
        {
            public int TargetId;
            public TargetType TargetType;
            public GameObject Clone;
            public Camera PreviewCamera;
            public RenderTexture RenderTexture;
            public bool Active;
        }

        private TargetingConfig _config;
        private IStateStore _stateStore;
        private PreviewSlot[] _slots;
        private int _previewLayer;

        private EntityManager _entityManager;
        private bool _ecsReady;

        [Inject]
        public void Construct(TargetingConfig config, IStateStore stateStore)
        {
            _config = config;
            _stateStore = stateStore;
        }

        private void Start()
        {
            _previewLayer = LayerMask.NameToLayer("TargetPreview");
            _slots = new PreviewSlot[6];

            TryInitializeECS();

            // Exclude TargetPreview layer from main camera
            var mainCam = Camera.main;
            if (mainCam != null && _previewLayer >= 0)
            {
                mainCam.cullingMask &= ~(1 << _previewLayer);
            }
        }

        private void TryInitializeECS()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            _entityManager = world.EntityManager;
            _ecsReady = true;
        }

        /// <summary>
        /// Get the RenderTexture for a locked target's preview. Creates slot if needed.
        /// </summary>
        public RenderTexture GetOrCreatePreview(int targetId, TargetType targetType)
        {
            // Check existing
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Active && _slots[i].TargetId == targetId)
                    return _slots[i].RenderTexture;
            }

            // Find free slot
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].Active)
                {
                    CreateSlot(ref _slots[i], i, targetId, targetType);
                    return _slots[i].RenderTexture;
                }
            }

            return null;
        }

        /// <summary>
        /// Release a preview slot when a target is unlocked.
        /// </summary>
        public void ReleasePreview(int targetId)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Active && _slots[i].TargetId == targetId)
                {
                    DestroySlot(ref _slots[i]);
                    return;
                }
            }
        }

        /// <summary>
        /// Release all preview slots.
        /// </summary>
        public void ReleaseAll()
        {
            if (_slots == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Active)
                    DestroySlot(ref _slots[i]);
            }
        }

        private void CreateSlot(ref PreviewSlot slot, int index, int targetId, TargetType targetType)
        {
            int rtSize = _config != null ? _config.ViewportRenderSize : 128;
            float fov = _config != null ? _config.ViewportFOV : 30f;
            Vector3 stageOffset = _config != null ? _config.PreviewStageOffset : new Vector3(0f, -1000f, 0f);

            Vector3 slotPos = stageOffset + new Vector3(index * 50f, 0f, 0f);

            // Create clone
            var clone = CreateClone(targetId, targetType, slotPos);
            if (clone == null) return;

            SetLayerRecursive(clone, _previewLayer);

            // Create RenderTexture
            var rt = new RenderTexture(rtSize, rtSize, 16);
            rt.name = $"TargetPreview_{targetId}";

            // Create preview camera
            var camGO = new GameObject($"PreviewCam_{targetId}");
            camGO.transform.SetParent(transform);
            var cam = camGO.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.targetTexture = rt;
            cam.cullingMask = 1 << _previewLayer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.05f, 0.09f, 1f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;

            // Position camera looking at clone
            float viewDistance = GetViewDistance(clone);
            cam.transform.position = slotPos + new Vector3(0f, viewDistance * 0.3f, -viewDistance);
            cam.transform.LookAt(slotPos);

            slot = new PreviewSlot
            {
                TargetId = targetId,
                TargetType = targetType,
                Clone = clone,
                PreviewCamera = cam,
                RenderTexture = rt,
                Active = true
            };
        }

        private GameObject CreateClone(int targetId, TargetType targetType, Vector3 position)
        {
            if (targetType == TargetType.Asteroid)
                return CreateAsteroidClone(targetId, position);
            if (targetType == TargetType.Station)
                return CreateStationClone(targetId, position);
            return null;
        }

        private GameObject CreateAsteroidClone(int targetId, Vector3 position)
        {
            if (!_ecsReady) TryInitializeECS();
            if (!_ecsReady) return null;

            float radius = 1f;
            Color tint = Color.gray;

            // Get radius from ECS
            var query = _entityManager.CreateEntityQuery(
                typeof(AsteroidComponent), typeof(LocalTransform));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (entities[i].Index == targetId)
                {
                    var asteroid = _entityManager.GetComponentData<AsteroidComponent>(entities[i]);
                    radius = asteroid.Radius;

                    if (_entityManager.HasComponent<AsteroidOreComponent>(entities[i]))
                    {
                        int oreId = _entityManager.GetComponentData<AsteroidOreComponent>(entities[i]).OreTypeId;
                        tint = GetOreTint(oreId);
                    }
                    break;
                }
            }
            entities.Dispose();

            // Create sphere primitive as visual stand-in
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"AsteroidPreview_{targetId}";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * radius * 2f;

            // Remove collider (preview only)
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Tint the material
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(renderer.sharedMaterial);
                mat.color = tint;
                renderer.material = mat;
            }

            go.transform.SetParent(transform);
            return go;
        }

        private GameObject CreateStationClone(int targetId, Vector3 position)
        {
            var stations = FindObjectsByType<TargetableStation>(FindObjectsSortMode.None);
            foreach (var station in stations)
            {
                if (station.TargetId == targetId)
                {
                    var clone = Instantiate(station.gameObject, position,
                        Quaternion.identity, transform);
                    clone.name = $"StationPreview_{targetId}";

                    // Remove all non-visual components
                    foreach (var mb in clone.GetComponentsInChildren<MonoBehaviour>())
                        Destroy(mb);
                    foreach (var col in clone.GetComponentsInChildren<Collider>())
                        Destroy(col);

                    return clone;
                }
            }
            return null;
        }

        private void LateUpdate()
        {
            if (_slots == null) return;
            if (!_ecsReady) TryInitializeECS();

            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].Active) continue;

                // Slowly rotate clone for visual interest
                if (_slots[i].Clone != null)
                {
                    _slots[i].Clone.transform.Rotate(Vector3.up, 20f * Time.deltaTime, Space.World);
                }
            }
        }

        private void DestroySlot(ref PreviewSlot slot)
        {
            if (slot.Clone != null) Destroy(slot.Clone);
            if (slot.PreviewCamera != null) Destroy(slot.PreviewCamera.gameObject);
            if (slot.RenderTexture != null)
            {
                slot.RenderTexture.Release();
                Destroy(slot.RenderTexture);
            }
            slot = default;
        }

        private void OnDestroy()
        {
            ReleaseAll();
        }

        private float GetViewDistance(GameObject clone)
        {
            var renderers = clone.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 5f;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.extents.magnitude * 2.5f;
        }

        private static Color GetOreTint(int oreTypeId)
        {
            return oreTypeId switch
            {
                0 => new Color(0.4f, 0.7f, 0.3f, 1f),  // Luminite (green)
                1 => new Color(0.6f, 0.35f, 0.2f, 1f),  // Ferrox (orange-brown)
                2 => new Color(0.5f, 0.3f, 0.8f, 1f),   // Auralite (purple)
                _ => Color.gray
            };
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            if (layer < 0) return;
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
