using DraftSpec.Configuration;
using DraftSpec.Plugins;

namespace DraftSpec.Tests.Configuration;

/// <summary>
/// Tests for PluginContext which wraps DraftSpecConfiguration for plugin initialization.
/// </summary>
[NotInParallel(nameof(Console))]
public class PluginContextTests
{
    [Test]
    public async Task GetService_Registered_ReturnsService()
    {
        // Arrange
        var config = new DraftSpecConfiguration();
        var testService = new TestService();
        config.AddService(testService);

        var context = CreateContext(config);

        // Act
        var result = context.GetService<TestService>();

        // Assert
        await Assert.That(result).IsSameReferenceAs(testService);
    }

    [Test]
    public async Task GetService_NotRegistered_ReturnsNull()
    {
        // Arrange
        var config = new DraftSpecConfiguration();
        var context = CreateContext(config);

        // Act
        var result = context.GetService<TestService>();

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetRequiredService_Registered_ReturnsService()
    {
        // Arrange
        var config = new DraftSpecConfiguration();
        var testService = new TestService();
        config.AddService(testService);

        var context = CreateContext(config);

        // Act
        var result = context.GetRequiredService<TestService>();

        // Assert
        await Assert.That(result).IsSameReferenceAs(testService);
    }

    [Test]
    public async Task GetRequiredService_NotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new DraftSpecConfiguration();
        var context = CreateContext(config);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => context.GetRequiredService<TestService>());
        await Assert.That(exception.Message).Contains("TestService");
        await Assert.That(exception.Message).Contains("not registered");
    }

    [Test]
    public async Task Log_Debug_FormatsCorrectly()
    {
        // Arrange
        var config = new DraftSpecConfiguration();
        var context = CreateContext(config);
        var output = CaptureConsoleOutput(() => context.Log(LogLevel.Debug, "Test debug message"));

        // Assert
        await Assert.That(output).Contains("[DEBUG]");
        await Assert.That(output).Contains("Test debug message");
    }

    [Test]
    public async Task Log_Info_FormatsCorrectly()
    {
        // Arrange
        var config = new DraftSpecConfiguration();
        var context = CreateContext(config);
        var output = CaptureConsoleOutput(() => context.Log(LogLevel.Info, "Test info message"));

        // Assert
        await Assert.That(output).Contains("[INFO]");
        await Assert.That(output).Contains("Test info message");
    }

    [Test]
    public async Task Log_Warning_FormatsCorrectly()
    {
        // Arrange
        var config = new DraftSpecConfiguration();
        var context = CreateContext(config);
        var output = CaptureConsoleOutput(() => context.Log(LogLevel.Warning, "Test warning message"));

        // Assert
        await Assert.That(output).Contains("[WARN]");
        await Assert.That(output).Contains("Test warning message");
    }

    [Test]
    public async Task Log_Error_FormatsCorrectly()
    {
        // Arrange
        var config = new DraftSpecConfiguration();
        var context = CreateContext(config);
        var output = CaptureConsoleOutput(() => context.Log(LogLevel.Error, "Test error message"));

        // Assert
        await Assert.That(output).Contains("[ERROR]");
        await Assert.That(output).Contains("Test error message");
    }

    /// <summary>
    /// Creates a PluginContext via plugin initialization since PluginContext is internal.
    /// </summary>
    private static IPluginContext CreateContext(DraftSpecConfiguration config)
    {
        IPluginContext? capturedContext = null;
        var plugin = new ContextCapturingPlugin(ctx => capturedContext = ctx);
        config.UsePlugin(plugin);

        // Build a runner to trigger plugin initialization
        // This is how plugins get their context in production
        _ = new SpecRunnerBuilder().WithConfiguration(config).Build();

        return capturedContext!;
    }

    private static string CaptureConsoleOutput(Action action)
    {
        var originalOut = Console.Out;
        try
        {
            using var sw = new StringWriter();
            Console.SetOut(sw);
            action();
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private class TestService;

    /// <summary>
    /// Plugin that captures the IPluginContext during initialization.
    /// </summary>
    private class ContextCapturingPlugin : IPlugin
    {
        private readonly Action<IPluginContext> _captureAction;

        public ContextCapturingPlugin(Action<IPluginContext> captureAction)
        {
            _captureAction = captureAction;
        }

        public string Name => "ContextCapturing";
        public string Version => "1.0.0";

        public void Initialize(IPluginContext context)
        {
            _captureAction(context);
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }
}
