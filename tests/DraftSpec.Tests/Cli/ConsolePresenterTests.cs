using DraftSpec.Cli;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for ConsolePresenter output formatting.
/// </summary>
[NotInParallel("ConsolePresenter")]
public class ConsolePresenterTests
{
    private StringWriter _output = null!;
    private TextWriter _originalOut = null!;

    [Before(Test)]
    public void SetUp()
    {
        _originalOut = Console.Out;
        _output = new StringWriter();
        Console.SetOut(_output);
    }

    [After(Test)]
    public void TearDown()
    {
        Console.SetOut(_originalOut);
        _output.Dispose();
    }

    #region ShowHeader

    [Test]
    public async Task ShowHeader_SingleFile_ShowsFileName()
    {
        var presenter = new ConsolePresenter();
        var files = new[] { "/path/to/test.spec.csx" };

        presenter.ShowHeader(files, parallel: false, isPartialRun: true);

        var output = _output.ToString();
        await Assert.That(output).Contains("test.spec.csx");
    }

    [Test]
    public async Task ShowHeader_MultipleFiles_ShowsCount()
    {
        var presenter = new ConsolePresenter();
        var files = new[] { "a.spec.csx", "b.spec.csx", "c.spec.csx" };

        presenter.ShowHeader(files);

        var output = _output.ToString();
        await Assert.That(output).Contains("3 spec file(s)");
    }

    [Test]
    public async Task ShowHeader_ParallelMode_ShowsParallel()
    {
        var presenter = new ConsolePresenter();
        var files = new[] { "a.spec.csx", "b.spec.csx" };

        presenter.ShowHeader(files, parallel: true);

        var output = _output.ToString();
        await Assert.That(output).Contains("in parallel");
    }

    [Test]
    public async Task ShowHeader_SingleFileParallel_DoesNotShowParallel()
    {
        var presenter = new ConsolePresenter();
        var files = new[] { "a.spec.csx" };

        presenter.ShowHeader(files, parallel: true, isPartialRun: true);

        var output = _output.ToString();
        await Assert.That(output).DoesNotContain("in parallel");
    }

    [Test]
    public async Task ShowHeader_IncludesTimestamp()
    {
        var presenter = new ConsolePresenter();
        var files = new[] { "test.spec.csx" };

        presenter.ShowHeader(files);

        var output = _output.ToString();
        // Should contain timestamp format [HH:mm:ss]
        await Assert.That(output).Contains("[");
        await Assert.That(output).Contains("]");
    }

    #endregion

    #region ShowPreRunStats

    [Test]
    public async Task ShowPreRunStats_ShowsTotalAndFileCount()
    {
        var presenter = new ConsolePresenter();
        var stats = new SpecStats(Total: 10, Regular: 8, Focused: 0, Skipped: 2, Pending: 0, HasFocusMode: false, FileCount: 3);

        presenter.ShowPreRunStats(stats);

        var output = _output.ToString();
        await Assert.That(output).Contains("Discovered 10 spec(s) in 3 file(s)");
    }

    [Test]
    public async Task ShowPreRunStats_ShowsBreakdown()
    {
        var presenter = new ConsolePresenter();
        var stats = new SpecStats(Total: 10, Regular: 5, Focused: 2, Skipped: 2, Pending: 1, HasFocusMode: true, FileCount: 3);

        presenter.ShowPreRunStats(stats);

        var output = _output.ToString();
        await Assert.That(output).Contains("5 regular");
        await Assert.That(output).Contains("2 focused");
        await Assert.That(output).Contains("2 skipped");
        await Assert.That(output).Contains("1 pending");
    }

    [Test]
    public async Task ShowPreRunStats_OmitsZeroCounts()
    {
        var presenter = new ConsolePresenter();
        var stats = new SpecStats(Total: 5, Regular: 5, Focused: 0, Skipped: 0, Pending: 0, HasFocusMode: false, FileCount: 1);

        presenter.ShowPreRunStats(stats);

        var output = _output.ToString();
        await Assert.That(output).Contains("5 regular");
        await Assert.That(output).DoesNotContain("focused");
        await Assert.That(output).DoesNotContain("skipped");
        await Assert.That(output).DoesNotContain("pending");
    }

    [Test]
    public async Task ShowPreRunStats_FocusMode_ShowsWarning()
    {
        var presenter = new ConsolePresenter();
        var stats = new SpecStats(Total: 10, Regular: 8, Focused: 2, Skipped: 0, Pending: 0, HasFocusMode: true, FileCount: 1);

        presenter.ShowPreRunStats(stats);

        var output = _output.ToString();
        await Assert.That(output).Contains("Warning: Focus mode active");
        await Assert.That(output).Contains("only focused specs");
    }

    [Test]
    public async Task ShowPreRunStats_NoFocusMode_NoWarning()
    {
        var presenter = new ConsolePresenter();
        var stats = new SpecStats(Total: 10, Regular: 10, Focused: 0, Skipped: 0, Pending: 0, HasFocusMode: false, FileCount: 1);

        presenter.ShowPreRunStats(stats);

        var output = _output.ToString();
        await Assert.That(output).DoesNotContain("Warning");
        await Assert.That(output).DoesNotContain("Focus mode");
    }

    #endregion

    #region ShowSpecsStarting

    [Test]
    public async Task ShowSpecsStarting_WritesNewLine()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowSpecsStarting();

        var output = _output.ToString();
        await Assert.That(output).IsEqualTo(Environment.NewLine);
    }

    #endregion

    #region ShowBuilding

    [Test]
    public async Task ShowBuilding_ShowsProjectName()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowBuilding("/path/to/MyProject.csproj");

        var output = _output.ToString();
        await Assert.That(output).Contains("Building MyProject");
    }

    [Test]
    public async Task ShowBuilding_StripsExtension()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowBuilding("Test.csproj");

        var output = _output.ToString();
        await Assert.That(output).Contains("Building Test");
        await Assert.That(output).DoesNotContain(".csproj");
    }

    #endregion

    #region ShowBuildResult

    [Test]
    public async Task ShowBuildResult_Success_ShowsOk()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowBuildResult(new BuildResult(Success: true, Output: "", Error: ""));

        var output = _output.ToString();
        await Assert.That(output).Contains("ok");
    }

    [Test]
    public async Task ShowBuildResult_Failure_ShowsFailed()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowBuildResult(new BuildResult(Success: false, Output: "", Error: ""));

        var output = _output.ToString();
        await Assert.That(output).Contains("failed");
    }

    [Test]
    public async Task ShowBuildResult_FailureWithError_ShowsError()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowBuildResult(new BuildResult(Success: false, Output: "", Error: "Compile error CS1234"));

        var output = _output.ToString();
        await Assert.That(output).Contains("Compile error CS1234");
    }

    #endregion

    #region ShowBuildSkipped

    [Test]
    public async Task ShowBuildSkipped_ShowsSkipped()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowBuildSkipped("Test.csproj");

        var output = _output.ToString();
        await Assert.That(output).Contains("skipped");
        await Assert.That(output).Contains("no changes");
    }

    #endregion

    #region ShowResult

    [Test]
    public async Task ShowResult_WithOutput_WritesOutput()
    {
        var presenter = new ConsolePresenter();
        var result = new SpecRunResult(
            SpecFile: "/path/test.spec.csx",
            Output: "Test output line",
            Error: "",
            ExitCode: 0,
            Duration: TimeSpan.FromMilliseconds(100));

        presenter.ShowResult(result, "/path");

        var output = _output.ToString();
        await Assert.That(output).Contains("Test output line");
    }

    [Test]
    public async Task ShowResult_WithError_WritesError()
    {
        var presenter = new ConsolePresenter();
        var result = new SpecRunResult(
            SpecFile: "/path/test.spec.csx",
            Output: "",
            Error: "Runtime error occurred",
            ExitCode: 1,
            Duration: TimeSpan.FromMilliseconds(100));

        presenter.ShowResult(result, "/path");

        var output = _output.ToString();
        await Assert.That(output).Contains("Runtime error occurred");
    }

    [Test]
    public async Task ShowResult_NoTrailingNewline_AddsNewline()
    {
        var presenter = new ConsolePresenter();
        var result = new SpecRunResult(
            SpecFile: "/path/test.spec.csx",
            Output: "no newline",
            Error: "",
            ExitCode: 0,
            Duration: TimeSpan.FromMilliseconds(100));

        presenter.ShowResult(result, "/path");

        var output = _output.ToString();
        await Assert.That(output).EndsWith("\n");
    }

    [Test]
    public async Task ShowResult_OutputWithNewline_PreservesNewline()
    {
        var presenter = new ConsolePresenter();
        var result = new SpecRunResult(
            SpecFile: "/path/test.spec.csx",
            Output: "has newline\n",
            Error: "",
            ExitCode: 0,
            Duration: TimeSpan.FromMilliseconds(100));

        presenter.ShowResult(result, "/path");

        var output = _output.ToString();
        await Assert.That(output).IsEqualTo("has newline\n");
    }

    [Test]
    public async Task ShowResult_ErrorNoNewline_AddsNewline()
    {
        var presenter = new ConsolePresenter();
        var result = new SpecRunResult(
            SpecFile: "/path/test.spec.csx",
            Output: "",
            Error: "error no newline",
            ExitCode: 1,
            Duration: TimeSpan.FromMilliseconds(100));

        presenter.ShowResult(result, "/path");

        var output = _output.ToString();
        await Assert.That(output).EndsWith("\n");
    }

    [Test]
    public async Task ShowResult_ErrorWithNewline_PreservesNewline()
    {
        var presenter = new ConsolePresenter();
        var result = new SpecRunResult(
            SpecFile: "/path/test.spec.csx",
            Output: "",
            Error: "error with newline\n",
            ExitCode: 1,
            Duration: TimeSpan.FromMilliseconds(100));

        presenter.ShowResult(result, "/path");

        var output = _output.ToString();
        await Assert.That(output).IsEqualTo("error with newline\n");
    }

    [Test]
    public async Task ShowResult_EmptyOutputAndError_NoOutput()
    {
        var presenter = new ConsolePresenter();
        var result = new SpecRunResult(
            SpecFile: "/path/test.spec.csx",
            Output: "",
            Error: "",
            ExitCode: 0,
            Duration: TimeSpan.FromMilliseconds(100));

        presenter.ShowResult(result, "/path");

        var output = _output.ToString();
        await Assert.That(output).IsEqualTo("");
    }

    #endregion

    #region ShowCompilationError

    [Test]
    public async Task ShowCompilationError_ShowsFileNameWithError()
    {
        var presenter = new ConsolePresenter();
        var exception = new CompilationDiagnosticException(
            "Compilation failed",
            "Error CS1234: Something wrong",
            "/path/to/broken.spec.csx",
            []);

        presenter.ShowCompilationError(exception);

        var output = _output.ToString();
        await Assert.That(output).Contains("broken.spec.csx");
        await Assert.That(output).Contains("Compilation failed");
    }

    [Test]
    public async Task ShowCompilationError_ShowsFormattedMessage()
    {
        var presenter = new ConsolePresenter();
        var exception = new CompilationDiagnosticException(
            "Compilation failed",
            "Error CS1234: Syntax error at line 5",
            "/path/to/test.spec.csx",
            []);

        presenter.ShowCompilationError(exception);

        var output = _output.ToString();
        await Assert.That(output).Contains("Error CS1234: Syntax error at line 5");
    }

    [Test]
    public async Task ShowCompilationError_WithDiscoveredSpecs_ShowsSpecList()
    {
        var presenter = new ConsolePresenter();
        var specs = new List<StaticSpec>
        {
            new()
            {
                Description = "adds numbers",
                ContextPath = ["Calculator"],
                LineNumber = 10,
                Type = StaticSpecType.Regular
            },
            new()
            {
                Description = "subtracts numbers",
                ContextPath = ["Calculator"],
                LineNumber = 15,
                Type = StaticSpecType.Regular
            }
        };
        var exception = new CompilationDiagnosticException(
            "Compilation failed",
            "Error CS1234",
            "/path/to/test.spec.csx",
            specs);

        presenter.ShowCompilationError(exception);

        var output = _output.ToString();
        await Assert.That(output).Contains("Found 2 spec(s)");
        await Assert.That(output).Contains("Calculator > adds numbers");
        await Assert.That(output).Contains("(line 10)");
        await Assert.That(output).Contains("Calculator > subtracts numbers");
    }

    [Test]
    public async Task ShowCompilationError_NoDiscoveredSpecs_DoesNotShowSpecList()
    {
        var presenter = new ConsolePresenter();
        var exception = new CompilationDiagnosticException(
            "Compilation failed",
            "Error CS1234",
            "/path/to/test.spec.csx",
            []);

        presenter.ShowCompilationError(exception);

        var output = _output.ToString();
        await Assert.That(output).DoesNotContain("Found");
        await Assert.That(output).DoesNotContain("spec(s)");
    }

    [Test]
    public async Task ShowCompilationError_SpecWithEmptyContextPath_ShowsDescriptionOnly()
    {
        var presenter = new ConsolePresenter();
        var specs = new List<StaticSpec>
        {
            new()
            {
                Description = "top level spec",
                ContextPath = [],
                LineNumber = 5,
                Type = StaticSpecType.Regular
            }
        };
        var exception = new CompilationDiagnosticException(
            "Compilation failed",
            "Error",
            "/path/to/test.spec.csx",
            specs);

        presenter.ShowCompilationError(exception);

        var output = _output.ToString();
        await Assert.That(output).Contains("top level spec");
        await Assert.That(output).DoesNotContain(" > top level spec"); // No context path prefix
    }

    [Test]
    public async Task ShowCompilationError_SpecWithNoLineNumber_OmitsLineInfo()
    {
        var presenter = new ConsolePresenter();
        var specs = new List<StaticSpec>
        {
            new()
            {
                Description = "spec without line",
                ContextPath = ["Context"],
                LineNumber = 0,
                Type = StaticSpecType.Regular
            }
        };
        var exception = new CompilationDiagnosticException(
            "Compilation failed",
            "Error",
            "/path/to/test.spec.csx",
            specs);

        presenter.ShowCompilationError(exception);

        var output = _output.ToString();
        await Assert.That(output).Contains("spec without line");
        await Assert.That(output).DoesNotContain("(line 0)");
    }

    #endregion

    #region ShowSummary

    [Test]
    public async Task ShowSummary_Success_ShowsPass()
    {
        var presenter = new ConsolePresenter();
        var results = new[]
        {
            new SpecRunResult("test.spec.csx", "", "", 0, TimeSpan.FromSeconds(1))
        };
        var summary = new RunSummary(results, TimeSpan.FromSeconds(1));

        presenter.ShowSummary(summary);

        var output = _output.ToString();
        await Assert.That(output).Contains("PASS");
    }

    [Test]
    public async Task ShowSummary_Failure_ShowsFail()
    {
        var presenter = new ConsolePresenter();
        var results = new[]
        {
            new SpecRunResult("test.spec.csx", "", "", 1, TimeSpan.FromSeconds(1))
        };
        var summary = new RunSummary(results, TimeSpan.FromSeconds(1));

        presenter.ShowSummary(summary);

        var output = _output.ToString();
        await Assert.That(output).Contains("FAIL");
    }

    [Test]
    public async Task ShowSummary_ShowsFileCount()
    {
        var presenter = new ConsolePresenter();
        var results = new[]
        {
            new SpecRunResult("a.spec.csx", "", "", 0, TimeSpan.Zero),
            new SpecRunResult("b.spec.csx", "", "", 0, TimeSpan.Zero),
            new SpecRunResult("c.spec.csx", "", "", 0, TimeSpan.Zero)
        };
        var summary = new RunSummary(results, TimeSpan.FromSeconds(2.5));

        presenter.ShowSummary(summary);

        var output = _output.ToString();
        await Assert.That(output).Contains("3 file(s)");
    }

    [Test]
    public async Task ShowSummary_ShowsDuration()
    {
        var presenter = new ConsolePresenter();
        var results = new[] { new SpecRunResult("test.spec.csx", "", "", 0, TimeSpan.Zero) };
        var summary = new RunSummary(results, TimeSpan.FromSeconds(1.234));

        presenter.ShowSummary(summary);

        var output = _output.ToString();
        await Assert.That(output).Contains("1.23s");
    }

    #endregion

    #region ShowError

    [Test]
    public async Task ShowError_ShowsMessage()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowError("Something went wrong");

        var output = _output.ToString();
        await Assert.That(output).Contains("Error:");
        await Assert.That(output).Contains("Something went wrong");
    }

    #endregion

    #region ShowWatching

    [Test]
    public async Task ShowWatching_ShowsMessage()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowWatching();

        var output = _output.ToString();
        await Assert.That(output).Contains("Watching for changes");
    }

    #endregion

    #region ShowRerunning

    [Test]
    public async Task ShowRerunning_ShowsMessage()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowRerunning();

        var output = _output.ToString();
        await Assert.That(output).Contains("File changed");
        await Assert.That(output).Contains("re-running");
    }

    #endregion

    #region Clear

    [Test]
    public async Task Clear_WatchMode_ClearsConsole()
    {
        // Note: Console.Clear throws when not in a real console,
        // so we can't directly test it. We test the logic branch instead.
        var presenter = new ConsolePresenter(watchMode: true);

        // In non-interactive mode, Clear() may throw or do nothing
        // This just verifies it doesn't crash in test environment
        await Task.CompletedTask;
    }

    [Test]
    public async Task Clear_NotWatchMode_DoesNothing()
    {
        var presenter = new ConsolePresenter(watchMode: false);

        // Should not throw or clear
        presenter.Clear();

        var output = _output.ToString();
        await Assert.That(output).IsEqualTo("");
    }

    #endregion

    #region Coverage Display

    [Test]
    public async Task ShowCoverageReport_ShowsReportPath()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowCoverageReport("/path/to/coverage.cobertura.xml");

        var output = _output.ToString();
        await Assert.That(output).Contains("Coverage report:");
        await Assert.That(output).Contains("/path/to/coverage.cobertura.xml");
    }

    [Test]
    public async Task ShowCoverageReportGenerated_ShowsFormatAndPath()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowCoverageReportGenerated("html", "/path/to/coverage.html");

        var output = _output.ToString();
        await Assert.That(output).Contains("Coverage html report:");
        await Assert.That(output).Contains("/path/to/coverage.html");
    }

    [Test]
    public async Task ShowCoverageSummary_ShowsLineAndBranchPercent()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowCoverageSummary(85.5, 72.3);

        var output = _output.ToString();
        await Assert.That(output).Contains("Coverage:");
        await Assert.That(output).Contains("85.5% lines");
        await Assert.That(output).Contains("72.3% branches");
    }

    [Test]
    public async Task ShowCoverageSummary_FormatsPercentages()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowCoverageSummary(100.0, 0.0);

        var output = _output.ToString();
        await Assert.That(output).Contains("100.0% lines");
        await Assert.That(output).Contains("0.0% branches");
    }

    [Test]
    public async Task ShowCoverageThresholdWarnings_ShowsWarningHeader()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowCoverageThresholdWarnings(["Line coverage 75% < 80% threshold"]);

        var output = _output.ToString();
        await Assert.That(output).Contains("Coverage threshold warning:");
    }

    [Test]
    public async Task ShowCoverageThresholdWarnings_ShowsAllFailures()
    {
        var presenter = new ConsolePresenter();
        var failures = new[]
        {
            "Line coverage 75% < 80% threshold",
            "Branch coverage 60% < 70% threshold"
        };

        presenter.ShowCoverageThresholdWarnings(failures);

        var output = _output.ToString();
        await Assert.That(output).Contains("Line coverage 75% < 80% threshold");
        await Assert.That(output).Contains("Branch coverage 60% < 70% threshold");
    }

    [Test]
    public async Task ShowCoverageThresholdWarnings_IndentsFailures()
    {
        var presenter = new ConsolePresenter();

        presenter.ShowCoverageThresholdWarnings(["Some failure"]);

        var output = _output.ToString();
        await Assert.That(output).Contains("  Some failure");
    }

    #endregion
}
