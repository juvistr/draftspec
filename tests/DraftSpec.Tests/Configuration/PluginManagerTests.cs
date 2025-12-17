using DraftSpec.Configuration;
using DraftSpec.Plugins;

namespace DraftSpec.Tests.Configuration;

/// <summary>
/// Tests for PluginManager.
/// </summary>
public class PluginManagerTests
{
    [Test]
    public async Task Register_Plugin_CanBeRetrieved()
    {
        using var manager = new PluginManager();
        var plugin = new TestPlugin();

        manager.Register(plugin);

        await Assert.That(manager.Get<TestPlugin>()).IsSameReferenceAs(plugin);
    }

    [Test]
    public async Task Register_ByType_CreatesAndRegistersPlugin()
    {
        using var manager = new PluginManager();

        manager.Register<TestPlugin>();

        await Assert.That(manager.Get<TestPlugin>()).IsNotNull();
    }

    [Test]
    public async Task Get_NotRegistered_ReturnsNull()
    {
        using var manager = new PluginManager();

        await Assert.That(manager.Get<TestPlugin>()).IsNull();
    }

    [Test]
    public async Task All_ReturnsAllPlugins()
    {
        using var manager = new PluginManager();
        var plugin1 = new TestPlugin();
        var plugin2 = new AnotherPlugin();

        manager.Register(plugin1);
        manager.Register(plugin2);

        var all = manager.All.ToList();
        await Assert.That(all.Count).IsEqualTo(2);
        await Assert.That(all).Contains(plugin1);
        await Assert.That(all).Contains(plugin2);
    }

    [Test]
    public async Task OfType_FiltersPlugins()
    {
        using var manager = new PluginManager();
        manager.Register(new TestPlugin());
        manager.Register(new AnotherPlugin());
        manager.Register(new FormatterPlugin());

        var formatterPlugins = manager.OfType<IFormatterPlugin>().ToList();

        await Assert.That(formatterPlugins.Count).IsEqualTo(1);
        await Assert.That(formatterPlugins[0]).IsTypeOf<FormatterPlugin>();
    }

    [Test]
    public async Task Count_ReturnsNumberOfPlugins()
    {
        using var manager = new PluginManager();

        await Assert.That(manager.Count).IsEqualTo(0);

        manager.Register(new TestPlugin());
        await Assert.That(manager.Count).IsEqualTo(1);

        manager.Register(new AnotherPlugin());
        await Assert.That(manager.Count).IsEqualTo(2);
    }

    [Test]
    public void Register_NullPlugin_ThrowsArgumentNullException()
    {
        using var manager = new PluginManager();

        Assert.Throws<ArgumentNullException>(() => manager.Register(null!));
    }

    [Test]
    public async Task Dispose_DisposesAllPlugins()
    {
        var plugin = new DisposablePlugin();
        var manager = new PluginManager();
        manager.Register(plugin);

        manager.Dispose();

        await Assert.That(plugin.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Dispose_ClearsPlugins()
    {
        var manager = new PluginManager();
        manager.Register(new TestPlugin());

        manager.Dispose();

        await Assert.That(manager.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Dispose_CanBeCalledMultipleTimes()
    {
        var plugin = new DisposablePlugin();
        var manager = new PluginManager();
        manager.Register(plugin);

        manager.Dispose();
        manager.Dispose(); // Should not throw

        await Assert.That(plugin.DisposeCount).IsEqualTo(1);
    }

    #region Test Plugins

    private class TestPlugin : IPlugin
    {
        public string Name => "Test";
        public string Version => "1.0.0";
        public void Initialize(IPluginContext context) { }
        public void Dispose() { }
    }

    private class AnotherPlugin : IPlugin
    {
        public string Name => "Another";
        public string Version => "1.0.0";
        public void Initialize(IPluginContext context) { }
        public void Dispose() { }
    }

    private class FormatterPlugin : IPlugin, IFormatterPlugin
    {
        public string Name => "Formatter";
        public string Version => "1.0.0";
        public void Initialize(IPluginContext context) { }
        public void RegisterFormatters(IFormatterRegistry registry) { }
        public void Dispose() { }
    }

    private class DisposablePlugin : IPlugin
    {
        public string Name => "Disposable";
        public string Version => "1.0.0";
        public bool IsDisposed { get; private set; }
        public int DisposeCount { get; private set; }

        public void Initialize(IPluginContext context) { }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                DisposeCount++;
            }
        }
    }

    #endregion
}
