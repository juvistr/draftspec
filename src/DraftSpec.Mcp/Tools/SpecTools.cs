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
                 "Emits progress notifications during execution for real-time feedback.")]
    public static async Task<string> RunSpec(
        SpecExecutionService executionService,
        McpServer server,
        [Description("The spec content using describe/it/expect syntax. " +
                     "Do NOT include #:package, using directives, or run() - these are added automatically.")]
        string specContent,
        [Description("Timeout in seconds (default: 10, max: 60)")]
        int timeoutSeconds = 10,
        CancellationToken cancellationToken = default)
    {
        timeoutSeconds = Math.Clamp(timeoutSeconds, 1, 60);

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
            specContent,
            TimeSpan.FromSeconds(timeoutSeconds),
            OnProgress,
            cancellationToken);

        return JsonSerializer.Serialize(result, JsonOptionsProvider.Default);
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