# ADR-004: Plugin Architecture

**Status:** Accepted
**Date:** 2025-12-15
**Deciders:** DraftSpec maintainers

## Context

DraftSpec needs extensibility points for:

- **Formatters** - Custom output formats (XML, TAP, JUnit)
- **Reporters** - Custom side effects (Slack notifications, database logging)
- **Middleware** - Custom cross-cutting concerns (tracing, metrics)

**Requirements:**

1. Self-contained - plugins bundle related functionality
2. Discoverable - easy to find and install
3. Initializable - plugins can perform setup
4. Composable - multiple plugins work together
5. Simple - low barrier to create plugins

**Alternatives considered:**

1. **Convention-based** - Magic naming/folders; implicit, hard to debug
2. **Reflection-based** - Scan for types; slow, fragile
3. **Interface-based** - Explicit registration; clear, testable

## Decision

Use an **interface-based plugin system** with explicit registration:

```csharp
public interface IPlugin : IDisposable
{
    string Name { get; }
    string Version { get; }
    void Initialize(IPluginContext context);
}

public interface IFormatterPlugin : IPlugin
{
    void RegisterFormatters(IFormatterRegistry registry);
}

public interface IReporterPlugin : IPlugin
{
    void RegisterReporters(IReporterRegistry registry);
}

public interface IMiddlewarePlugin : IPlugin
{
    void RegisterMiddleware(SpecRunnerBuilder builder);
}
```

**Key design choices:**

1. **Separate interfaces** - Plugins implement only what they provide
2. **Explicit registration** - Configuration calls `UsePlugin<T>()`
3. **Context injection** - Plugins receive services via `IPluginContext`
4. **Lifecycle management** - Plugins are `IDisposable` for cleanup

**Usage:**

```csharp
var config = new DraftSpecConfiguration()
    .UsePlugin<SlackReporterPlugin>()
    .UsePlugin<XmlFormatterPlugin>();

// Direct registration also supported
config.AddReporter(new CustomReporter());
config.AddFormatter("custom", new CustomFormatter());
```

## Consequences

### Positive

- **Type-safe** - Compile-time checking of plugin interfaces
- **Testable** - Plugins are regular classes with interfaces
- **Explicit** - No magic; clear what plugins do
- **Composable** - Mix plugins and direct registration

### Negative

- **Ceremony** - Creating a plugin requires implementing interface
- **No discovery** - Users must know plugin exists and register it
- **Initialization order** - Plugins initialized in registration order

### Neutral

- **No dynamic loading** - Plugins must be compiled with project
- **No versioning** - Version property informational only

## Implementation Notes

**IPluginContext** provides:

- `GetService<T>()` - Access registered services
- `GetRequiredService<T>()` - Throws if not found
- `Log()` - Structured logging

**Initialization sequence:**

1. Plugins registered via `UsePlugin<T>()`
2. `Initialize()` called on each plugin (registration order)
3. Specialized registration methods called (`RegisterFormatters`, etc.)
4. Runner builds with configured middleware

**Registry interfaces:**

```csharp
public interface IFormatterRegistry
{
    void Register(string name, IFormatter formatter);
    IFormatter? Get(string name);
}

public interface IReporterRegistry
{
    void Register(IReporter reporter);
    IEnumerable<IReporter> All { get; }
}
```
