using DraftSpec.Configuration;
using DraftSpec.Formatters;
using DraftSpec.Plugins;
using DraftSpec.Providers;

namespace DraftSpec.Tests.Configuration;

public class ConfigurationTests
{
    #region Formatter Registry

    [Test]
    public async Task FormatterRegistry_Register_StoresFormatter()
    {
        var registry = new FormatterRegistry();
        var formatter = new JsonFormatter();

        registry.Register("json", formatter);

        await Assert.That(registry.Get("json")).IsSameReferenceAs(formatter);
    }

    [Test]
    public async Task FormatterRegistry_Get_IsCaseInsensitive()
    {
        var registry = new FormatterRegistry();
        var formatter = new JsonFormatter();

        registry.Register("JSON", formatter);

        await Assert.That(registry.Get("json")).IsSameReferenceAs(formatter);
        await Assert.That(registry.Get("Json")).IsSameReferenceAs(formatter);
    }

    [Test]
    public async Task FormatterRegistry_Get_ReturnsNullForUnknown()
    {
        var registry = new FormatterRegistry();

        await Assert.That(registry.Get("unknown")).IsNull();
    }

    [Test]
    public async Task FormatterRegistry_Names_ReturnsRegisteredNames()
    {
        var registry = new FormatterRegistry();
        registry.Register("json", new JsonFormatter());
        registry.Register("markdown", new JsonFormatter());

        await Assert.That(registry.Names).Contains("json");
        await Assert.That(registry.Names).Contains("markdown");
    }

    [Test]
    public async Task FormatterRegistry_Contains_ChecksRegistration()
    {
        var registry = new FormatterRegistry();
        registry.Register("json", new JsonFormatter());

        await Assert.That(registry.Contains("json")).IsTrue();
        await Assert.That(registry.Contains("unknown")).IsFalse();
    }

    #endregion

    #region DraftSpecConfiguration

    [Test]
    public async Task Configuration_AddFormatter_RegistersFormatter()
    {
        var config = new DraftSpecConfiguration();
        var formatter = new JsonFormatter();

        config.AddFormatter("custom", formatter);

        await Assert.That(config.Formatters.Get("custom")).IsSameReferenceAs(formatter);
    }

    [Test]
    public async Task Configuration_AddReporter_RegistersReporter()
    {
        var config = new DraftSpecConfiguration();
        var reporter = new TestReporter("test");

        config.AddReporter(reporter);

        await Assert.That(config.Reporters.Get("test")).IsSameReferenceAs(reporter);
    }

    [Test]
    public async Task Configuration_UsePlugin_RegistersPlugin()
    {
        var config = new DraftSpecConfiguration();

        config.UsePlugin<TestPlugin>();

        await Assert.That(config.Plugins.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Configuration_AddService_MakesServiceAvailable()
    {
        var config = new DraftSpecConfiguration();
        var service = new TestService();

        config.AddService(service);

        await Assert.That(config.GetService<TestService>()).IsSameReferenceAs(service);
    }

    [Test]
    public async Task Configuration_GetService_ReturnsNullForUnregistered()
    {
        var config = new DraftSpecConfiguration();

        await Assert.That(config.GetService<TestService>()).IsNull();
    }

    [Test]
    public async Task Configuration_Initialize_CallsPluginInitialize()
    {
        var config = new DraftSpecConfiguration();
        var plugin = new TestPlugin();
        config.UsePlugin(plugin);

        config.Initialize();

        await Assert.That(plugin.InitializeCalled).IsTrue();
    }

    [Test]
    public async Task Configuration_Dispose_DisposesPlugins()
    {
        var config = new DraftSpecConfiguration();
        var plugin = new TestPlugin();
        config.UsePlugin(plugin);
        config.Initialize();

        config.Dispose();

        await Assert.That(plugin.DisposeCalled).IsTrue();
    }

    [Test]
    public async Task Configuration_Configure_CallsActionWithPlugin()
    {
        var config = new DraftSpecConfiguration();
        var plugin = new ConfigurablePlugin();
        config.UsePlugin(plugin);

        config.Configure<ConfigurablePlugin>(p => p.ConfiguredValue = "test-value");

        await Assert.That(plugin.ConfiguredValue).IsEqualTo("test-value");
    }

    [Test]
    public async Task Configuration_Configure_DoesNothingWhenPluginNotFound()
    {
        var config = new DraftSpecConfiguration();
        var wasCalled = false;

        config.Configure<ConfigurablePlugin>(p => wasCalled = true);

        await Assert.That(wasCalled).IsFalse();
    }

    [Test]
    public async Task Configuration_Initialize_RegistersFormattersFromPlugins()
    {
        var config = new DraftSpecConfiguration();
        var plugin = new TestFormatterPlugin();
        config.UsePlugin(plugin);

        config.Initialize();

        await Assert.That(plugin.RegisterFormattersCalled).IsTrue();
        await Assert.That(config.Formatters.Contains("test-format")).IsTrue();
    }

    [Test]
    public async Task Configuration_Initialize_RegistersReportersFromPlugins()
    {
        var config = new DraftSpecConfiguration();
        var plugin = new TestReporterPlugin();
        config.UsePlugin(plugin);

        config.Initialize();

        await Assert.That(plugin.RegisterReportersCalled).IsTrue();
        await Assert.That(config.Reporters.Get("test-reporter")).IsNotNull();
    }

    [Test]
    public async Task Configuration_Initialize_CalledTwice_OnlyInitializesOnce()
    {
        var config = new DraftSpecConfiguration();
        var plugin = new TestPlugin();
        config.UsePlugin(plugin);

        config.Initialize();
        plugin.InitializeCalled = false; // Reset flag
        config.Initialize(); // Second call should be no-op

        await Assert.That(plugin.InitializeCalled).IsFalse();
    }

    [Test]
    public async Task Configuration_InitializeMiddleware_RegistersMiddlewareFromPlugins()
    {
        var config = new DraftSpecConfiguration();
        var plugin = new TestMiddlewarePlugin();
        config.UsePlugin(plugin);
        config.Initialize();
        var builder = new SpecRunnerBuilder();

        config.InitializeMiddleware(builder);

        await Assert.That(plugin.RegisterMiddlewareCalled).IsTrue();
    }

    [Test]
    public async Task Configuration_EnvironmentProvider_DefaultsToSystemProvider()
    {
        var config = new DraftSpecConfiguration();

        await Assert.That(config.EnvironmentProvider).IsSameReferenceAs(SystemEnvironmentProvider.Instance);
    }

    [Test]
    public async Task Configuration_EnvironmentProvider_CanBeSet()
    {
        var config = new DraftSpecConfiguration();
        var customProvider = new InMemoryEnvironmentProvider();

        config.EnvironmentProvider = customProvider;

        await Assert.That(config.EnvironmentProvider).IsSameReferenceAs(customProvider);
    }

    #endregion

    #region Test Helpers

    private class TestReporter : IReporter
    {
        public TestReporter(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public List<string> Events { get; } = [];

        public Task OnRunStartingAsync(RunStartingContext context)
        {
            Events.Add("starting");
            return Task.CompletedTask;
        }

        public Task OnSpecCompletedAsync(SpecResult result)
        {
            Events.Add($"spec:{result.Spec.Description}");
            return Task.CompletedTask;
        }

        public Task OnRunCompletedAsync(SpecReport report)
        {
            Events.Add("completed");
            return Task.CompletedTask;
        }
    }

    private class TestPlugin : IPlugin
    {
        public string Name => "test";
        public string Version => "1.0.0";
        public bool InitializeCalled { get; set; }
        public bool DisposeCalled { get; private set; }

        public void Initialize(IPluginContext context)
        {
            InitializeCalled = true;
        }

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }

    private class TestService
    {
    }

    private class ConfigurablePlugin : IPlugin
    {
        public string Name => "configurable";
        public string Version => "1.0.0";
        public string? ConfiguredValue { get; set; }

        public void Initialize(IPluginContext context) { }
        public void Dispose() { }
    }

    private class TestFormatterPlugin : IFormatterPlugin
    {
        public string Name => "test-formatter";
        public string Version => "1.0.0";
        public bool RegisterFormattersCalled { get; private set; }

        public void Initialize(IPluginContext context) { }
        public void Dispose() { }

        public void RegisterFormatters(IFormatterRegistry registry)
        {
            RegisterFormattersCalled = true;
            registry.Register("test-format", new JsonFormatter());
        }
    }

    private class TestReporterPlugin : IReporterPlugin
    {
        public string Name => "test-reporter-plugin";
        public string Version => "1.0.0";
        public bool RegisterReportersCalled { get; private set; }

        public void Initialize(IPluginContext context) { }
        public void Dispose() { }

        public void RegisterReporters(IReporterRegistry registry)
        {
            RegisterReportersCalled = true;
            registry.Register(new TestReporter("test-reporter"));
        }
    }

    private class TestMiddlewarePlugin : IMiddlewarePlugin
    {
        public string Name => "test-middleware";
        public string Version => "1.0.0";
        public bool RegisterMiddlewareCalled { get; private set; }

        public void Initialize(IPluginContext context) { }
        public void Dispose() { }

        public void RegisterMiddleware(SpecRunnerBuilder builder)
        {
            RegisterMiddlewareCalled = true;
        }
    }

    #endregion
}