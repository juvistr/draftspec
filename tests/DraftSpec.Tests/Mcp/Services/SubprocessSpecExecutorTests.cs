using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Services;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Mcp.Services;

/// <summary>
/// Tests for SubprocessSpecExecutor class.
/// </summary>
public class SubprocessSpecExecutorTests
{
    [Test]
    public async Task ExecuteAsync_WithoutProgressCallback_UsesSimpleOverload()
    {
        var mockService = MockSpecExecutionService.Successful();
        var executor = new SubprocessSpecExecutor(mockService);

        var result = await executor.ExecuteAsync("test content", TimeSpan.FromSeconds(10));

        await Assert.That(mockService.ExecutionCount).IsEqualTo(1);
        await Assert.That(mockService.LastContent).IsEqualTo("test content");
        await Assert.That(mockService.LastTimeout).IsEqualTo(TimeSpan.FromSeconds(10));
        await Assert.That(mockService.WasProgressCallbackProvided).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_WithProgressCallback_UsesProgressOverload()
    {
        var mockService = MockSpecExecutionService.Successful();
        var progressReceived = new List<SpecProgressNotification>();
        Func<SpecProgressNotification, Task> onProgress = n =>
        {
            progressReceived.Add(n);
            return Task.CompletedTask;
        };

        var executor = new SubprocessSpecExecutor(mockService, onProgress);

        await executor.ExecuteAsync("test content", TimeSpan.FromSeconds(10));

        await Assert.That(mockService.WasProgressCallbackProvided).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_UsesPassedCancellationToken_WhenProvided()
    {
        var mockService = MockSpecExecutionService.Successful();
        using var cts = new CancellationTokenSource();
        var executor = new SubprocessSpecExecutor(mockService);

        await executor.ExecuteAsync("test content", TimeSpan.FromSeconds(10), cts.Token);

        await Assert.That(mockService.LastCancellationToken).IsEqualTo(cts.Token);
    }

    [Test]
    public async Task ExecuteAsync_UsesConstructorCancellationToken_WhenPassedIsDefault()
    {
        var mockService = MockSpecExecutionService.Successful();
        using var cts = new CancellationTokenSource();
        var executor = new SubprocessSpecExecutor(mockService, cancellationToken: cts.Token);

        await executor.ExecuteAsync("test content", TimeSpan.FromSeconds(10));

        await Assert.That(mockService.LastCancellationToken).IsEqualTo(cts.Token);
    }

    [Test]
    public async Task ExecuteAsync_PrefersPassedCancellationToken_OverConstructor()
    {
        var mockService = MockSpecExecutionService.Successful();
        using var constructorCts = new CancellationTokenSource();
        using var methodCts = new CancellationTokenSource();
        var executor = new SubprocessSpecExecutor(mockService, cancellationToken: constructorCts.Token);

        await executor.ExecuteAsync("test content", TimeSpan.FromSeconds(10), methodCts.Token);

        await Assert.That(mockService.LastCancellationToken).IsEqualTo(methodCts.Token);
    }

    [Test]
    public async Task ExecuteAsync_ReturnsSuccessResult()
    {
        var mockService = MockSpecExecutionService.Successful();
        var executor = new SubprocessSpecExecutor(mockService);

        var result = await executor.ExecuteAsync("test content", TimeSpan.FromSeconds(10));

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_ReturnsFailedResult()
    {
        var mockService = MockSpecExecutionService.Failed("execution error");
        var executor = new SubprocessSpecExecutor(mockService);

        var result = await executor.ExecuteAsync("test content", TimeSpan.FromSeconds(10));

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.ExitCode).IsEqualTo(1);
        await Assert.That(result.Error!.Message).IsEqualTo("execution error");
    }

    [Test]
    public async Task ExecuteAsync_PropagatesProgressNotifications()
    {
        var notifications = new List<SpecProgressNotification>
        {
            new() { Type = "start", Total = 2 },
            new() { Type = "progress", Completed = 1, Total = 2, Status = "passed", Spec = "test1" },
            new() { Type = "complete", Passed = 2, Failed = 0 }
        };
        var mockService = MockSpecExecutionService.Successful(notifications);
        var receivedNotifications = new List<SpecProgressNotification>();
        Func<SpecProgressNotification, Task> onProgress = n =>
        {
            receivedNotifications.Add(n);
            return Task.CompletedTask;
        };

        var executor = new SubprocessSpecExecutor(mockService, onProgress);
        await executor.ExecuteAsync("test content", TimeSpan.FromSeconds(10));

        await Assert.That(receivedNotifications.Count).IsEqualTo(3);
        await Assert.That(receivedNotifications[0].Type).IsEqualTo("start");
        await Assert.That(receivedNotifications[1].Type).IsEqualTo("progress");
        await Assert.That(receivedNotifications[2].Type).IsEqualTo("complete");
    }

    [Test]
    public async Task ExecuteAsync_PassesCorrectTimeout()
    {
        var mockService = MockSpecExecutionService.Successful();
        var executor = new SubprocessSpecExecutor(mockService);

        await executor.ExecuteAsync("content", TimeSpan.FromSeconds(42));

        await Assert.That(mockService.LastTimeout).IsEqualTo(TimeSpan.FromSeconds(42));
    }

    [Test]
    public async Task ExecuteAsync_PassesContentToService()
    {
        var mockService = MockSpecExecutionService.Successful();
        var executor = new SubprocessSpecExecutor(mockService);
        var specContent = "describe(\"test\", () => { it(\"works\", () => { }); });";

        await executor.ExecuteAsync(specContent, TimeSpan.FromSeconds(10));

        await Assert.That(mockService.LastContent).IsEqualTo(specContent);
    }
}
