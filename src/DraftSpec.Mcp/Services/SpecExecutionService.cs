using System.Diagnostics;
using System.Text.RegularExpressions;
using DraftSpec.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Executes DraftSpec tests using .NET 10 file-based apps.
/// </summary>
public partial class SpecExecutionService
{
    private readonly TempFileManager _tempFileManager;
    private readonly ILogger<SpecExecutionService> _logger;

    /// <summary>
    /// Template for wrapping user spec content.
    /// Uses .NET 10 file-based app with #:package directive.
    /// </summary>
    private const string SpecTemplate = """
                                        #:package DraftSpec@*
                                        #:property JsonSerializerIsReflectionEnabledByDefault=true
                                        using static DraftSpec.Dsl;

                                        {0}

                                        run(json: true);
                                        """;

    public SpecExecutionService(
        TempFileManager tempFileManager,
        ILogger<SpecExecutionService> logger)
    {
        _tempFileManager = tempFileManager;
        _logger = logger;
    }

    /// <summary>
    /// Execute spec content and return structured results.
    /// </summary>
    public async Task<RunSpecResult> ExecuteSpecAsync(
        string specContent,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        string? specFilePath = null;
        string? jsonOutputPath = null;

        try
        {
            var wrappedContent = WrapSpecContent(specContent);
            specFilePath = await _tempFileManager.CreateTempSpecFileAsync(wrappedContent, cancellationToken);
            jsonOutputPath = _tempFileManager.CreateTempJsonOutputPath();

            _logger.LogDebug("Executing spec at {Path}", specFilePath);

            var (exitCode, stdout, stderr) = await RunDotnetAsync(
                specFilePath,
                jsonOutputPath,
                timeout,
                cancellationToken);

            stopwatch.Stop();

            SpecReport? report = null;
            if (File.Exists(jsonOutputPath))
                try
                {
                    var json = await File.ReadAllTextAsync(jsonOutputPath, cancellationToken);
                    report = SpecReport.FromJson(json);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse JSON output");
                }

            return new RunSpecResult
            {
                Success = exitCode == 0 && report != null,
                ExitCode = exitCode,
                Report = report,
                ConsoleOutput = stdout,
                ErrorOutput = stderr,
                DurationMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (OperationCanceledException)
        {
            return new RunSpecResult
            {
                Success = false,
                ExitCode = -1,
                ErrorOutput = "Execution was cancelled",
                DurationMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (TimeoutException)
        {
            return new RunSpecResult
            {
                Success = false,
                ExitCode = -1,
                ErrorOutput = $"Execution timed out after {timeout.TotalSeconds}s",
                DurationMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing spec");
            return new RunSpecResult
            {
                Success = false,
                ExitCode = -1,
                ErrorOutput = $"Execution failed: {ex.Message}",
                DurationMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        finally
        {
            _tempFileManager.Cleanup(specFilePath, jsonOutputPath);
        }
    }

    [GeneratedRegex(@"run\s*\([^)]*\)\s*;")]
    internal static partial Regex RunCallPattern();

    /// <summary>
    /// Wraps user spec content with boilerplate (package directive, usings, run call).
    /// Internal for testability.
    /// </summary>
    internal static string WrapSpecContent(string content)
    {
        // Remove any existing boilerplate the user might have included
        var cleaned = content
            .Replace("#:package DraftSpec", "// (package directive handled by server)")
            .Replace("using static DraftSpec.Dsl;", "// (using directive handled by server)");

        // Remove run() calls with any arguments: run(), run(json:true), run(json: true), etc.
        cleaned = RunCallPattern().Replace(cleaned, "// (run handled by server)");

        return string.Format(SpecTemplate, cleaned);
    }

    private async Task<(int ExitCode, string Stdout, string Stderr)> RunDotnetAsync(
        string specFilePath,
        string jsonOutputPath,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            // Use temp directory as working dir so NuGet.config is found
            WorkingDirectory = _tempFileManager.TempDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Use ArgumentList for secure argument passing (no shell interpretation)
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add(specFilePath);

        // Set environment variable for JSON output file
        psi.EnvironmentVariables["DRAFTSPEC_JSON_OUTPUT_FILE"] = jsonOutputPath;

        using var process = new Process { StartInfo = psi };
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

        try
        {
            await process.WaitForExitAsync(cts.Token);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            return (process.ExitCode, stdout, stderr);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested &&
                                                 !cancellationToken.IsCancellationRequested)
        {
            try
            {
                process.Kill(true);
            }
            catch
            {
            }

            throw new TimeoutException($"Process timed out after {timeout.TotalSeconds}s");
        }
    }
}