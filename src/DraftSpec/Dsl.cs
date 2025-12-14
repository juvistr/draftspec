using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Console;

namespace DraftSpec;

/// <summary>
/// Static DSL for script-based specs (.csx files).
/// Usage: using static DraftSpec.Dsl;
/// </summary>
public static class Dsl
{
    private static readonly AsyncLocal<SpecContext?> CurrentContextLocal = new();
    private static readonly AsyncLocal<SpecContext?> RootContextLocal = new();

    private static SpecContext? CurrentContext
    {
        get => CurrentContextLocal.Value;
        set => CurrentContextLocal.Value = value;
    }

    private static SpecContext? RootContext
    {
        get => RootContextLocal.Value;
        set => RootContextLocal.Value = value;
    }

    /// <summary>
    /// Define a spec group. Creates root context on first call.
    /// </summary>
    public static void describe(string description, Action body)
    {
        if (RootContext is null)
        {
            // First describe call - create root
            RootContext = new SpecContext(description);
            CurrentContext = RootContext;
            try
            {
                body();
            }
            finally
            {
                CurrentContext = null;
            }
        }
        else if (CurrentContext is null)
        {
            // Another top-level describe - add as child of root
            var context = new SpecContext(description, RootContext);
            CurrentContext = context;
            try
            {
                body();
            }
            finally
            {
                CurrentContext = null;
            }
        }
        else
        {
            // Nested describe
            var parent = CurrentContext;
            var context = new SpecContext(description, parent);
            CurrentContext = context;
            try
            {
                body();
            }
            finally
            {
                CurrentContext = parent;
            }
        }
    }

    /// <summary>
    /// Alias for describe - used for sub-groupings.
    /// </summary>
    public static void context(string description, Action body) => describe(description, body);

    /// <summary>
    /// Define a spec with implementation.
    /// </summary>
    public static void it(string description, Action body)
    {
        EnsureContext();
        CurrentContext!.AddSpec(new SpecDefinition(description, body));
    }

    /// <summary>
    /// Define a pending spec (no implementation yet).
    /// </summary>
    public static void it(string description)
    {
        EnsureContext();
        CurrentContext!.AddSpec(new SpecDefinition(description));
    }

    /// <summary>
    /// Define a focused spec - only focused specs run when any exist.
    /// </summary>
    public static void fit(string description, Action body)
    {
        EnsureContext();
        CurrentContext!.AddSpec(new SpecDefinition(description, body) { IsFocused = true });
    }

    /// <summary>
    /// Define a skipped spec.
    /// </summary>
    public static void xit(string description, Action? body = null)
    {
        EnsureContext();
        CurrentContext!.AddSpec(new SpecDefinition(description, body) { IsSkipped = true });
    }

    /// <summary>
    /// Register a beforeEach hook for the current context.
    /// </summary>
    public static void before(Action hook)
    {
        EnsureContext();
        CurrentContext!.BeforeEach = hook;
    }

    /// <summary>
    /// Register an afterEach hook for the current context.
    /// </summary>
    public static void after(Action hook)
    {
        EnsureContext();
        CurrentContext!.AfterEach = hook;
    }

    /// <summary>
    /// Register a beforeAll hook for the current context.
    /// </summary>
    public static void beforeAll(Action hook)
    {
        EnsureContext();
        CurrentContext!.BeforeAll = hook;
    }

    /// <summary>
    /// Register an afterAll hook for the current context.
    /// </summary>
    public static void afterAll(Action hook)
    {
        EnsureContext();
        CurrentContext!.AfterAll = hook;
    }

    // ===== Assertions =====

    /// <summary>
    /// Create an expectation for a value.
    /// </summary>
    public static Expectation<T> expect<T>(
        T actual,
        [CallerArgumentExpression("actual")] string? expr = null)
        => new Expectation<T>(actual, expr);

    /// <summary>
    /// Create an expectation for a boolean value.
    /// </summary>
    public static BoolExpectation expect(
        bool actual,
        [CallerArgumentExpression("actual")] string? expr = null)
        => new BoolExpectation(actual, expr);

    /// <summary>
    /// Create an expectation for a string value.
    /// </summary>
    public static StringExpectation expect(
        string? actual,
        [CallerArgumentExpression("actual")] string? expr = null)
        => new StringExpectation(actual, expr);

    /// <summary>
    /// Create an expectation for an action (exception testing).
    /// </summary>
    public static ActionExpectation expect(
        Action action,
        [CallerArgumentExpression("action")] string? expr = null)
        => new ActionExpectation(action, expr);

    /// <summary>
    /// Create an expectation for an array.
    /// </summary>
    public static CollectionExpectation<T> expect<T>(
        T[] actual,
        [CallerArgumentExpression("actual")] string? expr = null)
        => new CollectionExpectation<T>(actual, expr);

    /// <summary>
    /// Create an expectation for a list.
    /// </summary>
    public static CollectionExpectation<T> expect<T>(
        List<T> actual,
        [CallerArgumentExpression("actual")] string? expr = null)
        => new CollectionExpectation<T>(actual, expr);

    /// <summary>
    /// Create an expectation for a collection.
    /// </summary>
    public static CollectionExpectation<T> expect<T>(
        IList<T> actual,
        [CallerArgumentExpression("actual")] string? expr = null)
        => new CollectionExpectation<T>(actual, expr);

    // Static options for JSON serialization (reused for performance)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Run all collected specs and output results.
    /// Sets Environment.ExitCode to 1 if any specs failed.
    /// </summary>
    /// <param name="json">If true, output JSON instead of console format</param>
    public static void run(bool json = false)
    {
        if (RootContext is null)
        {
            if (json)
                Console.WriteLine("{}");
            else
                Console.WriteLine("No specs defined.");
            return;
        }

        var runner = new SpecRunner();
        var results = runner.Run(RootContext);

        // Build unified report
        var report = SpecReportBuilder.Build(RootContext, results);

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
        }
        else
        {
            var formatter = new ConsoleFormatter();
            formatter.Format(report, Console.Out);
        }

        // Set exit code based on failures
        if (report.Summary.Failed > 0)
        {
            Environment.ExitCode = 1;
        }

        // Reset for next run
        RootContext = null;
        CurrentContext = null;
    }

    private static void EnsureContext()
    {
        if (CurrentContext is null)
            throw new InvalidOperationException("Must be called inside a describe() block");
    }

}
