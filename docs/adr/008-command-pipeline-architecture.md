# ADR-008: Command Pipeline Architecture with Delegate Injection

**Status:** Accepted
**Date:** 2026-01-03
**Deciders:** DraftSpec maintainers

## Context

The CLI has 12 commands with inconsistent patterns ranging from 58 to 657 lines:

| Command | Lines | Dependencies |
|---------|-------|--------------|
| NewCommand | 58 | 2 |
| SchemaCommand | 58 | 2 |
| InitCommand | 103 | 3 |
| CacheCommand | 110 | 1 |
| EstimateCommand | 122 | 4 |
| FlakyCommand | 148 | 3 |
| ListCommand | 176 | 2 |
| DocsCommand | 215 | 2 |
| ValidateCommand | 242 | 2 |
| CoverageMapCommand | 252 | 2 |
| WatchCommand | 288 | 5 |
| RunCommand | 657 | 11 |

**Problems:**

1. **Duplicated logic** - Path resolution, spec discovery, parsing repeated across commands
2. **Inline instantiation** - `StaticSpecParser`, `DependencyGraphBuilder` created inline, preventing mocking
3. **Hard to extend** - New commands start from scratch
4. **Testing difficulty** - Commands have many dependencies, making unit testing cumbersome

**Alternatives considered:**

1. **Handler + Presenter** - Extract handler classes, but still leaves testing seam unclear
2. **Inject all phases** - Each command receives all phases as constructor parameters, but this just moves the DI complexity
3. **Aggregate Services** - Group related dependencies, but doesn't solve composability
4. **Pipeline with delegate injection** - Commands receive pre-built pipeline delegate

## Decision

Use a **unified command pipeline** with **delegate injection**:

1. **Every command is a pipeline** of composable phases
2. **Phases are middleware** - receive context, can call `next` or short-circuit
3. **DI assembles pipelines** - composition root builds pipelines using keyed services
4. **Commands receive delegate** - thin commands just map options→context and invoke

### Core Abstractions

```csharp
// Phase interface (middleware pattern, same as spec execution)
public interface ICommandPhase
{
    Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> next,
        CancellationToken ct);
}

// Single flat context - no inheritance hierarchy
// Phases communicate via Items dictionary (like HttpContext.Items)
public class CommandContext
{
    // Universal inputs (set by command)
    public required string Path { get; init; }
    public required IConsole Console { get; init; }
    public required IFileSystem FileSystem { get; init; }

    // Phase-to-phase communication
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    // Convenience accessors
    public T? Get<T>(string key) => Items.TryGetValue(key, out var v) ? (T?)v : default;
    public void Set<T>(string key, T value) => Items[key] = value;
}

// Well-known keys for phase communication
public static class ContextKeys
{
    public const string ProjectPath = nameof(ProjectPath);
    public const string SpecFiles = nameof(SpecFiles);
    public const string ParsedSpecs = nameof(ParsedSpecs);
    public const string Quarantine = nameof(Quarantine);
    public const string Filter = nameof(Filter);
    public const string RunResults = nameof(RunResults);
}

// Builder assembles phases into delegate
public class CommandPipelineBuilder
{
    public CommandPipelineBuilder Use(ICommandPhase phase);
    public CommandPipelineBuilder UseWhen(Func<CommandContext, bool> predicate, ICommandPhase phase);
    public Func<CommandContext, CancellationToken, Task<int>> Build();
}
```

**Design choice: Composition over inheritance**

We use a single `CommandContext` with an Items dictionary rather than a type hierarchy (e.g., `RunContext : CommandContext`). This avoids fragile base class problems and keeps all phases using the same non-generic interface.

### DI Registration (Keyed Services)

```csharp
// Composition root assembles pipelines
services.AddKeyedScoped<Func<CommandContext, CancellationToken, Task<int>>>(
    "list",
    (sp, _) => new CommandPipelineBuilder()
        .Use(sp.GetRequiredService<PathResolutionPhase>())
        .Use(sp.GetRequiredService<SpecDiscoveryPhase>())
        .Use(sp.GetRequiredService<SpecParsingPhase>())
        .Use(sp.GetRequiredService<ListOutputPhase>())
        .Build());

// Run command with conditional phases
services.AddKeyedScoped<Func<CommandContext, CancellationToken, Task<int>>>(
    "run",
    (sp, _) => new CommandPipelineBuilder()
        .Use(sp.GetRequiredService<PathResolutionPhase>())
        .Use(sp.GetRequiredService<SpecDiscoveryPhase>())
        .UseWhen(ctx => ctx.Get<bool>(ContextKeys.Quarantine),
                 sp.GetRequiredService<QuarantinePhase>())
        .UseWhen(ctx => ctx.Get<string?>(ContextKeys.AffectedBy) != null,
                 sp.GetRequiredService<ImpactAnalysisPhase>())
        .Use(sp.GetRequiredService<SpecExecutionPhase>())
        .Use(sp.GetRequiredService<RunOutputPhase>())
        .Build());
```

### Command Implementation

```csharp
public class ListCommand : ICommand<ListOptions>
{
    private readonly Func<CommandContext, CancellationToken, Task<int>> _pipeline;

    public ListCommand(
        [FromKeyedServices("list")] Func<CommandContext, CancellationToken, Task<int>> pipeline,
        IConsole console,
        IFileSystem fileSystem)
    {
        _pipeline = pipeline;
        // ...
    }

    public Task<int> ExecuteAsync(ListOptions options, CancellationToken ct)
    {
        var context = MapOptionsToContext(options);
        return _pipeline(context, ct);
    }
}
```

### Testing Strategy

The delegate boundary is the testing seam:

**1. Command Unit Tests** - Mock the delegate, verify options→context mapping:
```csharp
[Test]
public async Task ListCommand_MapsOptionsToContext_Correctly()
{
    CommandContext? captured = null;
    Func<CommandContext, CancellationToken, Task<int>> mock =
        (ctx, _) => { captured = ctx; return Task.FromResult(0); };

    var command = new ListCommand(mock, new MockConsole(), new MockFileSystem());
    await command.ExecuteAsync(new ListOptions { Path = "/specs" }, ct);

    await Assert.That(captured!.Path).IsEqualTo("/specs");
}
```

**2. Phase Unit Tests** - Test individual phase behavior in isolation:
```csharp
[Test]
public async Task SpecDiscoveryPhase_NoSpecFiles_ShortCircuits()
{
    var phase = new SpecDiscoveryPhase(new MockSpecFinder().WithSpecs([]));
    var nextCalled = false;

    await phase.ExecuteAsync(context, (_, _) => {
        nextCalled = true;
        return Task.FromResult(0);
    }, ct);

    await Assert.That(nextCalled).IsFalse();
}
```

**3. Integration Tests** - Use real DI container with mocked externals:
```csharp
[Test]
public async Task ListCommand_Integration_WithSpecs_OutputsTree()
{
    var services = new ServiceCollection();
    services.AddSingleton<IFileSystem>(new MockFileSystem().AddFile("/specs/test.spec.csx", "..."));
    services.AddCommandPipelines();  // Real phases

    var command = services.BuildServiceProvider().GetRequiredService<ListCommand>();
    await command.ExecuteAsync(new ListOptions { Path = "/specs" }, ct);
}
```

## Consequences

### Positive

- **Unified pattern** - All 12 commands follow the same architecture
- **Composable phases** - Reuse PathResolutionPhase, SpecDiscoveryPhase, etc.
- **Clean testing seam** - Mock one delegate instead of N dependencies
- **Explicit pipeline** - Registration shows exact phase order
- **Short-circuit support** - Phases can return early (no specs found → exit 0)
- **Thin commands** - Commands become ~30 lines (options mapping only)
- **Same DI as production** - Integration tests use real container

### Negative

- **More files** - Each phase is a separate class
- **Pipeline debugging** - Harder to step through than linear code
- **Context mutations** - Phases mutate shared context (explicit via contracts)

### Neutral

- **Keyed services** - Requires .NET 8+ (already using .NET 10)
- **Learning curve** - New pattern for contributors (but matches existing spec middleware)

## Reusable Phases

| Phase | Responsibility | Commands |
|-------|----------------|----------|
| `PathResolutionPhase` | Resolve path, validate, set `ProjectPath` | All path-based |
| `SpecDiscoveryPhase` | Find spec files, set `SpecFiles` | Run, Watch, List, Validate, Docs, CoverageMap |
| `SpecParsingPhase` | Parse specs, set `ParsedSpecs` | List, Validate, Docs, CoverageMap, Run |
| `HistoryLoadPhase` | Load spec history | Run, Estimate, Flaky |
| `FilterApplyPhase` | Apply name/tag/context filters | List, Docs, Validate |

## Phase Contracts

Each phase documents what Items keys it requires and produces:

```csharp
/// <summary>
/// Discovers spec files at the resolved path.
///
/// Requires: Items[ProjectPath] is set
/// Produces: Items[SpecFiles]
/// Short-circuits: If no spec files found (returns 0)
/// </summary>
public class SpecDiscoveryPhase : ICommandPhase { ... }
```

## Implementation Plan

See milestone v0.8.1 for tracking. Implementation in 10 PRs:

1. Pipeline infrastructure (interfaces, builder, ContextKeys)
2. Common phases + DI registration
3. ListCommand migration
4. ValidateCommand + DocsCommand
5. CoverageMapCommand
6. History commands (Estimate, Flaky)
7. Simple commands (New, Schema, Init, Cache)
8. Run phases (Part 1: Quarantine, LineFilter, ImpactAnalysis)
9. Run phases (Part 2: Interactive, Partition, Execution, Output)
10. RunCommand + WatchCommand migration

## References

- [ADR-003: Middleware Pipeline Pattern](./003-middleware-pipeline.md) - Same pattern for spec execution
- `src/DraftSpec/Middleware/ISpecMiddleware.cs` - Existing middleware interface
