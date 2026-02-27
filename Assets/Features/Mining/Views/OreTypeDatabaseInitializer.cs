using UnityEngine;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Views
{
    /// <summary>
    /// Initializes the OreTypeBlobBakingSystem with OreTypeDefinition ScriptableObjects.
    /// Place on a GameObject in the scene and assign ore definitions in the inspector.
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    public sealed class OreTypeDatabaseInitializer : MonoBehaviour
    {
        [SerializeField] private OreTypeDefinition[] oreDefinitions;

        private void Awake()
        {
            if (oreDefinitions == null || oreDefinitions.Length == 0)
            {
                Debug.LogWarning("[VoidHarvest] OreTypeDatabaseInitializer: No ore definitions assigned.");
                return;
            }

            OreTypeBlobBakingSystem.SetOreDefinitions(oreDefinitions);
            Debug.Log($"[VoidHarvest] OreTypeDatabaseInitializer: Loaded {oreDefinitions.Length} ore definitions.");
        }
    }
}
