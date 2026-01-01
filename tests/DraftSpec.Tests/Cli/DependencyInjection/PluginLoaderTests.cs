using System.Reflection;
using DraftSpec.Cli;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Formatters;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.DependencyInjection;

/// <summary>
/// Tests for PluginLoader class.
/// </summary>
public class PluginLoaderTests
{
    #region Constructor Tests

    [Test]
    public async Task PluginLoader_NoDirectories_UsesDefaultPluginDirectory()
    {
        var scanner = new MockPluginScanner();
        var assemblyLoader = new MockAssemblyLoader();
        var console = new MockConsole();

        var loader = new PluginLoader(scanner, assemblyLoader, console);

        // Should use default plugin directory when no directories are specified
        var plugins = loader.DiscoverPlugins();

        // The scanner should have been called with the default directory
        await Assert.That(scanner.CheckedDirectories.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task PluginLoader_CustomDirectories_UsesProvidedDirectories()
    {
        var scanner = new MockPluginScanner();
        var assemblyLoader = new MockAssemblyLoader();
        var console = new MockConsole();

        var customDir = "/custom/plugins";
        var loader = new PluginLoader(scanner, assemblyLoader, console);

        loader.DiscoverPlugins([customDir]);

        await Assert.That(scanner.CheckedDirectories).Contains(customDir);
    }

    #endregion

    #region DiscoverPlugins Tests

    [Test]
    public async Task DiscoverPlugins_NoDirectoryExists_ReturnsEmpty()
    {
        var scanner = new MockPluginScanner();
        var assemblyLoader = new MockAssemblyLoader();
        var console = new MockConsole();

        var loader = new PluginLoader(scanner, assemblyLoader, console);

        var plugins = loader.DiscoverPlugins(["/nonexistent"]);

        await Assert.That(plugins.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task DiscoverPlugins_EmptyDirectory_ReturnsEmpty()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        // No files in the directory

        var assemblyLoader = new MockAssemblyLoader();
        var console = new MockConsole();

        var loader = new PluginLoader(scanner, assemblyLoader, console);

        var plugins = loader.DiscoverPlugins(["/plugins"]);

        await Assert.That(plugins.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task DiscoverPlugins_CachesResults_OnSecondCall()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));

        var console = new MockConsole();
        var loader = new PluginLoader(scanner, assemblyLoader, console);

        // First call
        var plugins1 = loader.DiscoverPlugins(["/plugins"]).ToList();

        // Reset counters
        scanner.CheckedDirectories.Clear();
        scanner.ScannedDirectories.Clear();

        // Second call
        var plugins2 = loader.DiscoverPlugins(["/plugins"]).ToList();

        // Should return cached results without scanning again
        await Assert.That(scanner.CheckedDirectories.Count).IsEqualTo(0);
        await Assert.That(plugins1.Count).IsEqualTo(plugins2.Count);
    }

    [Test]
    public async Task DiscoverPlugins_LoadError_LogsWarningAndContinues()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.BadPlugin.dll");
        scanner.AddPluginFile("/plugins", "DraftSpec.GoodPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        // BadPlugin throws exception
        assemblyLoader.SetLoadException("/plugins/DraftSpec.BadPlugin.dll", new InvalidOperationException("Corrupted assembly"));

        // GoodPlugin loads successfully - use the test formatter type
        assemblyLoader.AddRealType("/plugins/DraftSpec.GoodPlugin.dll", typeof(TestFormatterWithAttribute));

        var console = new MockConsole();
        var loader = new PluginLoader(scanner, assemblyLoader, console);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        // Should have logged error for BadPlugin
        await Assert.That(console.Errors).Contains("DraftSpec.BadPlugin.dll");
        await Assert.That(console.Errors).Contains("Corrupted assembly");

        // Should have successfully loaded GoodPlugin
        await Assert.That(plugins.Count).IsEqualTo(1);
        await Assert.That(plugins[0].Name).IsEqualTo("testformatter");
    }

    #endregion

    #region Plugin Detection Tests

    [Test]
    public async Task DiscoverPlugins_TypeWithDraftSpecPluginAttribute_Discovered()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        // Use a real type that has the attribute
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));

        var console = new MockConsole();
        var loader = new PluginLoader(scanner, assemblyLoader, console);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        await Assert.That(plugins.Count).IsEqualTo(1);
        await Assert.That(plugins[0].Name).IsEqualTo("testformatter");
        await Assert.That(plugins[0].Type).IsEqualTo(typeof(TestFormatterWithAttribute));
    }

    [Test]
    public async Task DiscoverPlugins_TypeWithoutAttribute_NotDiscovered()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        // Use a real type WITHOUT the attribute
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(FormatterWithoutAttribute));

        var console = new MockConsole();
        var loader = new PluginLoader(scanner, assemblyLoader, console);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        await Assert.That(plugins.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DiscoverPlugins_FormatterType_HasFormatterKind()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));

        var console = new MockConsole();
        var loader = new PluginLoader(scanner, assemblyLoader, console);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        await Assert.That(plugins.Count).IsEqualTo(1);
        await Assert.That(plugins[0].Kind).IsEqualTo(PluginKind.Formatter);
    }

    [Test]
    public async Task DiscoverPlugins_PluginInfo_ContainsAssembly()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));

        var console = new MockConsole();
        var loader = new PluginLoader(scanner, assemblyLoader, console);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        await Assert.That(plugins.Count).IsEqualTo(1);
        await Assert.That(plugins[0].Assembly).IsEqualTo(typeof(TestFormatterWithAttribute).Assembly);
    }

    [Test]
    public async Task DiscoverPlugins_NonFormatterType_Skipped()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        // Type with attribute but NOT implementing IFormatter
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(NonFormatterWithAttribute));

        var console = new MockConsole();
        var loader = new PluginLoader(scanner, assemblyLoader, console);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        // Should be skipped because it doesn't implement known plugin interfaces
        await Assert.That(plugins.Count).IsEqualTo(0);
    }

    #endregion

    #region RegisterFormatters Tests

    [Test]
    public async Task RegisterFormatters_RegistersDiscoveredFormatters()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.JsonFormatter.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.JsonFormatter.dll", typeof(TestFormatterWithAttribute));

        var console = new MockConsole();
        var loader = new PluginLoader(scanner, assemblyLoader, console);

        // Discover plugins first with directories
        loader.DiscoverPlugins(["/plugins"]);

        var registry = new MockFormatterRegistry();
        loader.RegisterFormatters(registry);

        await Assert.That(registry.RegisteredFormatters).ContainsKey("testformatter");
    }

    #endregion

    #region Signature Verification Tests

    [Test]
    public async Task DiscoverPlugins_WithoutConfig_LoadsAllPlugins()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));
        // No public key token set - unsigned

        var console = new MockConsole();
        var loader = new PluginLoader(scanner, assemblyLoader, console); // No config

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        // Should load plugin without verification
        await Assert.That(plugins.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DiscoverPlugins_RequireSignedPlugins_UnsignedPlugin_Rejected()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));
        assemblyLoader.SetPublicKeyToken("/plugins/DraftSpec.MyPlugin.dll", null); // Unsigned

        var console = new MockConsole();
        var config = new PluginsConfig
        {
            RequireSignedPlugins = true,
            TrustedPublicKeyTokens = ["abc123def456"]
        };
        var loader = new PluginLoader(scanner, assemblyLoader, console, config);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        await Assert.That(plugins.Count).IsEqualTo(0);
        await Assert.That(console.Errors).Contains("not signed");
    }

    [Test]
    public async Task DiscoverPlugins_RequireSignedPlugins_UntrustedToken_Rejected()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));
        assemblyLoader.SetPublicKeyToken("/plugins/DraftSpec.MyPlugin.dll", "untrustedtoken");

        var console = new MockConsole();
        var config = new PluginsConfig
        {
            RequireSignedPlugins = true,
            TrustedPublicKeyTokens = ["abc123def456"]
        };
        var loader = new PluginLoader(scanner, assemblyLoader, console, config);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        await Assert.That(plugins.Count).IsEqualTo(0);
        await Assert.That(console.Errors).Contains("untrusted signature");
        await Assert.That(console.Errors).Contains("untrustedtoken");
    }

    [Test]
    public async Task DiscoverPlugins_RequireSignedPlugins_TrustedToken_Loaded()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));
        assemblyLoader.SetPublicKeyToken("/plugins/DraftSpec.MyPlugin.dll", "abc123def456");

        var console = new MockConsole();
        var config = new PluginsConfig
        {
            RequireSignedPlugins = true,
            TrustedPublicKeyTokens = ["abc123def456"]
        };
        var loader = new PluginLoader(scanner, assemblyLoader, console, config);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        await Assert.That(plugins.Count).IsEqualTo(1);
        await Assert.That(plugins[0].Name).IsEqualTo("testformatter");
    }

    [Test]
    public async Task DiscoverPlugins_RequireSignedPlugins_CaseInsensitiveTokenMatch()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));
        assemblyLoader.SetPublicKeyToken("/plugins/DraftSpec.MyPlugin.dll", "ABC123DEF456"); // Uppercase

        var console = new MockConsole();
        var config = new PluginsConfig
        {
            RequireSignedPlugins = true,
            TrustedPublicKeyTokens = ["abc123def456"] // Lowercase
        };
        var loader = new PluginLoader(scanner, assemblyLoader, console, config);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        // Should match case-insensitively
        await Assert.That(plugins.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DiscoverPlugins_RequireSignedPlugins_False_LoadsUnsignedPlugins()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));
        assemblyLoader.SetPublicKeyToken("/plugins/DraftSpec.MyPlugin.dll", null); // Unsigned

        var console = new MockConsole();
        var config = new PluginsConfig
        {
            RequireSignedPlugins = false // Explicitly disabled
        };
        var loader = new PluginLoader(scanner, assemblyLoader, console, config);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        // Should load without verification
        await Assert.That(plugins.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DiscoverPlugins_RequireSignedPlugins_TrustedCertThumbprint_Loaded()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));
        assemblyLoader.SetCertificateThumbprint(
            "/plugins/DraftSpec.MyPlugin.dll",
            "A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2");

        var console = new MockConsole();
        var config = new PluginsConfig
        {
            RequireSignedPlugins = true,
            TrustedCertificateThumbprints = ["A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2"]
        };
        var loader = new PluginLoader(scanner, assemblyLoader, console, config);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        await Assert.That(plugins.Count).IsEqualTo(1);
        await Assert.That(plugins[0].Name).IsEqualTo("testformatter");
    }

    [Test]
    public async Task DiscoverPlugins_RequireSignedPlugins_UntrustedCertThumbprint_Rejected()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));
        assemblyLoader.SetCertificateThumbprint(
            "/plugins/DraftSpec.MyPlugin.dll",
            "UNTRUSTED0000000000000000000000000000000000000000000000000000000");

        var console = new MockConsole();
        var config = new PluginsConfig
        {
            RequireSignedPlugins = true,
            TrustedCertificateThumbprints = ["A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2"]
        };
        var loader = new PluginLoader(scanner, assemblyLoader, console, config);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        await Assert.That(plugins.Count).IsEqualTo(0);
        await Assert.That(console.Errors).Contains("untrusted signature");
        await Assert.That(console.Errors).Contains("certificate");
    }

    [Test]
    public async Task DiscoverPlugins_RequireSignedPlugins_CertThumbprintPrecedesPublicKeyToken()
    {
        // If both certificate and public key token are present, cert should be checked first
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));
        // Set both - cert is trusted, but public key token is NOT in the trusted list
        assemblyLoader.SetCertificateThumbprint(
            "/plugins/DraftSpec.MyPlugin.dll",
            "A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2");
        assemblyLoader.SetPublicKeyToken("/plugins/DraftSpec.MyPlugin.dll", "untrustedtoken");

        var console = new MockConsole();
        var config = new PluginsConfig
        {
            RequireSignedPlugins = true,
            TrustedCertificateThumbprints = ["A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2"],
            TrustedPublicKeyTokens = ["differenttoken"] // Not the one the plugin has
        };
        var loader = new PluginLoader(scanner, assemblyLoader, console, config);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        // Should load because cert thumbprint is trusted (even though public key token is not)
        await Assert.That(plugins.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DiscoverPlugins_RequireSignedPlugins_FallsBackToPublicKeyToken()
    {
        // If no certificate, should fall back to public key token check
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));
        // No certificate, but has a trusted public key token
        assemblyLoader.SetCertificateThumbprint("/plugins/DraftSpec.MyPlugin.dll", null);
        assemblyLoader.SetPublicKeyToken("/plugins/DraftSpec.MyPlugin.dll", "abc123def456");

        var console = new MockConsole();
        var config = new PluginsConfig
        {
            RequireSignedPlugins = true,
            TrustedPublicKeyTokens = ["abc123def456"]
        };
        var loader = new PluginLoader(scanner, assemblyLoader, console, config);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        // Should load because public key token is trusted
        await Assert.That(plugins.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DiscoverPlugins_RequireSignedPlugins_CertThumbprintCaseInsensitive()
    {
        var scanner = new MockPluginScanner();
        scanner.AddDirectory("/plugins");
        scanner.AddPluginFile("/plugins", "DraftSpec.MyPlugin.dll");

        var assemblyLoader = new MockAssemblyLoader();
        assemblyLoader.AddRealType("/plugins/DraftSpec.MyPlugin.dll", typeof(TestFormatterWithAttribute));
        // Uppercase thumbprint
        assemblyLoader.SetCertificateThumbprint(
            "/plugins/DraftSpec.MyPlugin.dll",
            "A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2");

        var console = new MockConsole();
        var config = new PluginsConfig
        {
            RequireSignedPlugins = true,
            // Lowercase in config
            TrustedCertificateThumbprints = ["a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"]
        };
        var loader = new PluginLoader(scanner, assemblyLoader, console, config);

        var plugins = loader.DiscoverPlugins(["/plugins"]).ToList();

        // Should match case-insensitively
        await Assert.That(plugins.Count).IsEqualTo(1);
    }

    #endregion

    #region Test Types for Plugin Detection

    /// <summary>
    /// Test formatter with DraftSpecPlugin attribute - should be discovered.
    /// </summary>
    [DraftSpecPlugin("testformatter")]
    public class TestFormatterWithAttribute : IFormatter
    {
        public string Format(SpecReport report) => "test output";
        public string FileExtension => ".test";
    }

    /// <summary>
    /// Test formatter WITHOUT DraftSpecPlugin attribute - should NOT be discovered.
    /// </summary>
    public class FormatterWithoutAttribute : IFormatter
    {
        public string Format(SpecReport report) => "no attribute";
        public string FileExtension => ".noattr";
    }

    /// <summary>
    /// Test type WITH attribute but NOT implementing IFormatter - should be skipped.
    /// </summary>
    [DraftSpecPlugin("nonformatter")]
    public class NonFormatterWithAttribute
    {
        public string DoSomething() => "not a formatter";
    }

    #endregion

    #region Test Helpers - Mocks

    private class MockPluginScanner : IPluginScanner
    {
        private readonly HashSet<string> _directories = [];
        private readonly Dictionary<string, List<string>> _pluginFiles = [];

        public List<string> CheckedDirectories { get; } = [];
        public List<string> ScannedDirectories { get; } = [];

        public void AddDirectory(string directory)
        {
            _directories.Add(directory);
        }

        public void AddPluginFile(string directory, string fileName)
        {
            if (!_pluginFiles.ContainsKey(directory))
                _pluginFiles[directory] = [];

            _pluginFiles[directory].Add(Path.Combine(directory, fileName));
        }

        public bool DirectoryExists(string directory)
        {
            CheckedDirectories.Add(directory);
            return _directories.Contains(directory);
        }

        public IEnumerable<string> FindPluginFiles(string directory)
        {
            ScannedDirectories.Add(directory);
            return _pluginFiles.TryGetValue(directory, out var files) ? files : [];
        }
    }

    private class MockAssemblyLoader : IAssemblyLoader
    {
        private readonly Dictionary<string, List<Type>> _realTypes = [];
        private readonly Dictionary<string, Exception> _loadExceptions = [];
        private readonly Dictionary<string, string?> _publicKeyTokens = [];
        private readonly Dictionary<string, string?> _certificateThumbprints = [];

        /// <summary>
        /// Add a real type to be "discovered" from an assembly path.
        /// </summary>
        public void AddRealType(string path, Type type)
        {
            if (!_realTypes.ContainsKey(path))
                _realTypes[path] = [];
            _realTypes[path].Add(type);
        }

        public void SetLoadException(string path, Exception exception)
        {
            _loadExceptions[path] = exception;
        }

        /// <summary>
        /// Set the public key token for a given assembly path.
        /// Pass null to simulate an unsigned assembly.
        /// </summary>
        public void SetPublicKeyToken(string path, string? token)
        {
            _publicKeyTokens[path] = token;
        }

        /// <summary>
        /// Set the certificate thumbprint for a given assembly path.
        /// Pass null to simulate an assembly without Authenticode signature.
        /// </summary>
        public void SetCertificateThumbprint(string path, string? thumbprint)
        {
            _certificateThumbprints[path] = thumbprint;
        }

        public Assembly? LoadAssembly(string path)
        {
            if (_loadExceptions.TryGetValue(path, out var exception))
                throw exception;

            // Return the assembly of the first real type, or null
            if (_realTypes.TryGetValue(path, out var types) && types.Count > 0)
                return types[0].Assembly;

            return null;
        }

        public IEnumerable<Type> GetExportedTypes(Assembly assembly)
        {
            // Return all real types registered for any path that match this assembly
            foreach (var kvp in _realTypes)
            {
                foreach (var type in kvp.Value)
                {
                    if (type.Assembly == assembly)
                        yield return type;
                }
            }
        }

        public object? CreateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }

        public string? GetPublicKeyToken(string path)
        {
            return _publicKeyTokens.TryGetValue(path, out var token) ? token : null;
        }

        public string? GetCertificateThumbprint(string path)
        {
            return _certificateThumbprints.TryGetValue(path, out var thumbprint) ? thumbprint : null;
        }
    }

    private class MockFormatter : IFormatter
    {
        public string Format(SpecReport report) => "mock output";
        public string FileExtension => ".mock";
    }

    private class MockFormatterRegistry : ICliFormatterRegistry
    {
        public Dictionary<string, Func<string?, IFormatter>> RegisteredFormatters { get; } = [];

        public void Register(string name, Func<string?, IFormatter> factory)
        {
            RegisteredFormatters[name] = factory;
        }

        public IFormatter? GetFormatter(string name, string? cssUrl = null)
        {
            return RegisteredFormatters.TryGetValue(name, out var factory) ? factory(cssUrl) : null;
        }

        public IEnumerable<string> Names => RegisteredFormatters.Keys;
    }

    #endregion
}
