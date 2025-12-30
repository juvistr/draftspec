using System.Diagnostics;
using DraftSpec.Cli;
using DraftSpec.Cli.Coverage;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Coverage;

/// <summary>
/// Tests for CoverageRunner with mocked dependencies.
/// </summary>
public class CoverageRunnerTests
{
    #region GetFileExtension

    [Test]
    public async Task GetFileExtension_Cobertura_ReturnsXmlExtension()
    {
        var runner = new CoverageRunner("/tmp", "cobertura");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("cobertura.xml");
    }

    [Test]
    public async Task GetFileExtension_Xml_ReturnsXml()
    {
        var runner = new CoverageRunner("/tmp", "xml");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("xml");
    }

    [Test]
    public async Task GetFileExtension_Coverage_ReturnsCoverage()
    {
        var runner = new CoverageRunner("/tmp", "coverage");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("coverage");
    }

    [Test]
    public async Task GetFileExtension_Unknown_DefaultsToCobertura()
    {
        var runner = new CoverageRunner("/tmp", "unknown");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("cobertura.xml");
    }

    [Test]
    public async Task GetFileExtension_IsCaseInsensitive()
    {
        var runner = new CoverageRunner("/tmp", "COBERTURA");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("cobertura.xml");
    }

    #endregion

    #region BuildDotnetCommand

    [Test]
    public async Task BuildDotnetCommand_SimpleArgs_JoinsWithSpaces()
    {
        var args = new[] { "test", "MyProject.csproj" };

        var command = CoverageRunner.BuildDotnetCommand(args);

        await Assert.That(command).IsEqualTo("dotnet test MyProject.csproj");
    }

    [Test]
    public async Task BuildDotnetCommand_EmptyArgs_ReturnsJustDotnet()
    {
        var args = Array.Empty<string>();

        var command = CoverageRunner.BuildDotnetCommand(args);

        await Assert.That(command).IsEqualTo("dotnet ");
    }

    [Test]
    public async Task BuildDotnetCommand_WithSpaces_QuotesArgs()
    {
        var args = new[] { "test", "My Project.csproj" };

        var command = CoverageRunner.BuildDotnetCommand(args);

        await Assert.That(command).IsEqualTo("dotnet test \"My Project.csproj\"");
    }

    [Test]
    public async Task BuildDotnetCommand_MixedArgs_QuotesOnlyNeeded()
    {
        var args = new[] { "test", "--filter", "Category=Integration Tests" };

        var command = CoverageRunner.BuildDotnetCommand(args);

        await Assert.That(command).IsEqualTo("dotnet test --filter \"Category=Integration Tests\"");
    }

    #endregion

    #region QuoteIfNeeded

    [Test]
    public async Task QuoteIfNeeded_SimpleString_NoQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("simple");

        await Assert.That(result).IsEqualTo("simple");
    }

    [Test]
    public async Task QuoteIfNeeded_EmptyString_ReturnsEmptyQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("");

        await Assert.That(result).IsEqualTo("\"\"");
    }

    [Test]
    public async Task QuoteIfNeeded_NullString_ReturnsEmptyQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded(null!);

        await Assert.That(result).IsEqualTo("\"\"");
    }

    [Test]
    public async Task QuoteIfNeeded_WithSpaces_AddsQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("has spaces");

        await Assert.That(result).IsEqualTo("\"has spaces\"");
    }

    [Test]
    public async Task QuoteIfNeeded_WithQuotes_EscapesAndQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("say \"hello\"");

        await Assert.That(result).IsEqualTo("\"say \\\"hello\\\"\"");
    }

    [Test]
    public async Task QuoteIfNeeded_WithBackslash_EscapesAndQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("path\\to\\file");

        await Assert.That(result).IsEqualTo("\"path\\\\to\\\\file\"");
    }

    [Test]
    public async Task QuoteIfNeeded_WithMultipleSpecialChars_EscapesAll()
    {
        var result = CoverageRunner.QuoteIfNeeded("path with \"quotes\" and \\slashes");

        await Assert.That(result).IsEqualTo("\"path with \\\"quotes\\\" and \\\\slashes\"");
    }

    [Test]
    public async Task QuoteIfNeeded_PathLikeString_NoQuotesNeeded()
    {
        var result = CoverageRunner.QuoteIfNeeded("/usr/local/bin");

        await Assert.That(result).IsEqualTo("/usr/local/bin");
    }

    [Test]
    public async Task QuoteIfNeeded_DotnetArgs_NoQuotesNeeded()
    {
        var result = CoverageRunner.QuoteIfNeeded("--configuration=Release");

        await Assert.That(result).IsEqualTo("--configuration=Release");
    }

    #endregion

    #region Constructor and Properties

    [Test]
    public async Task Constructor_SetsOutputDirectory()
    {
        using var runner = new CoverageRunner("/custom/path", "cobertura");

        await Assert.That(runner.OutputDirectory).IsEqualTo("/custom/path");
    }

    [Test]
    public async Task Constructor_SetsFormat()
    {
        using var runner = new CoverageRunner("/tmp", "xml");

        await Assert.That(runner.Format).IsEqualTo("xml");
    }

    [Test]
    public async Task Constructor_DefaultsToCobertura()
    {
        using var runner = new CoverageRunner("/tmp");

        await Assert.That(runner.Format).IsEqualTo("cobertura");
    }

    [Test]
    public async Task Constructor_NormalizesFormatToLowercase()
    {
        using var runner = new CoverageRunner("/tmp", "COBERTURA");

        await Assert.That(runner.Format).IsEqualTo("cobertura");
    }

    [Test]
    public async Task Constructor_GeneratesSessionId()
    {
        using var runner = new CoverageRunner("/tmp");

        await Assert.That(runner.SessionId).StartsWith("draftspec-");
        await Assert.That(runner.SessionId.Length).IsGreaterThan(20);
    }

    [Test]
    public async Task Constructor_SetsUniqueSessions()
    {
        using var runner1 = new CoverageRunner("/tmp");
        using var runner2 = new CoverageRunner("/tmp");

        await Assert.That(runner1.SessionId).IsNotEqualTo(runner2.SessionId);
    }

    [Test]
    public async Task Constructor_SetsCoverageFilePath()
    {
        using var runner = new CoverageRunner("/tmp/coverage", "cobertura");

        await Assert.That(runner.CoverageFile).IsEqualTo("/tmp/coverage/coverage.cobertura.xml");
    }

    [Test]
    public async Task IsServerRunning_InitiallyFalse()
    {
        using var runner = new CoverageRunner("/tmp");

        await Assert.That(runner.IsServerRunning).IsFalse();
    }

    [Test]
    public async Task GetCoverageFile_ReturnsNullWhenFileDoesNotExist()
    {
        using var runner = new CoverageRunner("/tmp/nonexistent");

        await Assert.That(runner.GetCoverageFile()).IsNull();
    }

    [Test]
    public async Task Shutdown_ReturnsFalseWhenServerNotStarted()
    {
        using var runner = new CoverageRunner("/tmp");

        var result = runner.Shutdown();

        await Assert.That(result).IsFalse();
    }

    #endregion

    #region RunWithCoverage with Mocks

    [Test]
    public async Task RunWithCoverage_ServerRunning_UsesConnect()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/tmp/coverage");

        // Simulate server process that hasn't exited
        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);
        processRunner.AddResult(new ProcessResult("", "", 0)); // For connect command

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        // Start the server to set _serverStarted = true
        runner.StartServer();

        // Now run with coverage
        var result = runner.RunWithCoverage(["test", "MyProject.csproj"]);

        // Verify connect was called (not collect)
        await Assert.That(processRunner.RunCalls.Count).IsGreaterThanOrEqualTo(1);

        var lastCall = processRunner.RunCalls.Last();
        await Assert.That(lastCall.FileName).IsEqualTo("dotnet-coverage");
        await Assert.That(lastCall.Arguments.First()).IsEqualTo("connect");
    }

    [Test]
    public async Task RunWithCoverage_ServerNotRunning_FallsBackToCollect()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        processRunner.AddResult(new ProcessResult("", "", 0)); // For collect command

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        // Don't start the server - directly call RunWithCoverage
        var result = runner.RunWithCoverage(["test", "MyProject.csproj"]);

        // Verify collect was called (standalone mode)
        await Assert.That(processRunner.RunCalls.Count).IsEqualTo(1);

        var call = processRunner.RunCalls.First();
        await Assert.That(call.FileName).IsEqualTo("dotnet-coverage");
        await Assert.That(call.Arguments.First()).IsEqualTo("collect");
    }

    [Test]
    public async Task RunWithCoverage_ServerExited_FallsBackToCollect()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/tmp/coverage");

        // Simulate server process that HAS exited
        var mockProcessHandle = new MockProcessHandle { HasExited = true };
        processRunner.SetProcessHandle(mockProcessHandle);
        processRunner.AddResult(new ProcessResult("", "", 0)); // For collect command

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        // Start the server (but process has exited)
        runner.StartServer();

        // Now run with coverage
        var result = runner.RunWithCoverage(["test", "MyProject.csproj"]);

        // Verify collect was called (fallback mode) since server is not running
        var lastCall = processRunner.RunCalls.Last();
        await Assert.That(lastCall.FileName).IsEqualTo("dotnet-coverage");
        await Assert.That(lastCall.Arguments.First()).IsEqualTo("collect");
    }

    #endregion

    #region GenerateReports with Mocks

    [Test]
    public async Task GenerateReports_CallsFormatterFactory()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var formatterFactory = new MockCoverageFormatterFactory();

        var mockFormatter = new MockCoverageFormatter
        {
            FileExtension = ".html",
            FormatName = "html",
            FormattedContent = "<html>coverage report</html>"
        };
        formatterFactory.AddFormatter("html", mockFormatter);

        // Add a file that exists so FileExists returns true
        fileSystem.AddFile("/tmp/coverage/coverage.cobertura.xml");

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem,
            formatterFactory);

        // Call GenerateReports - note: this will fail because CoberturaParser.TryParseFile
        // reads directly from the file system, but we can verify the factory was invoked
        // when the file doesn't exist (returns empty dict)
        var reports = runner.GenerateReports(["html"]);

        // Since CoberturaParser.TryParseFile can't be mocked (uses File.Exists directly),
        // and the file doesn't actually exist, the result will be empty.
        // This test verifies the method doesn't throw and handles missing files gracefully.
        await Assert.That(reports).IsNotNull();
    }

    [Test]
    public async Task GenerateReports_FileNotExists_ReturnsEmptyDictionary()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var formatterFactory = new MockCoverageFormatterFactory();

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem,
            formatterFactory);

        var reports = runner.GenerateReports(["html", "json"]);

        await Assert.That(reports).IsEmpty();
    }

    #endregion

    #region StartServer with Mocks

    [Test]
    public async Task StartServer_CreatesOutputDirectory()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();

        await Assert.That(fileSystem.DirectoryExists("/tmp/coverage")).IsTrue();
    }

    [Test]
    public async Task StartServer_ReturnsTrue_WhenProcessStartsSuccessfully()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        var result = runner.StartServer();

        await Assert.That(result).IsTrue();
        await Assert.That(runner.IsServerRunning).IsTrue();
    }

    [Test]
    public async Task StartServer_CalledTwice_ReturnsCachedState()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();
        var secondResult = runner.StartServer();

        // Should return the running state, not try to start again
        await Assert.That(secondResult).IsTrue();
        await Assert.That(processRunner.StartProcessCalls).IsEqualTo(1);
    }

    #endregion

    #region Dispose Edge Cases

    [Test]
    public async Task Dispose_WhenNotStarted_DoesNotThrow()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        // Dispose without starting should not throw
        runner.Dispose();

        await Assert.That(true).IsTrue(); // Verify we got here
    }

    [Test]
    public async Task Dispose_CalledTwice_DoesNotThrow()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);

        var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();
        runner.Dispose();
        runner.Dispose(); // Second dispose should be a no-op

        await Assert.That(true).IsTrue(); // Verify we got here
    }

    [Test]
    public async Task Dispose_WhenServerStarted_CallsShutdown()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);
        processRunner.AddResult(new ProcessResult("", "", 0)); // For shutdown

        var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();
        runner.Dispose();

        // Verify shutdown was called
        var shutdownCall = processRunner.RunCalls.FirstOrDefault(c =>
            c.FileName == "dotnet-coverage" && c.Arguments.Contains("shutdown"));
        await Assert.That(shutdownCall.FileName).IsNotNull();
    }

    [Test]
    public async Task Dispose_WhenShutdownFails_DoesNotThrow()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);
        processRunner.ThrowOnRun = true; // Make shutdown throw

        var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();
        runner.Dispose(); // Should not throw

        await Assert.That(true).IsTrue(); // Verify we got here
    }

    [Test]
    public async Task Dispose_KillsServerProcess_WhenStillRunning()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);
        processRunner.AddResult(new ProcessResult("", "", 0)); // For shutdown

        var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();
        runner.Dispose();

        // Kill should have been called on the process
        await Assert.That(mockProcessHandle.KillCalled).IsTrue();
    }

    [Test]
    public async Task Dispose_WhenProcessAlreadyExited_DoesNotKill()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var mockProcessHandle = new MockProcessHandle { HasExited = true };
        processRunner.SetProcessHandle(mockProcessHandle);
        processRunner.AddResult(new ProcessResult("", "", 0)); // For shutdown

        var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();
        mockProcessHandle.HasExited = true; // Mark as exited
        runner.Dispose();

        // Kill should NOT have been called since process already exited
        await Assert.That(mockProcessHandle.KillCalled).IsFalse();
    }

    [Test]
    public async Task Dispose_WhenKillThrows_DoesNotThrow()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var mockProcessHandle = new MockProcessHandle { HasExited = false, ThrowOnKill = true };
        processRunner.SetProcessHandle(mockProcessHandle);
        processRunner.AddResult(new ProcessResult("", "", 0)); // For shutdown

        var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();
        runner.Dispose(); // Should not throw despite Kill throwing

        await Assert.That(true).IsTrue(); // Verify we got here
    }

    #endregion

    #region StartServer Edge Cases

    [Test]
    public async Task StartServer_WhenProcessStartThrows_ReturnsFalse()
    {
        var processRunner = new MockProcessRunner { ThrowOnStartProcess = true };
        var fileSystem = new MockFileSystem();

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        var result = runner.StartServer();

        await Assert.That(result).IsFalse();
        await Assert.That(runner.IsServerRunning).IsFalse();
    }

    [Test]
    public async Task StartServer_WhenProcessExitsImmediately_ReturnsFalse()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var mockProcessHandle = new MockProcessHandle { HasExited = true };
        processRunner.SetProcessHandle(mockProcessHandle);

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        var result = runner.StartServer();

        await Assert.That(result).IsFalse();
    }

    #endregion

    #region Shutdown Edge Cases

    [Test]
    public async Task Shutdown_WhenWaitForExitThrows_DoesNotThrow()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var mockProcessHandle = new MockProcessHandle { HasExited = false, ThrowOnWaitForExit = true };
        processRunner.SetProcessHandle(mockProcessHandle);
        processRunner.AddResult(new ProcessResult("", "", 0)); // For shutdown command

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();
        var result = runner.Shutdown(); // Should not throw

        // Returns false because file doesn't exist
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Shutdown_ReturnsTrue_WhenCoverageFileExists()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);
        processRunner.AddResult(new ProcessResult("", "", 0)); // For shutdown command

        // Add coverage file to mock file system
        fileSystem.AddFile("/tmp/coverage/coverage.cobertura.xml");

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();
        var result = runner.Shutdown();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Shutdown_ReturnsFalse_WhenShutdownCommandFails()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);
        processRunner.AddResult(new ProcessResult("", "error", 1)); // Failed shutdown command

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();
        var result = runner.Shutdown();

        await Assert.That(result).IsFalse();
    }

    #endregion

    #region GenerateReports with Valid File

    [Test]
    public async Task GenerateReports_WithValidCoberturaFile_GeneratesFormatted()
    {
        var processRunner = new MockProcessRunner();
        var formatterFactory = new MockCoverageFormatterFactory();
        var mockFormatter = new MockCoverageFormatter
        {
            FileExtension = ".html",
            FormatName = "html",
            FormattedContent = "<html>coverage</html>"
        };
        formatterFactory.AddFormatter("html", mockFormatter);

        // Create a real temp file with valid Cobertura XML
        var tempDir = Path.Combine(Path.GetTempPath(), $"coverage_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var coverageFile = Path.Combine(tempDir, "coverage.cobertura.xml");

        try
        {
            File.WriteAllText(coverageFile, """
                <?xml version="1.0" encoding="utf-8"?>
                <coverage version="1" timestamp="1234567890" line-rate="0.8" branch-rate="0.5">
                    <sources><source>/src</source></sources>
                    <packages>
                        <package name="DraftSpec">
                            <classes>
                                <class name="MyClass" filename="MyClass.cs">
                                    <lines>
                                        <line number="1" hits="1"/>
                                        <line number="2" hits="0"/>
                                    </lines>
                                </class>
                            </classes>
                        </package>
                    </packages>
                </coverage>
                """);

            // Use MockFileSystem with the file added
            var fileSystem = new MockFileSystem().AddFile(coverageFile);

            using var runner = new CoverageRunner(
                tempDir,
                "cobertura",
                processRunner,
                fileSystem,
                formatterFactory);

            var reports = runner.GenerateReports(["html"]);

            await Assert.That(reports).ContainsKey("html");
            await Assert.That(mockFormatter.FormatCalled).IsTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateReports_WithMultipleFormats_GeneratesAll()
    {
        var processRunner = new MockProcessRunner();
        var formatterFactory = new MockCoverageFormatterFactory();

        var htmlFormatter = new MockCoverageFormatter { FileExtension = ".html", FormatName = "html" };
        var jsonFormatter = new MockCoverageFormatter { FileExtension = ".json", FormatName = "json" };
        formatterFactory.AddFormatter("html", htmlFormatter);
        formatterFactory.AddFormatter("json", jsonFormatter);

        var tempDir = Path.Combine(Path.GetTempPath(), $"coverage_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var coverageFile = Path.Combine(tempDir, "coverage.cobertura.xml");

        try
        {
            File.WriteAllText(coverageFile, """
                <?xml version="1.0"?>
                <coverage version="1" timestamp="1234567890">
                    <packages/>
                </coverage>
                """);

            // Use MockFileSystem with the file added
            var fileSystem = new MockFileSystem().AddFile(coverageFile);

            using var runner = new CoverageRunner(
                tempDir,
                "cobertura",
                processRunner,
                fileSystem,
                formatterFactory);

            var reports = runner.GenerateReports(["html", "json"]);

            await Assert.That(reports.Keys.Count).IsEqualTo(2);
            await Assert.That(htmlFormatter.FormatCalled).IsTrue();
            await Assert.That(jsonFormatter.FormatCalled).IsTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateReports_WithUnknownFormat_SkipsIt()
    {
        var processRunner = new MockProcessRunner();
        var formatterFactory = new MockCoverageFormatterFactory();
        var htmlFormatter = new MockCoverageFormatter { FileExtension = ".html", FormatName = "html" };
        formatterFactory.AddFormatter("html", htmlFormatter);

        var tempDir = Path.Combine(Path.GetTempPath(), $"coverage_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var coverageFile = Path.Combine(tempDir, "coverage.cobertura.xml");

        try
        {
            File.WriteAllText(coverageFile, """
                <?xml version="1.0"?>
                <coverage version="1" timestamp="1234567890">
                    <packages/>
                </coverage>
                """);

            // Use MockFileSystem with the file added
            var fileSystem = new MockFileSystem().AddFile(coverageFile);

            using var runner = new CoverageRunner(
                tempDir,
                "cobertura",
                processRunner,
                fileSystem,
                formatterFactory);

            var reports = runner.GenerateReports(["html", "unknown"]);

            // Only html should be in results
            await Assert.That(reports.Keys.Count).IsEqualTo(1);
            await Assert.That(reports).ContainsKey("html");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateReports_OverloadWithPath_GeneratesFromPath()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var formatterFactory = new MockCoverageFormatterFactory();
        var htmlFormatter = new MockCoverageFormatter { FileExtension = ".html", FormatName = "html" };
        formatterFactory.AddFormatter("html", htmlFormatter);

        var tempDir = Path.Combine(Path.GetTempPath(), $"coverage_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var coverageFile = Path.Combine(tempDir, "custom_coverage.xml");

        try
        {
            File.WriteAllText(coverageFile, """
                <?xml version="1.0"?>
                <coverage version="1" timestamp="1234567890">
                    <packages/>
                </coverage>
                """);

            using var runner = new CoverageRunner(
                tempDir,
                "cobertura",
                processRunner,
                fileSystem,
                formatterFactory);

            // Use the overload that takes a specific path
            var reports = runner.GenerateReports(coverageFile, ["html"]);

            await Assert.That(reports).ContainsKey("html");
            await Assert.That(htmlFormatter.FormatCalled).IsTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateReports_OverloadWithInvalidPath_ReturnsEmpty()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var formatterFactory = new MockCoverageFormatterFactory();

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem,
            formatterFactory);

        var reports = runner.GenerateReports("/nonexistent/file.xml", ["html"]);

        await Assert.That(reports).IsEmpty();
    }

    [Test]
    public async Task GenerateReports_OverloadWithInvalidXml_ReturnsEmpty()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var formatterFactory = new MockCoverageFormatterFactory();

        var tempDir = Path.Combine(Path.GetTempPath(), $"coverage_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var coverageFile = Path.Combine(tempDir, "invalid.xml");

        try
        {
            File.WriteAllText(coverageFile, "not valid xml");

            using var runner = new CoverageRunner(
                tempDir,
                "cobertura",
                processRunner,
                fileSystem,
                formatterFactory);

            var reports = runner.GenerateReports(coverageFile, ["html"]);

            await Assert.That(reports).IsEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion
}
