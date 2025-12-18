using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DraftSpec.Mcp.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr (stdout is reserved for MCP protocol)
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => { options.LogToStandardErrorThreshold = LogLevel.Trace; });

// Register services
builder.Services.AddSingleton<TempFileManager>();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<SpecExecutionService>();
builder.Services.AddSingleton<InProcessSpecRunner>();

// Configure MCP server with stdio transport
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly();

var host = builder.Build();

// Log security warnings at startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogWarning("DraftSpec MCP Server starting - SECURITY WARNING:");
logger.LogWarning("  This server executes arbitrary C# code with full process privileges.");
logger.LogWarning("  Only connect from trusted AI assistants in trusted environments.");
logger.LogWarning("  Do NOT expose this server to untrusted networks or users.");
logger.LogWarning("  See SECURITY.md for deployment guidance.");

await host.RunAsync();

/// <summary>Marker class for logging.</summary>
internal partial class Program { }