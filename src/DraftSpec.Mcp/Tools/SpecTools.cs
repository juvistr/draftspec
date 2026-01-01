using System.ComponentModel;
using System.Text.Json;
using DraftSpec.Formatters;
using ModelContextProtocol.Server;
using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Services;

namespace DraftSpec.Mcp.Tools;

/// <summary>
/// MCP tools for running DraftSpec tests.
/// </summary>
[McpServerToolType]
public static class SpecTools
{
    /// <summary>
    /// Run a DraftSpec test specification and return the results.
    /// </summary>
    [McpServerTool(Name = "run_spec")]
    [Description("Execute a DraftSpec test specification and return structured results. " +
                 "Provide just the describe/it blocks - boilerplate is added automatically. " +
                 "Emits progress notifications during execution for real-time feedback. " +
                 "Use session_id to accumulate specs across multiple calls for iterative development.")]
    public static async Task<string> RunSpec(
        ISpecExecutionService executionService,
        SessionManager sessionManager,
        McpServer server,
        McpOptions options,
        [Description("The spec content using describe/it/expect syntax. " +
                     "Do NOT include #:package, using directives, or run() - these are added automatically.")]
        string specContent,
        [Description("Optional session ID for accumulating specs. When provided, previous specs from " +
                     "this session are prepended to the current content, enabling iterative development.")]
        string? sessionId = null,
        [Description("Timeout in seconds (default: 10, max: 60)")]
        int timeoutSeconds = 10,
        CancellationToken cancellationToken = default)
    {
        // Validate content
        if (string.IsNullOrWhiteSpace(specContent))
        {
            var errorResult = new RunSpecResult
            {
                Success = false,
                Error = new SpecError
                {
                    Category = ErrorCategory.Validation,
                    Message = "Spec content cannot be null or empty"
                }
            };
            return JsonSerializer.Serialize(errorResult, JsonOptionsProvider.Default);
        }

        if (specContent.Length > options.MaxSpecContentSizeBytes)
        {
            var errorResult = new RunSpecResult
            {
                Success = false,
                Error = new SpecError
                {
                    Category = ErrorCategory.Validation,
                    Message = $"Spec content exceeds maximum size limit of {options.MaxSpecContentSizeBytes:N0} bytes"
                }
            };
            return JsonSerializer.Serialize(errorResult, JsonOptionsProvider.Default);
        }

        var orchestrator = new SpecRunOrchestrator(sessionManager);

        // Set up progress callback (null server check for testing)
        Func<SpecProgressNotification, Task>? onProgress = null;
        if (server != null)
        {
            onProgress = async notification =>
            {
                var progressData = new
                {
                    progressToken = "spec_execution",
                    progress = notification.ProgressPercent,
                    total = 100.0,
                    message = FormatProgressMessage(notification)
                };

                await server.SendNotificationAsync(
                    "notifications/progress",
                    progressData,
                    JsonOptionsProvider.Default,
                    cancellationToken);
            };
        }

        var executor = new SubprocessSpecExecutor(executionService, onProgress, cancellationToken);

        var result = await orchestrator.RunAsync(
            executor,
            specContent,
            sessionId,
            timeoutSeconds,
            cancellationToken);

        return JsonSerializer.Serialize(result.ToResponse(), JsonOptionsProvider.Default);
    }

    /// <summary>
    /// Run multiple spec files in a single batch for efficiency.
    /// </summary>
    [McpServerTool(Name = "run_specs_batch")]
    [Description("Execute multiple DraftSpec specifications in a single call. " +
                 "Supports parallel execution for faster test runs. " +
                 "Returns aggregated summary and individual results.")]
    public static async Task<string> RunSpecsBatch(
        ISpecExecutionService executionService,
        McpServer server,
        McpOptions options,
        [Description("Array of specs to execute, each with a name and content")]
        List<BatchSpecInput> specs,
        [Description("Run specs in parallel (default: true)")]
        bool parallel = true,
        [Description("Timeout per spec in seconds (default: 10, max: 60)")]
        int timeoutSeconds = 10,
        CancellationToken cancellationToken = default)
    {
        if (specs == null || specs.Count == 0)
        {
            return JsonSerializer.Serialize(new BatchSpecResult
            {
                AllPassed = true,
                TotalSpecs = 0,
                PassedSpecs = 0,
                FailedSpecs = 0,
                TotalDurationMs = 0,
                Results = []
            }, JsonOptionsProvider.Default);
        }

        // Validate content size for each spec
        foreach (var spec in specs)
        {
            if (spec.Content != null && spec.Content.Length > options.MaxSpecContentSizeBytes)
            {
                return JsonSerializer.Serialize(new BatchSpecResult
                {
                    AllPassed = false,
                    TotalSpecs = specs.Count,
                    PassedSpecs = 0,
                    FailedSpecs = specs.Count,
                    TotalDurationMs = 0,
                    Results = [],
                    Error = $"Spec '{spec.Name}' exceeds maximum content size limit of {options.MaxSpecContentSizeBytes:N0} bytes"
                }, JsonOptionsProvider.Default);
            }
        }

        timeoutSeconds = Math.Clamp(timeoutSeconds, 1, 60);
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Progress tracking for batch
        var completedSpecs = 0;

        async Task ReportBatchProgress(string specName)
        {
            completedSpecs++;

            if (server == null)
            {
                return;
            }

            var progressData = new
            {
                progressToken = "batch_execution",
                progress = (double)completedSpecs / specs.Count * 100,
                total = 100.0,
                message = FormatBatchProgressMessage(completedSpecs, specs.Count, specName)
            };

            await server.SendNotificationAsync(
                "notifications/progress",
                progressData,
                JsonOptionsProvider.Default,
                cancellationToken);
        }

        List<NamedSpecResult> results;

        if (parallel)
        {
            // Execute all specs in parallel
            var tasks = specs.Select(async spec =>
            {
                var result = await executionService.ExecuteSpecAsync(spec.Content, timeout, cancellationToken);

                await ReportBatchProgress(spec.Name);

                return new NamedSpecResult
                {
                    Name = spec.Name,
                    Success = result.Success,
                    ExitCode = result.ExitCode,
                    Report = result.Report,
                    Error = result.Error,
                    DurationMs = result.DurationMs
                };
            });

            results = (await Task.WhenAll(tasks)).ToList();
        }
        else
        {
            // Execute specs sequentially
            results = [];
            foreach (var spec in specs)
            {
                var result = await executionService.ExecuteSpecAsync(spec.Content, timeout, cancellationToken);

                await ReportBatchProgress(spec.Name);

                results.Add(new NamedSpecResult
                {
                    Name = spec.Name,
                    Success = result.Success,
                    ExitCode = result.ExitCode,
                    Report = result.Report,
                    Error = result.Error,
                    DurationMs = result.DurationMs
                });
            }
        }

        stopwatch.Stop();

        var passedCount = results.Count(r => r.Success);
        var failedCount = results.Count - passedCount;

        var batchResult = new BatchSpecResult
        {
            AllPassed = failedCount == 0,
            TotalSpecs = results.Count,
            PassedSpecs = passedCount,
            FailedSpecs = failedCount,
            TotalDurationMs = stopwatch.Elapsed.TotalMilliseconds,
            Results = results
        };

        return JsonSerializer.Serialize(batchResult, JsonOptionsProvider.Default);
    }

    /// <summary>
    /// Generate DraftSpec code from a structured description.
    /// </summary>
    [McpServerTool(Name = "scaffold_specs")]
    [Description("Generate DraftSpec code from a structured description. " +
                 "Outputs pending specs ready for the agent to fill in assertions.")]
    public static string ScaffoldSpecs(
        [Description("Recursive structure of describe blocks and specs")]
        ScaffoldNode structure)
    {
        return Scaffolder.Generate(structure);
    }

    /// <summary>
    /// Formats a progress notification into a human-readable message.
    /// Internal for testing.
    /// </summary>
    internal static string FormatProgressMessage(SpecProgressNotification notification)
    {
        return notification.Type switch
        {
            "start" => $"Starting {notification.Total} specs...",
            "progress" => $"[{notification.Completed}/{notification.Total}] {notification.Status}: {notification.Spec}",
            "complete" => $"Completed: {notification.Passed} passed, {notification.Failed} failed",
            _ => notification.Type
        };
    }

    /// <summary>
    /// Formats a batch progress message.
    /// Internal for testing.
    /// </summary>
    internal static string FormatBatchProgressMessage(int completed, int total, string specName)
    {
        return $"[{completed}/{total}] Completed: {specName}";
    }
}
