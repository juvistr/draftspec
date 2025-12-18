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
        SpecExecutionService executionService,
        SessionManager sessionManager,
        McpServer server,
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
        timeoutSeconds = Math.Clamp(timeoutSeconds, 1, 60);

        // Get session if sessionId provided
        Session? session = null;
        string effectiveContent = specContent;

        if (!string.IsNullOrEmpty(sessionId))
        {
            session = sessionManager.GetSession(sessionId);
            if (session == null)
            {
                var error = new
                {
                    success = false,
                    error = $"Session '{sessionId}' not found or has expired. Create a new session with create_session."
                };
                return JsonSerializer.Serialize(error, JsonOptionsProvider.Default);
            }

            // Combine accumulated content with new content
            effectiveContent = session.GetFullContent(specContent);
        }

        // Progress callback to emit MCP notifications
        async Task OnProgress(SpecProgressNotification notification)
        {
            var progressData = new
            {
                progressToken = "spec_execution",
                progress = notification.ProgressPercent,
                total = 100.0,
                message = notification.Type switch
                {
                    "start" => $"Starting {notification.Total} specs...",
                    "progress" => $"[{notification.Completed}/{notification.Total}] {notification.Status}: {notification.Spec}",
                    "complete" => $"Completed: {notification.Passed} passed, {notification.Failed} failed",
                    _ => notification.Type
                }
            };

            await server.SendNotificationAsync(
                "notifications/progress",
                progressData,
                JsonOptionsProvider.Default,
                cancellationToken);
        }

        var result = await executionService.ExecuteSpecAsync(
            effectiveContent,
            TimeSpan.FromSeconds(timeoutSeconds),
            OnProgress,
            cancellationToken);

        // If session is active and run succeeded, accumulate the new content
        if (session != null && result.Success)
        {
            session.AppendContent(specContent);
        }

        // Add session info to result if using sessions
        if (session != null)
        {
            var sessionResult = new
            {
                result.Success,
                result.Report,
                result.ConsoleOutput,
                result.ErrorOutput,
                result.ExitCode,
                result.DurationMs,
                sessionId = session.Id,
                accumulatedContentLength = session.AccumulatedContent.Length
            };
            return JsonSerializer.Serialize(sessionResult, JsonOptionsProvider.Default);
        }

        return JsonSerializer.Serialize(result, JsonOptionsProvider.Default);
    }

    /// <summary>
    /// Run multiple spec files in a single batch for efficiency.
    /// </summary>
    [McpServerTool(Name = "run_specs_batch")]
    [Description("Execute multiple DraftSpec specifications in a single call. " +
                 "Supports parallel execution for faster test runs. " +
                 "Returns aggregated summary and individual results.")]
    public static async Task<string> RunSpecsBatch(
        SpecExecutionService executionService,
        McpServer server,
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

        timeoutSeconds = Math.Clamp(timeoutSeconds, 1, 60);
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Progress tracking for batch
        var totalSpecs = specs.Sum(s => 1); // Could estimate inner spec count if needed
        var completedSpecs = 0;

        async Task ReportBatchProgress(string specName, int index)
        {
            completedSpecs++;
            var progressData = new
            {
                progressToken = "batch_execution",
                progress = (double)completedSpecs / specs.Count * 100,
                total = 100.0,
                message = $"[{completedSpecs}/{specs.Count}] Completed: {specName}"
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
            var tasks = specs.Select(async (spec, index) =>
            {
                var result = await executionService.ExecuteSpecAsync(
                    spec.Content,
                    timeout,
                    cancellationToken);

                await ReportBatchProgress(spec.Name, index);

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
            for (var i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                var result = await executionService.ExecuteSpecAsync(
                    spec.Content,
                    timeout,
                    cancellationToken);

                await ReportBatchProgress(spec.Name, i);

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
}