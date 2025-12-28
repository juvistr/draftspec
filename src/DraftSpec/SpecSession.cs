using DraftSpec.Configuration;

namespace DraftSpec;

/// <summary>
/// Encapsulates all state for a spec execution session.
/// Replaces scattered AsyncLocal fields with a single cohesive state container.
/// </summary>
/// <remarks>
/// A SpecSession tracks:
/// - The spec tree (RootContext, CurrentContext)
/// - Tags being applied to specs (CurrentTags)
/// - Execution configuration (RunnerBuilder, Configuration)
///
/// Each async execution context (CSX script, test method) gets its own session
/// via the static Dsl.Session property which uses AsyncLocal for isolation.
/// </remarks>
public class SpecSession : IDisposable
{
    /// <summary>
    /// Gets or sets the root context containing the spec tree.
    /// Created when the first describe() block is executed.
    /// </summary>
    public SpecContext? RootContext { get; set; }

    /// <summary>
    /// Gets or sets the currently active context during spec tree construction.
    /// Used by describe(), it(), before(), after() to know where to add specs/hooks.
    /// </summary>
    public SpecContext? CurrentContext { get; set; }

    /// <summary>
    /// Gets or sets the current tags in scope.
    /// Specs created within tag() blocks inherit these tags.
    /// </summary>
    public List<string>? CurrentTags { get; set; }

    /// <summary>
    /// Gets or sets the runner builder for configuring middleware.
    /// </summary>
    public SpecRunnerBuilder? RunnerBuilder { get; set; }

    /// <summary>
    /// Gets or sets the configuration for plugins, formatters, and reporters.
    /// </summary>
    public DraftSpecConfiguration? Configuration { get; set; }

    /// <summary>
    /// Resets all session state for a clean execution context.
    /// Called after run() completes or when explicitly resetting.
    /// </summary>
    public void Reset()
    {
        RootContext = null;
        CurrentContext = null;
        CurrentTags = null;
        RunnerBuilder = null;
        Configuration?.Dispose();
        Configuration = null;
    }

    /// <summary>
    /// Disposes the session by resetting all state.
    /// </summary>
    public void Dispose()
    {
        Reset();
        GC.SuppressFinalize(this);
    }
}
