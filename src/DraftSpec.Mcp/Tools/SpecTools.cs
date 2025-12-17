using System.ComponentModel;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Run a DraftSpec test specification and return the results.
    /// </summary>
    [McpServerTool(Name = "run_spec")]
    [Description("Execute a DraftSpec test specification and return structured results. " +
                 "Provide just the describe/it blocks - boilerplate is added automatically.")]
    public static async Task<string> RunSpec(
        SpecExecutionService executionService,
        [Description("The spec content using describe/it/expect syntax. " +
                     "Do NOT include #:package, using directives, or run() - these are added automatically.")]
        string specContent,
        [Description("Timeout in seconds (default: 10, max: 60)")]
        int timeoutSeconds = 10,
        CancellationToken cancellationToken = default)
    {
        timeoutSeconds = Math.Clamp(timeoutSeconds, 1, 60);

        var result = await executionService.ExecuteSpecAsync(
            specContent,
            TimeSpan.FromSeconds(timeoutSeconds),
            cancellationToken);

        return JsonSerializer.Serialize(result, JsonOptions);
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