using DraftSpec.Configuration;
using DraftSpec.Formatters;
using DraftSpec.Plugins;

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

        await Assert.That(config.Plugins.Count()).IsGreaterThan(0);
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

    #endregion

    #region Test Helpers

    private class TestReporter : IReporter
    {
        public TestReporter(string name) => Name = name;
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
        public bool InitializeCalled { get; private set; }
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

    #endregion
}
