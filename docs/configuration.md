# Configuration Reference

Configure DraftSpec with project files, CLI flags, and environment variables.

## Project Configuration (draftspec.json)

Create a `draftspec.json` file in your project root for persistent settings that apply to all spec runs.

### Basic Example

```json
{
  "parallel": true,
  "bail": false,
  "timeout": 10000
}
```

### Full Configuration

```json
{
  "specPattern": "**/*.spec.csx",
  "timeout": 10000,
  "parallel": true,
  "maxParallelism": 4,
  "reporters": ["console", "json"],
  "outputDirectory": "./test-results",
  "tags": {
    "include": ["unit", "fast"],
    "exclude": ["slow", "integration"]
  },
  "bail": false,
  "noCache": false,
  "format": "console"
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `specPattern` | string | `**/*.spec.csx` | Glob pattern for finding spec files |
| `timeout` | number | 30000 | Spec timeout in milliseconds |
| `parallel` | boolean | false | Run spec files in parallel |
| `maxParallelism` | number | CPU count | Maximum concurrent spec files |
| `reporters` | string[] | `["console"]` | Reporter names to use |
| `outputDirectory` | string | `.` | Directory for output files |
| `tags.include` | string[] | `[]` | Only run specs with these tags |
| `tags.exclude` | string[] | `[]` | Exclude specs with these tags |
| `bail` | boolean | false | Stop on first failure |
| `noCache` | boolean | false | Disable dotnet-script caching |
| `format` | string | `console` | Default output format |

### CLI Override Behavior

CLI options always take precedence over config file values:

```bash
# draftspec.json has parallel: false
draftspec run . --parallel  # Uses parallel: true

# draftspec.json has tags.include: ["unit"]
draftspec run . --tags integration  # Runs integration tests instead
```

### JSON Features

The configuration file supports:
- **Comments**: Both `//` and `/* */` styles
- **Trailing commas**: For easier editing
- **Case-insensitive keys**: `Parallel`, `PARALLEL`, and `parallel` all work

```json
{
  // Enable parallel execution for CI
  "parallel": true,
  "bail": true,  // Note: trailing comma allowed
}
```

---

## CLI Configuration

All configuration can be passed via CLI flags. See [CLI Reference](cli.md) for full details.

```bash
# Tag filtering
draftspec run . --tags unit,fast
draftspec run . --exclude-tags slow,integration

# Output control
draftspec run . --format json -o results.json
draftspec run . --format html -o report.html

# Execution options
draftspec run . --parallel
draftspec run . --bail
```

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DRAFTSPEC_TIMEOUT` | Spec timeout in milliseconds | 30000 |
| `DRAFTSPEC_PARALLEL` | Enable parallel execution | false |

---

## Middleware (Plugin Authors)

Middleware wraps spec execution for cross-cutting concerns. This is for plugin authors extending DraftSpec.

### ISpecMiddleware Interface

```csharp
public interface ISpecMiddleware
{
    Task<SpecResult> ExecuteAsync(
        SpecExecutionContext context,
        Func<SpecExecutionContext, Task<SpecResult>> next);
}
```

### Creating Custom Middleware

```csharp
public class LoggingMiddleware : ISpecMiddleware
{
    public async Task<SpecResult> ExecuteAsync(
        SpecExecutionContext ctx,
        Func<SpecExecutionContext, Task<SpecResult>> next)
    {
        Console.WriteLine($"Starting: {ctx.Spec.Description}");
        var sw = Stopwatch.StartNew();

        var result = await next(ctx);

        sw.Stop();
        Console.WriteLine($"Finished: {result.Status} ({sw.ElapsedMilliseconds}ms)");

        return result;
    }
}
```

### SpecExecutionContext

Available properties in middleware:

| Property | Type | Description |
|----------|------|-------------|
| `Spec` | `SpecDefinition` | The spec being executed |
| `ContextPath` | `IReadOnlyList<string>` | Path of describe blocks (e.g., ["Calculator", "Add"]) |
| `State` | `Dictionary<string, object>` | Mutable state bag for passing data between middleware |

---

## Plugins (Plugin Authors)

Plugins package reusable functionality for extending DraftSpec.

### IPlugin Interface

Base interface for all plugins:

```csharp
public interface IPlugin : IDisposable
{
    string Name { get; }
    string Version { get; }
    void Initialize(IPluginContext context);
}
```

### Plugin Types

| Interface | Purpose |
|-----------|---------|
| `IFormatterPlugin` | Register custom output formatters |
| `IReporterPlugin` | Register reporters for side effects |
| `IMiddlewarePlugin` | Register middleware in the execution pipeline |

### IFormatterPlugin

Register formatters that transform reports to specific formats:

```csharp
public class XmlFormatterPlugin : IFormatterPlugin
{
    public string Name => "XmlFormatter";
    public string Version => "1.0.0";

    public void Initialize(IPluginContext context) { }

    public void RegisterFormatters(IFormatterRegistry registry)
    {
        registry.Register("xml", new XmlFormatter());
    }

    public void Dispose() { }
}
```

### IReporterPlugin

Register reporters for side effects (notifications, file writes):

```csharp
public class SlackNotifierPlugin : IReporterPlugin
{
    private readonly string _webhookUrl;

    public string Name => "SlackNotifier";
    public string Version => "1.0.0";

    public SlackNotifierPlugin(string webhookUrl)
        => _webhookUrl = webhookUrl;

    public void Initialize(IPluginContext context) { }

    public void RegisterReporters(IReporterRegistry registry)
    {
        registry.Register(new SlackReporter(_webhookUrl));
    }

    public void Dispose() { }
}
```

### IMiddlewarePlugin

Register middleware in the execution pipeline:

```csharp
public class PerformancePlugin : IMiddlewarePlugin
{
    public string Name => "Performance";
    public string Version => "1.0.0";

    public void Initialize(IPluginContext context) { }

    public void RegisterMiddleware(SpecRunnerBuilder builder)
    {
        builder.Use(new TimingMiddleware());
        builder.Use(new MemoryTrackingMiddleware());
    }

    public void Dispose() { }
}
```

---

## Reporters (Plugin Authors)

Reporters receive events during spec execution.

### IReporter Interface

```csharp
public interface IReporter
{
    string Name { get; }

    // Called before specs start running
    Task OnRunStartingAsync(RunStartingContext context);

    // Called after each spec completes (for streaming output)
    Task OnSpecCompletedAsync(SpecResult result);

    // Called when all specs are done
    Task OnRunCompletedAsync(SpecReport report);
}
```

### Creating a Custom Reporter

```csharp
public class FileReporter : IReporter
{
    private readonly string _path;

    public string Name => "FileReporter";

    public FileReporter(string path) => _path = path;

    public Task OnRunStartingAsync(RunStartingContext context)
    {
        Console.WriteLine($"Starting {context.TotalSpecs} specs...");
        return Task.CompletedTask;
    }

    public Task OnSpecCompletedAsync(SpecResult result)
    {
        // Stream results as they complete
        return Task.CompletedTask;
    }

    public async Task OnRunCompletedAsync(SpecReport report)
    {
        var json = report.ToJson();
        await File.WriteAllTextAsync(_path, json);
    }
}
```

---

## Formatters (Plugin Authors)

Formatters transform spec reports into output formats.

### IFormatter Interface

```csharp
public interface IFormatter
{
    string Format(SpecReport report);
    string FileExtension { get; }
}
```

### Creating a Custom Formatter

```csharp
public class XmlFormatter : IFormatter
{
    public string FileExtension => ".xml";

    public string Format(SpecReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<testsuites>");

        foreach (var context in report.Contexts)
        {
            FormatContext(sb, context, 1);
        }

        sb.AppendLine("</testsuites>");
        return sb.ToString();
    }

    private void FormatContext(StringBuilder sb, SpecContextReport ctx, int indent)
    {
        var pad = new string(' ', indent * 2);
        sb.AppendLine($"{pad}<testsuite name=\"{ctx.Description}\">");

        foreach (var spec in ctx.Specs)
        {
            sb.AppendLine($"{pad}  <testcase name=\"{spec.Description}\" status=\"{spec.Status}\" />");
        }

        foreach (var child in ctx.Contexts)
        {
            FormatContext(sb, child, indent + 1);
        }

        sb.AppendLine($"{pad}</testsuite>");
    }
}
```

### Built-in Formatters

| Name | Format | Description |
|------|--------|-------------|
| `json` | JSON | Structured data for tooling |
| `markdown` | Markdown | Documentation-friendly |
| `html` | HTML | Browser-viewable report |

---

## spec_helper.csx Pattern

Centralize shared references and fixtures in `spec_helper.csx`:

```csharp
#r "nuget: DraftSpec"
#r "bin/Debug/net10.0/MyProject.dll"

using static DraftSpec.Dsl;

// Shared fixtures
public class TestDatabase
{
    public void Setup() { /* ... */ }
    public void Teardown() { /* ... */ }
}

public static TestDatabase Db = new();
```

Spec files load the helper:

```csharp
#load "spec_helper.csx"

describe("MyFeature", () => {
    before(() => Db.Setup());
    after(() => Db.Teardown());

    it("works", () => { /* ... */ });
});
```

---

## See Also

- **[DSL Reference](dsl-reference.md)** - describe/it/hooks API
- **[Assertions](assertions.md)** - expect() API
- **[CLI Reference](cli.md)** - Command-line options
