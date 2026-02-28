using UnityEngine;
using VContainer;
using VContainer.Unity;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Procedural.Data;
using VoidHarvest.Features.Docking.Data;

/// <summary>
/// Per-scene DI scope. Child of RootLifetimeScope; registers scene-specific services.
/// See Constitution § VI: Explicit Over Implicit.
/// </summary>
public class SceneLifetimeScope : LifetimeScope
{
    [Header("Mining VFX Configs")]
    [SerializeField] private MiningVFXConfig miningVFXConfig;
    [SerializeField] private DepletionVFXConfig depletionVFXConfig;
    [SerializeField] private OreChunkConfig oreChunkConfig;
    [SerializeField] private MiningAudioConfig miningAudioConfig;

    [Header("Visual Mapping")]
    [SerializeField] private AsteroidVisualMappingConfig asteroidVisualMappingConfig;

    [Header("Docking")]
    [SerializeField] private DockingConfig dockingConfig;
    [SerializeField] private DockingVFXConfig dockingVFXConfig;
    [SerializeField] private DockingAudioConfig dockingAudioConfig;

    /// <summary>
    /// Configure scene-level DI bindings. Registers VFX ScriptableObject configs
    /// for injection into mining view-layer MonoBehaviours.
    /// </summary>
    protected override void Configure(IContainerBuilder builder)
    {
        if (miningVFXConfig != null)
            builder.RegisterInstance(miningVFXConfig);
        if (depletionVFXConfig != null)
            builder.RegisterInstance(depletionVFXConfig);
        if (oreChunkConfig != null)
            builder.RegisterInstance(oreChunkConfig);
        if (miningAudioConfig != null)
            builder.RegisterInstance(miningAudioConfig);
        if (asteroidVisualMappingConfig != null)
            builder.RegisterInstance(asteroidVisualMappingConfig);
        if (dockingConfig != null)
            builder.RegisterInstance(dockingConfig);
        if (dockingVFXConfig != null)
            builder.RegisterInstance(dockingVFXConfig);
        if (dockingAudioConfig != null)
            builder.RegisterInstance(dockingAudioConfig);
    }
}
