// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for C# 9 record init-only setters.
    /// Required because Unity targets .NET Framework which does not define this type.
    /// </summary>
    public static class IsExternalInit { }
}
