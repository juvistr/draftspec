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

    /// <summary>
    /// Gets the public key token from a signed assembly.
    /// Returns null if the assembly is not signed or cannot be read.
    /// </summary>
    /// <param name="path">Path to the assembly file.</param>
    /// <returns>Lowercase hex string of the public key token, or null if not signed.</returns>
    string? GetPublicKeyToken(string path);
}
