using VContainer;
using VContainer.Unity;

/// <summary>
/// Per-scene DI scope. Child of RootLifetimeScope; registers scene-specific services.
/// See Constitution § VI: Explicit Over Implicit.
/// </summary>
public class SceneLifetimeScope : LifetimeScope
{
    /// <summary>
    /// Configure scene-level DI bindings. Currently empty in MVP.
    /// </summary>
    protected override void Configure(IContainerBuilder builder)
    {
    }
}
