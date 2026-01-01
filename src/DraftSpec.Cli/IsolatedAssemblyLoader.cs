using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

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

    public string? GetCertificateThumbprint(string path)
    {
        try
        {
            return ExtractAuthenticodeThumbprint(path);
        }
        catch
        {
            // File is not Authenticode-signed, not found, or other error
            return null;
        }
    }

    /// <summary>
    /// Extracts the SHA256 thumbprint from an Authenticode-signed file.
    /// Excluded from coverage: requires Windows Authenticode-signed file to test.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static string ExtractAuthenticodeThumbprint(string path)
    {
        // Note: CreateFromSignedFile is marked obsolete (SYSLIB0057) but there's no
        // direct replacement in X509CertificateLoader for extracting Authenticode
        // signatures. The method still works and is the only cross-platform option.
#pragma warning disable SYSLIB0057
        using var cert = X509Certificate2.CreateFromSignedFile(path);
#pragma warning restore SYSLIB0057

        // Compute SHA256 thumbprint (more secure than default SHA1)
        return cert.GetCertHashString(HashAlgorithmName.SHA256);
    }
}
