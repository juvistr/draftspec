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

    public string? GetPublicKeyToken(string path)
    {
        try
        {
            var assemblyName = AssemblyName.GetAssemblyName(path);
            var publicKeyToken = assemblyName.GetPublicKeyToken();
            if (publicKeyToken == null || publicKeyToken.Length == 0)
                return null;
            return Convert.ToHexStringLower(publicKeyToken);
        }
        catch
        {
            return null;
        }
    }
}
