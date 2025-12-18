using System.Text.Json;
using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Services;
using DraftSpec.Mcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DraftSpec.Tests.Mcp.Tools;

/// <summary>
/// Integration tests for SpecTools MCP methods.
/// Tests use in-process execution mode to avoid subprocess overhead and McpServer dependency.
/// </summary>
[NotInParallel("SpecTools")]
public class SpecToolsTests
{
    private readonly SpecExecutionService _executionService;
    private readonly InProcessSpecRunner _inProcessRunner;
    private readonly SessionManager _sessionManager;
    private readonly TempFileManager _tempFileManager;

    public SpecToolsTests()
    {
        _tempFileManager = new TempFileManager(
            NullLogger<TempFileManager>.Instance);
        _executionService = new SpecExecutionService(
            _tempFileManager,
            NullLogger<SpecExecutionService>.Instance);
        _inProcessRunner = new InProcessSpecRunner(
            NullLogger<InProcessSpecRunner>.Instance);
        _sessionManager = new SessionManager(
            NullLogger<SessionManager>.Instance);
    }

    #region run_spec - Basic Execution

    [Test]
    public async Task RunSpec_PassingSpec_ReturnsSuccess()
    {
        var specContent = """
            describe("Math", () =>
            {
                it("adds numbers", () => expect(1 + 1).toBe(2));
            });
            """;

        var result = await SpecTools.RunSpec(
            _executionService,
            _inProcessRunner,
            _sessionManager,
            server: null!,
            specContent,
            sessionId: null,
            timeoutSeconds: 10,
            inProcess: true);

        var json = JsonDocument.Parse(result);
        await Assert.That(json.RootElement.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(json.RootElement.GetProperty("exitCode").GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task RunSpec_FailingSpec_ReturnsFailure()
    {
        var specContent = """
            describe("Math", () =>
            {
                it("fails", () => expect(1).toBe(2));
            });
            """;

        var result = await SpecTools.RunSpec(
            _executionService,
            _inProcessRunner,
            _sessionManager,
            server: null!,
            specContent,
            sessionId: null,
            timeoutSeconds: 10,
            inProcess: true);

        var json = JsonDocument.Parse(result);
        await Assert.That(json.RootElement.GetProperty("success").GetBoolean()).IsFalse();
        await Assert.That(json.RootElement.GetProperty("exitCode").GetInt32()).IsEqualTo(1);
    }

    [Test]
    public async Task RunSpec_IncludesReport()
    {
        var specContent = """
            describe("Test", () =>
            {
                it("spec1", () => expect(true).toBeTrue());
                it("spec2", () => expect(true).toBeTrue());
            });
            """;

        var result = await SpecTools.RunSpec(
            _executionService,
            _inProcessRunner,
            _sessionManager,
            server: null!,
            specContent,
            sessionId: null,
            timeoutSeconds: 10,
            inProcess: true);

        var json = JsonDocument.Parse(result);
        await Assert.That(json.RootElement.TryGetProperty("report", out var report)).IsTrue();
        await Assert.That(report.GetProperty("summary").GetProperty("total").GetInt32()).IsEqualTo(2);
    }

    [Test]
    public async Task RunSpec_IncludesDuration()
    {
        var specContent = """
            describe("Test", () =>
            {
                it("runs", () => expect(true).toBeTrue());
            });
            """;

        var result = await SpecTools.RunSpec(
            _executionService,
            _inProcessRunner,
            _sessionManager,
            server: null!,
            specContent,
            sessionId: null,
            timeoutSeconds: 10,
            inProcess: true);

        var json = JsonDocument.Parse(result);
        await Assert.That(json.RootElement.TryGetProperty("durationMs", out var duration)).IsTrue();
        await Assert.That(duration.GetDouble()).IsGreaterThan(0);
    }

    #endregion

    #region run_spec - Timeout Handling

    [Test]
    public async Task RunSpec_TimeoutClamped_ToMaximum60()
    {
        var specContent = """
            describe("Test", () =>
            {
                it("runs", () => expect(true).toBeTrue());
            });
            """;

        // Should not throw even with large timeout (clamped to 60)
        var result = await SpecTools.RunSpec(
            _executionService,
            _inProcessRunner,
            _sessionManager,
            server: null!,
            specContent,
            sessionId: null,
            timeoutSeconds: 1000,
            inProcess: true);

        var json = JsonDocument.Parse(result);
        await Assert.That(json.RootElement.GetProperty("success").GetBoolean()).IsTrue();
    }

    [Test]
    public async Task RunSpec_TimeoutClamped_ToMinimum1()
    {
        var specContent = """
            describe("Test", () =>
            {
                it("runs", () => expect(true).toBeTrue());
            });
            """;

        // Should work with very small timeout (clamped to 1)
        var result = await SpecTools.RunSpec(
            _executionService,
            _inProcessRunner,
            _sessionManager,
            server: null!,
            specContent,
            sessionId: null,
            timeoutSeconds: 0,
            inProcess: true);

        var json = JsonDocument.Parse(result);
        await Assert.That(json.RootElement.GetProperty("success").GetBoolean()).IsTrue();
    }

    #endregion

    #region run_spec - Session Integration

    [Test]
    public async Task RunSpec_InvalidSession_ReturnsError()
    {
        var specContent = """
            describe("Test", () =>
            {
                it("runs", () => expect(true).toBeTrue());
            });
            """;

        var result = await SpecTools.RunSpec(
            _executionService,
            _inProcessRunner,
            _sessionManager,
            server: null!,
            specContent,
            sessionId: "non-existent-session",
            timeoutSeconds: 10,
            inProcess: true);

        var json = JsonDocument.Parse(result);
        await Assert.That(json.RootElement.GetProperty("success").GetBoolean()).IsFalse();
        await Assert.That(json.RootElement.GetProperty("error").GetString()).Contains("not found");
    }

    [Test]
    public async Task RunSpec_WithSession_ReturnsSessionInfo()
    {
        // Create session
        var session = _sessionManager.CreateSession();

        var specContent = """
            describe("Test", () =>
            {
                it("runs", () => expect(true).toBeTrue());
            });
            """;

        var result = await SpecTools.RunSpec(
            _executionService,
            _inProcessRunner,
            _sessionManager,
            server: null!,
            specContent,
            sessionId: session.Id,
            timeoutSeconds: 10,
            inProcess: true);

        var json = JsonDocument.Parse(result);
        await Assert.That(json.RootElement.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(json.RootElement.GetProperty("sessionId").GetString()).IsEqualTo(session.Id);
        await Assert.That(json.RootElement.TryGetProperty("accumulatedContentLength", out _)).IsTrue();
    }

    [Test]
    public async Task RunSpec_WithSession_AccumulatesContent()
    {
        var session = _sessionManager.CreateSession();

        // First call
        var spec1 = """
            var helper = () => 42;
            describe("First", () =>
            {
                it("defines helper", () => expect(helper()).toBe(42));
            });
            """;

        await SpecTools.RunSpec(
            _executionService,
            _inProcessRunner,
            _sessionManager,
            server: null!,
            spec1,
            sessionId: session.Id,
            timeoutSeconds: 10,
            inProcess: true);

        // Second call uses accumulated content
        var spec2 = """
            describe("Second", () =>
            {
                it("uses helper", () => expect(helper()).toBe(42));
            });
            """;

        var result = await SpecTools.RunSpec(
            _executionService,
            _inProcessRunner,
            _sessionManager,
            server: null!,
            spec2,
            sessionId: session.Id,
            timeoutSeconds: 10,
            inProcess: true);

        var json = JsonDocument.Parse(result);
        await Assert.That(json.RootElement.GetProperty("success").GetBoolean()).IsTrue();
    }

    [Test]
    public async Task RunSpec_FailedRun_DoesNotAccumulate()
    {
        var session = _sessionManager.CreateSession();

        // Failed spec should not be accumulated
        var failingSpec = """
            describe("Failing", () =>
            {
                it("fails", () => expect(1).toBe(2));
            });
            """;

        await SpecTools.RunSpec(
            _executionService,
            _inProcessRunner,
            _sessionManager,
            server: null!,
            failingSpec,
            sessionId: session.Id,
            timeoutSeconds: 10,
            inProcess: true);

        // Session should not have accumulated the failing content
        await Assert.That(session.AccumulatedContent).IsEqualTo("");
    }

    #endregion

    #region run_spec - Compilation Errors

    [Test]
    public async Task RunSpec_CompilationError_ReturnsErrorDetails()
    {
        var specContent = """
            describe("Bad", () =>
            {
                it("syntax error", () =>
                {
                    var x = // missing value
                });
            });
            """;

        var result = await SpecTools.RunSpec(
            _executionService,
            _inProcessRunner,
            _sessionManager,
            server: null!,
            specContent,
            sessionId: null,
            timeoutSeconds: 10,
            inProcess: true);

        var json = JsonDocument.Parse(result);
        await Assert.That(json.RootElement.GetProperty("success").GetBoolean()).IsFalse();
        await Assert.That(json.RootElement.TryGetProperty("error", out var error)).IsTrue();
        await Assert.That(error.GetProperty("category").GetString()!.ToLower()).IsEqualTo("compilation");
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

    #endregion
}
