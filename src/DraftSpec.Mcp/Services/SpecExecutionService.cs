using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using DraftSpec.Mcp.Models;
using DraftSpec.Formatters;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Executes DraftSpec tests using .NET 10 file-based apps.
/// </summary>
public partial class SpecExecutionService
{
    private readonly TempFileManager _tempFileManager;
    private readonly IAsyncProcessRunner _processRunner;
    private readonly ILogger<SpecExecutionService> _logger;

    /// <summary>
    /// Prefix used by ProgressStreamReporter for progress lines.
    /// </summary>
    private const string ProgressLinePrefix = "DRAFTSPEC_PROGRESS:";

    /// <summary>
    /// Template for wrapping user spec content.
    /// Uses .NET 10 file-based app with #:package directive.
    /// </summary>
    private const string SpecTemplate = """
                                        #:package DraftSpec@*
                                        #:property JsonSerializerIsReflectionEnabledByDefault=true
                                        using static DraftSpec.Dsl;

                                        {0}

                                        // Execute specs and output JSON
                                        if (RootContext != null)
                                        {{
                                            var runner = new DraftSpec.SpecRunner();
                                            var results = runner.Run(RootContext);
                                            var report = DraftSpec.SpecReportBuilder.Build(RootContext, results);
                                            Console.WriteLine(report.ToJson());
                                        }}
                                        """;

    public SpecExecutionService(
        TempFileManager tempFileManager,
        IAsyncProcessRunner processRunner,
        ILogger<SpecExecutionService> logger)
    {
        _tempFileManager = tempFileManager;
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <summary>
    /// Execute spec content and return structured results.
    /// </summary>
    public Task<RunSpecResult> ExecuteSpecAsync(
        string specContent,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        return ExecuteSpecAsync(specContent, timeout, null, cancellationToken);
    }

    /// <summary>
    /// Execute spec content and return structured results with progress notifications.
    /// </summary>
    /// <param name="specContent">The spec content to execute</param>
    /// <param name="timeout">Execution timeout</param>
    /// <param name="onProgress">Optional callback for progress notifications</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<RunSpecResult> ExecuteSpecAsync(
        string specContent,
        TimeSpan timeout,
        Func<SpecProgressNotification, Task>? onProgress,
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
                onProgress,
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

            var success = exitCode == 0 && report != null;
            var error = success ? null : ErrorParser.Parse(stderr, stdout, exitCode, timedOut: false);

            return new RunSpecResult
            {
                Success = success,
                ExitCode = exitCode,
                Report = report,
                Error = error,
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
                Error = new SpecError
                {
                    Category = ErrorCategory.Runtime,
                    Message = "Execution was cancelled"
                },
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
                Error = new SpecError
                {
                    Category = ErrorCategory.Timeout,
                    Message = $"Execution timed out after {timeout.TotalSeconds}s"
                },
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
                Error = new SpecError
                {
                    Category = ErrorCategory.Runtime,
                    Message = $"Execution failed: {ex.Message}",
                    StackTrace = ex.StackTrace
                },
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
        Func<SpecProgressNotification, Task>? onProgress,
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

        // Enable progress streaming if callback is provided
        if (onProgress != null)
        {
            psi.EnvironmentVariables["DRAFTSPEC_PROGRESS_STREAM"] = "true";
        }

        await using var process = await _processRunner.StartAsync(psi, cancellationToken);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        // Read stdout with progress line handling
        var stdoutTask = onProgress != null
            ? ReadStdoutWithProgressAsync(process.StandardOutput, onProgress, cts.Token)
            : process.StandardOutput.ReadToEndAsync(cts.Token);
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

    /// <summary>
    /// Reads stdout line by line, parsing progress lines and invoking the callback.
    /// Non-progress lines are collected and returned as the stdout content.
    /// </summary>
    private async Task<string> ReadStdoutWithProgressAsync(
        StreamReader reader,
        Func<SpecProgressNotification, Task> onProgress,
        CancellationToken cancellationToken)
    {
        var outputLines = new List<string>();

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith(ProgressLinePrefix, StringComparison.Ordinal))
            {
                // Parse and emit progress notification
                var json = line[ProgressLinePrefix.Length..];
                try
                {
                    var notification = JsonSerializer.Deserialize<SpecProgressNotification>(json, JsonOptionsProvider.Default);
                    if (notification != null)
                    {
                        await onProgress(notification);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse progress line: {Line}", line);
                }
            }
            else
            {
                // Regular output line
                outputLines.Add(line);
            }
        }

        return string.Join(Environment.NewLine, outputLines);
    }
}
