#!/usr/bin/env dotnet script
// End-to-end test for DraftSpec MCP server
// Simulates MCP client sending run_spec tool call

#r "nuget: System.Text.Json, 9.0.0"

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

var mcpProjectPath = Path.GetDirectoryName(GetScriptPath());

Console.WriteLine("Starting MCP server...");

var psi = new ProcessStartInfo
{
    FileName = "dotnet",
    WorkingDirectory = mcpProjectPath,
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true
};
psi.ArgumentList.Add("run");

var process = Process.Start(psi);

// Helper to send JSON-RPC message
async Task SendMessage(object message)
{
    var json = JsonSerializer.Serialize(message);
    await process.StandardInput.WriteLineAsync(json);
    await process.StandardInput.FlushAsync();
}

// Helper to read response
async Task<JsonNode> ReadResponse()
{
    var line = await process.StandardOutput.ReadLineAsync();
    if (line == null) return null;
    return JsonNode.Parse(line);
}

try
{
    // 1. Initialize
    Console.WriteLine("\n1. Sending initialize request...");
    await SendMessage(new
    {
        jsonrpc = "2.0",
        id = 1,
        method = "initialize",
        @params = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { },
            clientInfo = new { name = "test-client", version = "1.0" }
        }
    });

    var initResponse = await ReadResponse();
    Console.WriteLine($"   Response: {initResponse?["result"]?["serverInfo"]?["name"]}");

    // 2. Send initialized notification
    await SendMessage(new
    {
        jsonrpc = "2.0",
        method = "notifications/initialized"
    });

    // 3. List tools
    Console.WriteLine("\n2. Listing available tools...");
    await SendMessage(new
    {
        jsonrpc = "2.0",
        id = 2,
        method = "tools/list"
    });

    var toolsResponse = await ReadResponse();
    var tools = toolsResponse?["result"]?["tools"]?.AsArray();
    Console.WriteLine($"   Found {tools?.Count ?? 0} tool(s):");
    if (tools != null)
    {
        foreach (var tool in tools)
        {
            Console.WriteLine($"   - {tool?["name"]}");
        }
    }

    // 4. Call run_spec with a simple test
    Console.WriteLine("\n3. Calling run_spec with a simple test...");
    var specContent = @"describe(""Calculator"", () => {
    it(""adds numbers correctly"", () => {
        expect(1 + 1).toBe(2);
    });

    it(""multiplies numbers"", () => {
        expect(3 * 4).toBe(12);
    });

    it(""handles negative numbers"", () => {
        expect(-5 + 3).toBe(-2);
    });
});";

    await SendMessage(new
    {
        jsonrpc = "2.0",
        id = 3,
        method = "tools/call",
        @params = new
        {
            name = "run_spec",
            arguments = new
            {
                specContent = specContent,
                timeoutSeconds = 30
            }
        }
    });

    Console.WriteLine("   Waiting for spec execution (this may take a moment for first run)...");

    var callResponse = await ReadResponse();
    var content = callResponse?["result"]?["content"]?[0]?["text"]?.GetValue<string>();

    if (content != null)
    {
        var result = JsonNode.Parse(content);
        Console.WriteLine("\n4. Results:");
        Console.WriteLine($"   Success: {result?["success"]}");
        Console.WriteLine($"   Exit Code: {result?["exitCode"]}");
        Console.WriteLine($"   Duration: {result?["durationMs"]:F0}ms");

        var report = result?["report"];
        if (report != null)
        {
            var summary = report["summary"];
            Console.WriteLine($"\n   Summary:");
            Console.WriteLine($"   - Total:   {summary?["total"]}");
            Console.WriteLine($"   - Passed:  {summary?["passed"]}");
            Console.WriteLine($"   - Failed:  {summary?["failed"]}");
            Console.WriteLine($"   - Pending: {summary?["pending"]}");
        }

        var consoleOutput = result?["consoleOutput"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(consoleOutput))
        {
            Console.WriteLine($"\n   Console Output:\n{consoleOutput}");
        }

        var errorOutput = result?["errorOutput"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(errorOutput))
        {
            Console.WriteLine($"\n   Error Output:\n{errorOutput}");
        }
    }
    else
    {
        Console.WriteLine($"   Raw response: {callResponse}");
    }

    Console.WriteLine("\n✓ End-to-end test complete!");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Error: {ex.Message}");

    // Show stderr if available
    var stderr = await process.StandardError.ReadToEndAsync();
    if (!string.IsNullOrWhiteSpace(stderr))
    {
        Console.WriteLine($"\nServer stderr:\n{stderr}");
    }
}
finally
{
    try { process.Kill(); } catch { }
    process.Dispose();
}

string GetScriptPath()
{
    var args = Environment.GetCommandLineArgs();
    foreach (var a in args)
    {
        if (a.EndsWith(".csx")) return a;
    }
    return Directory.GetCurrentDirectory();
}
