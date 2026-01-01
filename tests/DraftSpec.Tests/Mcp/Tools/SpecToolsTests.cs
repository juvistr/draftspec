using System.Text.Json;
using DraftSpec.Mcp;
using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Services;
using DraftSpec.Mcp.Tools;
using DraftSpec.Tests.Infrastructure.Mocks;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Tests.Mcp.Tools;

/// <summary>
/// Tests for SpecTools MCP tool methods and helper methods.
/// </summary>
public class SpecToolsTests
{
    private readonly ILogger<SessionManager> _logger = new NullLogger<SessionManager>();
    private readonly string _baseTempDir;
    private readonly McpOptions _options = new();

    private class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    public SpecToolsTests()
    {
        _baseTempDir = Path.Combine(Path.GetTempPath(), $"spectools-tests-{Guid.NewGuid()}");
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_baseTempDir))
        {
            try { Directory.Delete(_baseTempDir, recursive: true); }
            catch { /* ignore */ }
        }
    }

    #region Helper Methods

    private async Task<string> RunSpecAsync(
        MockSpecExecutionService service,
        string specContent,
        string? sessionId = null,
        int timeoutSeconds = 10,
        McpOptions? options = null)
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        return await SpecTools.RunSpec(
            service,
            sessionManager,
            null!,
            options ?? _options,
            specContent,
            sessionId,
            timeoutSeconds);
    }

    private async Task<string> RunSpecWithSessionAsync(
        MockSpecExecutionService service,
        SessionManager sessionManager,
        string specContent,
        string? sessionId = null,
        int timeoutSeconds = 10)
    {
        return await SpecTools.RunSpec(
            service,
            sessionManager,
            null!,
            _options,
            specContent,
            sessionId,
            timeoutSeconds);
    }

    private async Task<string> RunBatchAsync(
        MockSpecExecutionService service,
        List<BatchSpecInput> specs,
        bool parallel = true,
        int timeoutSeconds = 10,
        McpOptions? options = null)
    {
        return await SpecTools.RunSpecsBatch(
            service,
            null!,
            options ?? _options,
            specs,
            parallel,
            timeoutSeconds);
    }

    #endregion

    #region RunSpec Tests

    [Test]
    public async Task RunSpec_WithoutSession_ExecutesAndReturnsResult()
    {
        var result = await RunSpecAsync(
            MockSpecExecutionService.Successful(),
            "describe(\"test\", () => {});");

        await Assert.That(result).IsNotNull();
        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(parsed.GetProperty("success").GetBoolean()).IsTrue();
    }

    [Test]
    public async Task RunSpec_WithValidSession_CombinesContentAndReturnsSessionInfo()
    {
        var mockService = MockSpecExecutionService.Successful();
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var session = sessionManager.CreateSession();
        session.AppendContent("// previous content\n");

        var result = await RunSpecWithSessionAsync(
            mockService,
            sessionManager,
            "describe(\"new\", () => {});",
            sessionId: session.Id);

        await Assert.That(result).Contains("sessionId");
        await Assert.That(result).Contains(session.Id);
    }

    [Test]
    public async Task RunSpec_WithInvalidSession_ReturnsError()
    {
        var mockService = MockSpecExecutionService.Successful();
        using var sessionManager = new SessionManager(_logger, _baseTempDir);

        var result = await RunSpecWithSessionAsync(
            mockService,
            sessionManager,
            "describe(\"test\", () => {});",
            sessionId: "invalid-session-id");

        await Assert.That(result).Contains("success");
        await Assert.That(result).Contains("false");
        await Assert.That(result).Contains("not found");
    }

    [Test]
    public async Task RunSpec_ClampsTimeoutToMax60()
    {
        var mockService = MockSpecExecutionService.Successful();

        await RunSpecAsync(mockService, "describe(\"test\", () => {});", timeoutSeconds: 120);

        await Assert.That(mockService.LastTimeout).IsEqualTo(TimeSpan.FromSeconds(60));
    }

    [Test]
    public async Task RunSpec_ClampsTimeoutToMin1()
    {
        var mockService = MockSpecExecutionService.Successful();

        await RunSpecAsync(mockService, "describe(\"test\", () => {});", timeoutSeconds: -5);

        await Assert.That(mockService.LastTimeout).IsEqualTo(TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task RunSpec_FailedExecution_ReturnsFailureResult()
    {
        var result = await RunSpecAsync(
            MockSpecExecutionService.Failed("execution failed"),
            "describe(\"test\", () => {});");

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(parsed.GetProperty("success").GetBoolean()).IsFalse();
    }

    [Test]
    public async Task RunSpec_NullContent_ReturnsValidationError()
    {
        var result = await RunSpecAsync(MockSpecExecutionService.Successful(), null!);

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(parsed.GetProperty("success").GetBoolean()).IsFalse();
        await Assert.That(parsed.GetProperty("error").GetProperty("category").GetString())
            .IsEqualTo("Validation");
        await Assert.That(parsed.GetProperty("error").GetProperty("message").GetString())
            .Contains("null or empty");
    }

    [Test]
    public async Task RunSpec_EmptyContent_ReturnsValidationError()
    {
        var result = await RunSpecAsync(MockSpecExecutionService.Successful(), "   ");

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(parsed.GetProperty("success").GetBoolean()).IsFalse();
        await Assert.That(parsed.GetProperty("error").GetProperty("category").GetString())
            .IsEqualTo("Validation");
    }

    [Test]
    public async Task RunSpec_ContentExceedsSizeLimit_ReturnsValidationError()
    {
        var options = new McpOptions { MaxSpecContentSizeBytes = 100 };
        var largeContent = new string('x', 200);

        var result = await RunSpecAsync(MockSpecExecutionService.Successful(), largeContent, options: options);

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(parsed.GetProperty("success").GetBoolean()).IsFalse();
        await Assert.That(parsed.GetProperty("error").GetProperty("category").GetString())
            .IsEqualTo("Validation");
        await Assert.That(parsed.GetProperty("error").GetProperty("message").GetString())
            .Contains("exceeds maximum size");
    }

    #endregion

    #region RunSpecsBatch Tests

    [Test]
    public async Task RunSpecsBatch_EmptySpecs_ReturnsEmptyResult()
    {
        var result = await RunBatchAsync(MockSpecExecutionService.Successful(), []);

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(parsed.GetProperty("allPassed").GetBoolean()).IsTrue();
        await Assert.That(parsed.GetProperty("totalSpecs").GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task RunSpecsBatch_NullSpecs_ReturnsEmptyResult()
    {
        var result = await RunBatchAsync(MockSpecExecutionService.Successful(), null!);

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(parsed.GetProperty("allPassed").GetBoolean()).IsTrue();
        await Assert.That(parsed.GetProperty("totalSpecs").GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task RunSpecsBatch_SingleSpec_ExecutesAndReturnsResult()
    {
        var mockService = MockSpecExecutionService.Successful();

        var result = await RunBatchAsync(mockService,
            [new() { Name = "TestSpec", Content = "describe(\"test\", () => {});" }]);

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(parsed.GetProperty("totalSpecs").GetInt32()).IsEqualTo(1);
        await Assert.That(parsed.GetProperty("passedSpecs").GetInt32()).IsEqualTo(1);
        await Assert.That(mockService.ExecutionCount).IsEqualTo(1);
    }

    [Test]
    public async Task RunSpecsBatch_MultipleSpecs_Parallel_ExecutesAll()
    {
        var mockService = MockSpecExecutionService.Successful();

        var result = await RunBatchAsync(mockService,
        [
            new() { Name = "Spec1", Content = "describe(\"1\", () => {});" },
            new() { Name = "Spec2", Content = "describe(\"2\", () => {});" },
            new() { Name = "Spec3", Content = "describe(\"3\", () => {});" }
        ]);

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(parsed.GetProperty("totalSpecs").GetInt32()).IsEqualTo(3);
        await Assert.That(parsed.GetProperty("passedSpecs").GetInt32()).IsEqualTo(3);
        await Assert.That(mockService.ExecutionCount).IsEqualTo(3);
    }

    [Test]
    public async Task RunSpecsBatch_MultipleSpecs_Sequential_ExecutesAll()
    {
        var mockService = MockSpecExecutionService.Successful();

        var result = await RunBatchAsync(mockService,
        [
            new() { Name = "Spec1", Content = "describe(\"1\", () => {});" },
            new() { Name = "Spec2", Content = "describe(\"2\", () => {});" }
        ], parallel: false);

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(parsed.GetProperty("totalSpecs").GetInt32()).IsEqualTo(2);
        await Assert.That(mockService.ExecutionCount).IsEqualTo(2);
    }

    [Test]
    public async Task RunSpecsBatch_WithFailures_ReportsCorrectCounts()
    {
        var result = await RunBatchAsync(MockSpecExecutionService.Failed("spec failed"),
            [new() { Name = "FailingSpec", Content = "describe(\"fail\", () => {});" }]);

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(parsed.GetProperty("allPassed").GetBoolean()).IsFalse();
        await Assert.That(parsed.GetProperty("failedSpecs").GetInt32()).IsEqualTo(1);
    }

    [Test]
    public async Task RunSpecsBatch_ClampsTimeout()
    {
        var mockService = MockSpecExecutionService.Successful();

        await RunBatchAsync(mockService,
            [new() { Name = "Spec", Content = "describe(\"test\", () => {});" }],
            timeoutSeconds: 120);

        await Assert.That(mockService.LastTimeout).IsEqualTo(TimeSpan.FromSeconds(60));
    }

    [Test]
    public async Task RunSpecsBatch_IncludesResults_InResponse()
    {
        var result = await RunBatchAsync(MockSpecExecutionService.Successful(),
            [new() { Name = "MySpec", Content = "describe(\"test\", () => {});" }]);

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        var results = parsed.GetProperty("results");
        await Assert.That(results.GetArrayLength()).IsEqualTo(1);
        await Assert.That(results[0].GetProperty("name").GetString()).IsEqualTo("MySpec");
    }

    [Test]
    public async Task RunSpecsBatch_ContentExceedsSizeLimit_ReturnsValidationError()
    {
        var options = new McpOptions { MaxSpecContentSizeBytes = 100 };
        var largeContent = new string('x', 200);

        var result = await RunBatchAsync(MockSpecExecutionService.Successful(),
            [new() { Name = "LargeSpec", Content = largeContent }],
            options: options);

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(parsed.GetProperty("allPassed").GetBoolean()).IsFalse();
        await Assert.That(parsed.GetProperty("error").GetString()).Contains("exceeds maximum");
    }

    #endregion

    #region scaffold_specs

    [Test]
    public async Task ScaffoldSpecs_SimpleStructure_GeneratesCode()
    {
        var structure = new ScaffoldNode
        {
            Description = "Calculator",
            Specs = ["add", "subtract"]
        };

        var result = SpecTools.ScaffoldSpecs(structure);

        await Assert.That(result).Contains("describe(\"Calculator\"");
        await Assert.That(result).Contains("it(\"add\"");
        await Assert.That(result).Contains("it(\"subtract\"");
    }

    [Test]
    public async Task ScaffoldSpecs_NestedStructure_GeneratesNestedCode()
    {
        var structure = new ScaffoldNode
        {
            Description = "Math",
            Contexts =
            [
                new ScaffoldNode
                {
                    Description = "basic operations",
                    Specs = ["adds numbers", "subtracts numbers"]
                }
            ]
        };

        var result = SpecTools.ScaffoldSpecs(structure);

        await Assert.That(result).Contains("describe(\"Math\"");
        await Assert.That(result).Contains("describe(\"basic operations\"");
        await Assert.That(result).Contains("it(\"adds numbers\"");
    }

    [Test]
    public async Task ScaffoldSpecs_EmptyStructure_GeneratesEmptyDescribe()
    {
        var structure = new ScaffoldNode
        {
            Description = "Empty"
        };

        var result = SpecTools.ScaffoldSpecs(structure);

        await Assert.That(result).Contains("describe(\"Empty\"");
    }

    [Test]
    public async Task ScaffoldSpecs_WithSpecsAndContexts_GeneratesBoth()
    {
        var structure = new ScaffoldNode
        {
            Description = "Calculator",
            Specs = ["should exist"],
            Contexts =
            [
                new ScaffoldNode
                {
                    Description = "addition",
                    Specs = ["adds positive numbers", "adds negative numbers"]
                }
            ]
        };

        var result = SpecTools.ScaffoldSpecs(structure);

        await Assert.That(result).Contains("describe(\"Calculator\"");
        await Assert.That(result).Contains("it(\"should exist\"");
        await Assert.That(result).Contains("describe(\"addition\"");
        await Assert.That(result).Contains("it(\"adds positive numbers\"");
    }

    #endregion

    #region FormatProgressMessage Tests

    [Test]
    public async Task FormatProgressMessage_StartType_ReturnsStartingMessage()
    {
        var notification = new SpecProgressNotification { Type = "start", Total = 5 };

        var result = SpecTools.FormatProgressMessage(notification);

        await Assert.That(result).IsEqualTo("Starting 5 specs...");
    }

    [Test]
    public async Task FormatProgressMessage_ProgressType_ReturnsProgressMessage()
    {
        var notification = new SpecProgressNotification
        {
            Type = "progress",
            Completed = 3,
            Total = 10,
            Status = "passed",
            Spec = "adds numbers"
        };

        var result = SpecTools.FormatProgressMessage(notification);

        await Assert.That(result).IsEqualTo("[3/10] passed: adds numbers");
    }

    [Test]
    public async Task FormatProgressMessage_CompleteType_ReturnsCompletedMessage()
    {
        var notification = new SpecProgressNotification
        {
            Type = "complete",
            Passed = 8,
            Failed = 2
        };

        var result = SpecTools.FormatProgressMessage(notification);

        await Assert.That(result).IsEqualTo("Completed: 8 passed, 2 failed");
    }

    [Test]
    public async Task FormatProgressMessage_UnknownType_ReturnsTypeName()
    {
        var notification = new SpecProgressNotification { Type = "custom" };

        var result = SpecTools.FormatProgressMessage(notification);

        await Assert.That(result).IsEqualTo("custom");
    }

    [Test]
    public async Task FormatProgressMessage_ProgressWithFailed_ReturnsFormattedMessage()
    {
        var notification = new SpecProgressNotification
        {
            Type = "progress",
            Completed = 5,
            Total = 10,
            Status = "failed",
            Spec = "throws exception"
        };

        var result = SpecTools.FormatProgressMessage(notification);

        await Assert.That(result).IsEqualTo("[5/10] failed: throws exception");
    }

    #endregion

    #region FormatBatchProgressMessage Tests

    [Test]
    public async Task FormatBatchProgressMessage_ReturnsFormattedMessage()
    {
        var result = SpecTools.FormatBatchProgressMessage(3, 10, "Calculator");

        await Assert.That(result).IsEqualTo("[3/10] Completed: Calculator");
    }

    [Test]
    public async Task FormatBatchProgressMessage_FirstSpec_ShowsCorrectProgress()
    {
        var result = SpecTools.FormatBatchProgressMessage(1, 5, "FirstSpec");

        await Assert.That(result).IsEqualTo("[1/5] Completed: FirstSpec");
    }

    [Test]
    public async Task FormatBatchProgressMessage_LastSpec_ShowsCorrectProgress()
    {
        var result = SpecTools.FormatBatchProgressMessage(5, 5, "LastSpec");

        await Assert.That(result).IsEqualTo("[5/5] Completed: LastSpec");
    }

    #endregion
}
