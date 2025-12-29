using System.Reflection;
using System.Runtime.Loader;

namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Abstraction for loading assemblies and creating instances.
/// </summary>
public interface IAssemblyLoader
{
    /// <summary>
    /// Loads an assembly from the specified path.
    /// </summary>
    Assembly? LoadAssembly(string path);

    /// <summary>
    /// Gets all exported types from an assembly.
    /// </summary>
    IEnumerable<Type> GetExportedTypes(Assembly assembly);

    /// <summary>
    /// Creates an instance of the specified type.
    /// </summary>
    object? CreateInstance(Type type);
}

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
