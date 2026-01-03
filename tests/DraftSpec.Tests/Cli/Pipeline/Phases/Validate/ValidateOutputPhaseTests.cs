using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Validate;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Validate;

/// <summary>
/// Tests for <see cref="ValidateOutputPhase"/>.
/// </summary>
public class ValidateOutputPhaseTests
{
    #region Success Cases

    [Test]
    public async Task ExecuteAsync_NoErrors_ReturnsExitSuccess()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResults(console, CreateValidFile("test.spec.csx", 3));

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(999), // Should not be called
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(ValidateOutputPhase.ExitSuccess);
    }

    [Test]
    public async Task ExecuteAsync_ValidFile_ShowsCheckmark()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResults(console, CreateValidFile("test.spec.csx", 2));

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("\u2713"); // checkmark
        await Assert.That(console.Output).Contains("test.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_ValidFiles_ShowsSummary()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResults(console,
            CreateValidFile("a.spec.csx", 2),
            CreateValidFile("b.spec.csx", 3));

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("Files: 2");
        await Assert.That(console.Output).Contains("Specs: 5");
    }

    #endregion

    #region Error Cases

    [Test]
    public async Task ExecuteAsync_WithErrors_ReturnsExitErrors()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResults(console,
            CreateFileWithErrors("bad.spec.csx", "missing description"));

        var result = await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(result).IsEqualTo(ValidateOutputPhase.ExitErrors);
    }

    [Test]
    public async Task ExecuteAsync_WithErrors_ShowsCrossmark()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResults(console,
            CreateFileWithErrors("bad.spec.csx", "missing description"));

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Errors).Contains("\u2717"); // X mark
        await Assert.That(console.Errors).Contains("bad.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_WithErrorLocation_ShowsLineNumber()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var result = CreateFileWithErrors("bad.spec.csx", "missing description");
        result.Errors[0].LineNumber = 42;
        var context = CreateContextWithResults(console, result);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Errors).Contains("Line 42:");
    }

    #endregion

    #region Warning Cases

    [Test]
    public async Task ExecuteAsync_WarningsOnly_ReturnsExitSuccess()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResults(console,
            CreateFileWithWarnings("dynamic.spec.csx", "dynamic description"));

        var result = await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(result).IsEqualTo(ValidateOutputPhase.ExitSuccess);
    }

    [Test]
    public async Task ExecuteAsync_WarningsWithStrict_ReturnsExitWarnings()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResults(console,
            CreateFileWithWarnings("dynamic.spec.csx", "dynamic description"));
        context.Set(ContextKeys.Strict, true);

        var result = await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(result).IsEqualTo(ValidateOutputPhase.ExitWarnings);
    }

    [Test]
    public async Task ExecuteAsync_WarningsWithStrict_CountsWarningsInSummary()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResults(console,
            CreateFileWithWarnings("dynamic.spec.csx", "warning1", "warning2"));

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("Warnings: 2");
    }

    #endregion

    #region Quiet Mode

    [Test]
    public async Task ExecuteAsync_QuietMode_SuppressesOutput()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResults(console, CreateValidFile("test.spec.csx", 2));
        context.Set(ContextKeys.Quiet, true);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).DoesNotContain("\u2713");
        await Assert.That(console.Output).DoesNotContain("test.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_QuietModeWithErrors_StillShowsErrors()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResults(console,
            CreateFileWithErrors("bad.spec.csx", "error message"));
        context.Set(ContextKeys.Quiet, true);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Errors).Contains("bad.spec.csx");
        await Assert.That(console.Errors).Contains("error message");
    }

    [Test]
    public async Task ExecuteAsync_QuietModeWithWarnings_SuppressesWarnings()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResults(console,
            CreateFileWithWarnings("dynamic.spec.csx", "warning"));
        context.Set(ContextKeys.Quiet, true);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).DoesNotContain("\u26a0");
        await Assert.That(console.Output).DoesNotContain("warning");
    }

    #endregion

    #region Terminal Phase Tests

    [Test]
    public async Task ExecuteAsync_TerminalPhase_DoesNotCallNextPipeline()
    {
        var phase = new ValidateOutputPhase();
        var context = CreateContextWithResults(new MockConsole(), CreateValidFile("test.spec.csx", 1));
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(99); },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsFalse();
    }

    #endregion

    #region Error Tests

    [Test]
    public async Task ExecuteAsync_ValidationResultsNotSet_ReturnsError()
    {
        var phase = new ValidateOutputPhase();
        var console = new MockConsole();
        var context = CreateContext(console);
        // Don't set ValidationResults

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("ValidationResults not set");
    }

    #endregion

    #region Helper Methods

    private static CommandContext CreateContext(MockConsole? console = null)
    {
        return new CommandContext
        {
            Path = "/test",
            Console = console ?? new MockConsole(),
            FileSystem = new MockFileSystem()
        };
    }

    private static CommandContext CreateContextWithResults(
        MockConsole console,
        params FileValidationResult[] results)
    {
        var context = CreateContext(console);
        context.Set<IReadOnlyList<FileValidationResult>>(ContextKeys.ValidationResults, results);
        return context;
    }

    private static FileValidationResult CreateValidFile(string filePath, int specCount)
    {
        return new FileValidationResult
        {
            FilePath = filePath,
            SpecCount = specCount
        };
    }

    private static FileValidationResult CreateFileWithErrors(string filePath, params string[] errors)
    {
        var result = new FileValidationResult { FilePath = filePath };
        foreach (var error in errors)
        {
            result.Errors.Add(new ValidationIssue { Message = error });
        }
        return result;
    }

    private static FileValidationResult CreateFileWithWarnings(string filePath, params string[] warnings)
    {
        var result = new FileValidationResult { FilePath = filePath, SpecCount = 1 };
        foreach (var warning in warnings)
        {
            result.Warnings.Add(new ValidationIssue { Message = warning });
        }
        return result;
    }

    #endregion
}
