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

    #region Registration Edge Cases

    [Test]
    public async Task Register_SamePluginTwice_BothRegistered()
    {
        using var manager = new PluginManager();
        var plugin = new TestPlugin();

        manager.Register(plugin);
        manager.Register(plugin); // Same instance - still added

        await Assert.That(manager.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Register_TwoPluginsOfSameType_BothRegistered()
    {
        using var manager = new PluginManager();

        manager.Register(new TestPlugin());
        manager.Register(new TestPlugin()); // Different instance

        await Assert.That(manager.Count).IsEqualTo(2);
    }

    [Test]
    public async Task All_ReturnsPluginsInRegistrationOrder()
    {
        using var manager = new PluginManager();
        var plugin1 = new TestPlugin();
        var plugin2 = new AnotherPlugin();
        var plugin3 = new FormatterPlugin();

        manager.Register(plugin1);
        manager.Register(plugin2);
        manager.Register(plugin3);

        var all = manager.All.ToList();
        await Assert.That(all[0]).IsSameReferenceAs(plugin1);
        await Assert.That(all[1]).IsSameReferenceAs(plugin2);
        await Assert.That(all[2]).IsSameReferenceAs(plugin3);
    }

    [Test]
    public async Task Dispose_DisposesInRegistrationOrder()
    {
        var disposeOrder = new List<string>();
        var plugin1 = new DisposeOrderPlugin("first", disposeOrder);
        var plugin2 = new DisposeOrderPlugin("second", disposeOrder);
        var plugin3 = new DisposeOrderPlugin("third", disposeOrder);

        var manager = new PluginManager();
        manager.Register(plugin1);
        manager.Register(plugin2);
        manager.Register(plugin3);

        manager.Dispose();

        await Assert.That(disposeOrder).Count().IsEqualTo(3);
        await Assert.That(disposeOrder[0]).IsEqualTo("first");
        await Assert.That(disposeOrder[1]).IsEqualTo("second");
        await Assert.That(disposeOrder[2]).IsEqualTo("third");
    }

    [Test]
    public async Task OfType_ReturnsAllMatchingPlugins()
    {
        using var manager = new PluginManager();
        manager.Register(new TestPlugin());
        manager.Register(new AnotherPlugin());
        manager.Register(new FormatterPlugin());

        // IPlugin - all should match
        var allPlugins = manager.OfType<IPlugin>().ToList();
        await Assert.That(allPlugins.Count).IsEqualTo(3);
    }

    [Test]
    public async Task OfType_NoMatches_ReturnsEmpty()
    {
        using var manager = new PluginManager();
        manager.Register(new TestPlugin());

        // IFormatterPlugin - only FormatterPlugin implements it
        var formatters = manager.OfType<IFormatterPlugin>().ToList();
        await Assert.That(formatters.Count).IsEqualTo(0);
    }

    #endregion

    #region Error Handling

    [Test]
    public void Dispose_PluginThrows_PropagatesException()
    {
        var manager = new PluginManager();
        manager.Register(new ThrowingDisposePlugin());

        Assert.Throws<InvalidOperationException>(() => manager.Dispose());
    }

    [Test]
    public async Task Dispose_FirstPluginThrows_SubsequentPluginsNotDisposed()
    {
        var disposeOrder = new List<string>();

        var manager = new PluginManager();
        manager.Register(new ThrowingDisposePlugin()); // Will throw first
        manager.Register(new DisposeOrderPlugin("second", disposeOrder));
        manager.Register(new DisposeOrderPlugin("third", disposeOrder));

        try
        {
            manager.Dispose();
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Subsequent plugins were NOT disposed because first one threw
        await Assert.That(disposeOrder.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Dispose_MiddlePluginThrows_FirstDisposeCompletes()
    {
        var disposeOrder = new List<string>();

        var manager = new PluginManager();
        manager.Register(new DisposeOrderPlugin("first", disposeOrder));
        manager.Register(new ThrowingDisposePlugin()); // Will throw second
        manager.Register(new DisposeOrderPlugin("third", disposeOrder));

        try
        {
            manager.Dispose();
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // First plugin was disposed before the exception
        await Assert.That(disposeOrder.Count).IsEqualTo(1);
        await Assert.That(disposeOrder[0]).IsEqualTo("first");
    }

    [Test]
    public async Task Get_AfterDispose_ReturnsNull()
    {
        var manager = new PluginManager();
        manager.Register(new TestPlugin());

        manager.Dispose();

        // After dispose, Get returns null because _byType is cleared
        await Assert.That(manager.Get<TestPlugin>()).IsNull();
    }

    [Test]
    public async Task All_AfterDispose_ReturnsEmpty()
    {
        var manager = new PluginManager();
        manager.Register(new TestPlugin());
        manager.Register(new AnotherPlugin());

        manager.Dispose();

        await Assert.That(manager.All.Count()).IsEqualTo(0);
    }

    #endregion

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

    private class DisposeOrderPlugin : IPlugin
    {
        private readonly string _name;
        private readonly List<string> _order;

        public DisposeOrderPlugin(string name, List<string> order)
        {
            _name = name;
            _order = order;
        }

        public string Name => _name;
        public string Version => "1.0.0";

        public void Initialize(IPluginContext context) { }

        public void Dispose()
        {
            _order.Add(_name);
        }
    }

    private class ThrowingDisposePlugin : IPlugin
    {
        public string Name => "Throwing";
        public string Version => "1.0.0";

        public void Initialize(IPluginContext context) { }

        public void Dispose()
        {
            throw new InvalidOperationException("Plugin dispose failed");
        }
    }

    #endregion
}
