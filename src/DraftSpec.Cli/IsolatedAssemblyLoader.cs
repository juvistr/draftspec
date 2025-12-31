using System.Reflection;
using System.Runtime.Loader;

namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Default implementation that loads assemblies in isolation using AssemblyLoadContext.
/// </summary>
public class IsolatedAssemblyLoader : IAssemblyLoader
{
    public Assembly? LoadAssembly(string path)
    {
        var context = new PluginLoadContext(path);
        return context.LoadFromAssemblyPath(path);
    }

    public IEnumerable<Type> GetExportedTypes(Assembly assembly)
        => assembly.GetExportedTypes();

    public object? CreateInstance(Type type)
        => Activator.CreateInstance(type);
}
