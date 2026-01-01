using DraftSpec.Cli.DependencyInjection;

namespace DraftSpec.Tests.Cli.DependencyInjection;

public class IsolatedAssemblyLoaderTests
{
    [Test]
    public async Task GetPublicKeyToken_SignedAssembly_ReturnsToken()
    {
        // System.Text.Json is a signed .NET assembly
        var assemblyPath = typeof(System.Text.Json.JsonSerializer).Assembly.Location;
        var loader = new IsolatedAssemblyLoader();

        var token = loader.GetPublicKeyToken(assemblyPath);

        // Microsoft's public key token for .NET libraries
        await Assert.That(token).IsNotNull();
        await Assert.That(token!.Length).IsEqualTo(16); // 8 bytes = 16 hex chars
    }

    [Test]
    public async Task GetPublicKeyToken_UnsignedAssembly_ReturnsNull()
    {
        // DraftSpec.dll is not signed
        var assemblyPath = typeof(DraftSpec.Dsl).Assembly.Location;
        var loader = new IsolatedAssemblyLoader();

        var token = loader.GetPublicKeyToken(assemblyPath);

        await Assert.That(token).IsNull();
    }

    [Test]
    public async Task GetPublicKeyToken_NonExistentPath_ReturnsNull()
    {
        var loader = new IsolatedAssemblyLoader();

        var token = loader.GetPublicKeyToken("/non/existent/path.dll");

        await Assert.That(token).IsNull();
    }

    [Test]
    public async Task GetPublicKeyToken_InvalidFile_ReturnsNull()
    {
        // Use a text file instead of an assembly
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "not an assembly");
            var loader = new IsolatedAssemblyLoader();

            var token = loader.GetPublicKeyToken(tempFile);

            await Assert.That(token).IsNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task GetPublicKeyToken_ReturnsLowercaseHexString()
    {
        // System.Text.Json is a signed .NET assembly
        var assemblyPath = typeof(System.Text.Json.JsonSerializer).Assembly.Location;
        var loader = new IsolatedAssemblyLoader();

        var token = loader.GetPublicKeyToken(assemblyPath);

        await Assert.That(token).IsNotNull();
        // Verify it's lowercase hex (only contains 0-9 and a-f)
        await Assert.That(token!.All(c => char.IsAsciiDigit(c) || (c >= 'a' && c <= 'f'))).IsTrue();
    }

    [Test]
    public async Task GetCertificateThumbprint_NonExistentPath_ReturnsNull()
    {
        var loader = new IsolatedAssemblyLoader();

        var thumbprint = loader.GetCertificateThumbprint("/non/existent/path.dll");

        await Assert.That(thumbprint).IsNull();
    }

    [Test]
    public async Task GetCertificateThumbprint_InvalidFile_ReturnsNull()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "not an assembly");
            var loader = new IsolatedAssemblyLoader();

            var thumbprint = loader.GetCertificateThumbprint(tempFile);

            await Assert.That(thumbprint).IsNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task GetCertificateThumbprint_UnsignedAssembly_ReturnsNull()
    {
        // DraftSpec.dll is not Authenticode-signed
        var assemblyPath = typeof(DraftSpec.Dsl).Assembly.Location;
        var loader = new IsolatedAssemblyLoader();

        var thumbprint = loader.GetCertificateThumbprint(assemblyPath);

        await Assert.That(thumbprint).IsNull();
    }

    [Test]
    public async Task GetCertificateThumbprint_StrongNamedButNotAuthenticode_ReturnsNull()
    {
        // System.Text.Json is strong-named but not Authenticode-signed on non-Windows
        // On Windows it may be Authenticode-signed, so we just verify the format if present
        var assemblyPath = typeof(System.Text.Json.JsonSerializer).Assembly.Location;
        var loader = new IsolatedAssemblyLoader();

        var thumbprint = loader.GetCertificateThumbprint(assemblyPath);

        // Either null (not Authenticode-signed) or valid SHA256 thumbprint format
        if (thumbprint != null)
        {
            // SHA256 = 32 bytes = 64 hex characters
            await Assert.That(thumbprint.Length).IsEqualTo(64);
            // Verify it's uppercase hex (X509Certificate2.GetCertHashString returns uppercase)
            await Assert.That(thumbprint.All(c => char.IsAsciiDigit(c) || (c >= 'A' && c <= 'F'))).IsTrue();
        }
    }
}
