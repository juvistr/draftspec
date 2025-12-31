using System.Reflection;
using System.Runtime.Loader;

namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Custom AssemblyLoadContext for plugin isolation.
/// </summary>
internal class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to resolve from plugin directory first
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
            return LoadFromAssemblyPath(assemblyPath);

        // Fall back to default context for shared framework assemblies
        return null;
    }
}
