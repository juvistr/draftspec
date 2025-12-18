using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DraftSpec.Formatters;
using DraftSpec.Mcp.Models;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Executes DraftSpec tests in-process using Roslyn scripting.
/// Provides faster execution than subprocess mode at the cost of less isolation.
/// </summary>
public class InProcessSpecRunner
{
    private readonly ILogger<InProcessSpecRunner> _logger;
    private readonly LruCache<string, Script<object>> _scriptCache;
    private readonly ScriptOptions _scriptOptions;

    /// <summary>
    /// Default maximum number of cached scripts.
    /// </summary>
    public const int DefaultCacheCapacity = 100;

    public InProcessSpecRunner(ILogger<InProcessSpecRunner> logger, int cacheCapacity = DefaultCacheCapacity)
    {
        _logger = logger;
        _scriptCache = new LruCache<string, Script<object>>(cacheCapacity);

        // Configure script options with DraftSpec references
        // Note: Static imports must be added in script code, not via WithImports
        _scriptOptions = ScriptOptions.Default
            .WithReferences(
                typeof(DraftSpec.Dsl).Assembly,
                typeof(DraftSpec.Formatters.SpecReport).Assembly,
                typeof(object).Assembly,
                typeof(Console).Assembly,
                typeof(System.Text.Json.JsonSerializer).Assembly,
                typeof(System.Collections.Generic.List<>).Assembly)
            .WithImports(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text.Json",
                "DraftSpec");
    }

    /// <summary>
    /// Execute spec content in-process and return results.
    /// </summary>
    /// <param name="specContent">The spec content to execute</param>
    /// <param name="timeout">Execution timeout</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result</returns>
    public async Task<RunSpecResult> ExecuteAsync(
        string specContent,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var contentHash = ComputeHash(specContent);

        try
        {
            // Wrap content with JSON output
            var wrappedContent = WrapSpecContent(specContent);

            // Get or compile script
            var script = GetOrCompileScript(contentHash, wrappedContent);

            // Execute with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            // Capture console output
            var originalOut = Console.Out;
            var outputCapture = new StringWriter();

            try
            {
                Console.SetOut(outputCapture);

                // Execute the script
                var result = await script.RunAsync(cancellationToken: cts.Token);

                stopwatch.Stop();

                // Parse captured output for JSON report
                var output = outputCapture.ToString();
                var report = ExtractReport(output);

                return new RunSpecResult
                {
                    Success = report?.Summary.Failed == 0,
                    ExitCode = report?.Summary.Failed == 0 ? 0 : 1,
                    Report = report,
                    ConsoleOutput = output,
                    DurationMs = stopwatch.Elapsed.TotalMilliseconds
                };
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new RunSpecResult
            {
                Success = false,
                ExitCode = -1,
                Error = new SpecError
                {
                    Category = ErrorCategory.Timeout,
                    Message = $"In-process execution timed out after {timeout.TotalSeconds}s"
                },
                ErrorOutput = $"Execution timed out after {timeout.TotalSeconds}s",
                DurationMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (CompilationErrorException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Script compilation failed");

            var errorMessage = string.Join("\n", ex.Diagnostics.Select(d => d.ToString()));
            var firstError = ex.Diagnostics.FirstOrDefault();

            return new RunSpecResult
            {
                Success = false,
                ExitCode = 1,
                Error = new SpecError
                {
                    Category = ErrorCategory.Compilation,
                    Message = firstError?.GetMessage() ?? "Compilation failed",
                    ErrorCode = firstError?.Id,
                    LineNumber = firstError?.Location.GetLineSpan().StartLinePosition.Line + 1
                },
                ErrorOutput = errorMessage,
                DurationMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "In-process spec execution failed");

            var error = ErrorParser.Parse(ex.ToString(), null, 1, timedOut: false)
                ?? new SpecError
                {
                    Category = ErrorCategory.Runtime,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                };

            return new RunSpecResult
            {
                Success = false,
                ExitCode = 1,
                Error = error,
                ErrorOutput = ex.ToString(),
                DurationMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
    }

    /// <summary>
    /// Wrap spec content for in-process execution.
    /// </summary>
    private static string WrapSpecContent(string content)
    {
        // Remove any existing run() calls
        var cleaned = SpecExecutionService.RunCallPattern().Replace(content, "// (run handled by runner)");

        return $$"""
            using static DraftSpec.Dsl;

            // Reset state for clean execution
            DraftSpec.Dsl.Reset();

            {{cleaned}}

            // Run and output JSON
            DraftSpec.Dsl.run(json: true);
            """;
    }

    /// <summary>
    /// Get cached script or compile new one.
    /// Uses LRU cache for automatic eviction of least recently used scripts.
    /// </summary>
    private Script<object> GetOrCompileScript(string contentHash, string content)
    {
        return _scriptCache.GetOrAdd(contentHash, _ =>
        {
            _logger.LogDebug("Compiling script for {Hash}", contentHash[..8]);
            var script = CSharpScript.Create(content, _scriptOptions);

            // Compile to catch errors early
            script.Compile();

            return script;
        });
    }

    /// <summary>
    /// Extract spec report from console output.
    /// </summary>
    private Models.SpecReport? ExtractReport(string output)
    {
        // Look for JSON output from Dsl.run(json: true)
        // The output should be a JSON object starting with {
        var lines = output.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("{") && trimmed.Contains("\"summary\""))
            {
                try
                {
                    return Models.SpecReport.FromJson(trimmed);
                }
                catch
                {
                    // Continue looking
                }
            }
        }

        // Try parsing the entire output as JSON
        return Models.SpecReport.FromJson(output);
    }

    /// <summary>
    /// Compute content hash for caching using HashCode (fast non-cryptographic hash).
    /// Uses .NET's built-in HashCode which internally uses a variant of xxHash.
    /// </summary>
    private static string ComputeHash(string content)
    {
        var hash = new HashCode();
        hash.AddBytes(Encoding.UTF8.GetBytes(content));
        return hash.ToHashCode().ToString("x8");
    }

    /// <summary>
    /// Clear the script cache.
    /// </summary>
    public void ClearCache()
    {
        _scriptCache.Clear();
        _logger.LogInformation("Script cache cleared");
    }

    /// <summary>
    /// Gets the current number of cached scripts.
    /// </summary>
    public int CacheCount => _scriptCache.Count;

    /// <summary>
    /// Gets the maximum cache capacity.
    /// </summary>
    public int CacheCapacity => _scriptCache.Capacity;
}
