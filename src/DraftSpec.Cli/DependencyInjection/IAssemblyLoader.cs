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
