using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        if (json)
            OutputJson(RootContext, results);
        else
            OutputConsole(results);

        // Set exit code based on failures
        var failed = results.Count(r => r.Status == SpecStatus.Failed);
        if (failed > 0)
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

    private static void OutputConsole(List<SpecResult> results)
    {
        Console.WriteLine();

        var printedPaths = new HashSet<string>();

        foreach (var result in results)
        {
            // Print context path segments
            for (int i = 0; i < result.ContextPath.Count; i++)
            {
                var pathKey = string.Join("/", result.ContextPath.Take(i + 1));
                if (!printedPaths.Contains(pathKey))
                {
                    printedPaths.Add(pathKey);
                    var indent = new string(' ', i * 2);
                    Console.WriteLine($"{indent}{result.ContextPath[i]}");
                }
            }

            // Print spec with status
            var specIndent = new string(' ', result.ContextPath.Count * 2);
            var (symbol, color) = result.Status switch
            {
                SpecStatus.Passed => ("✓", ConsoleColor.Green),
                SpecStatus.Failed => ("✗", ConsoleColor.Red),
                SpecStatus.Pending => ("○", ConsoleColor.Yellow),
                SpecStatus.Skipped => ("-", ConsoleColor.DarkGray),
                _ => ("?", ConsoleColor.White)
            };

            Console.ForegroundColor = color;
            Console.Write($"{specIndent}{symbol} ");
            Console.ResetColor();
            Console.WriteLine(result.Spec.Description);

            if (result.Status == SpecStatus.Failed && result.Exception != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{specIndent}  {result.Exception.Message}");
                Console.ResetColor();
            }
        }

        // Summary
        Console.WriteLine();
        Console.WriteLine(new string('-', 50));

        var passed = results.Count(r => r.Status == SpecStatus.Passed);
        var failed = results.Count(r => r.Status == SpecStatus.Failed);
        var pending = results.Count(r => r.Status == SpecStatus.Pending);
        var skipped = results.Count(r => r.Status == SpecStatus.Skipped);

        Console.Write($"{results.Count} specs: ");
        var first = true;
        void WriteStat(int count, string label, ConsoleColor color)
        {
            if (count == 0) return;
            if (!first) Console.Write(", ");
            first = false;
            Console.ForegroundColor = color;
            Console.Write($"{count} {label}");
            Console.ResetColor();
        }
        WriteStat(passed, "passed", ConsoleColor.Green);
        WriteStat(failed, "failed", ConsoleColor.Red);
        WriteStat(pending, "pending", ConsoleColor.Yellow);
        WriteStat(skipped, "skipped", ConsoleColor.DarkGray);
        Console.WriteLine();
    }

    private static void OutputJson(SpecContext rootContext, List<SpecResult> results)
    {
        var passed = results.Count(r => r.Status == SpecStatus.Passed);
        var failed = results.Count(r => r.Status == SpecStatus.Failed);
        var pending = results.Count(r => r.Status == SpecStatus.Pending);
        var skipped = results.Count(r => r.Status == SpecStatus.Skipped);

        var report = new JsonReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new JsonSummary
            {
                Total = results.Count,
                Passed = passed,
                Failed = failed,
                Pending = pending,
                Skipped = skipped
            },
            Contexts = BuildContextTree(rootContext, results)
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        Console.WriteLine(JsonSerializer.Serialize(report, options));
    }

    private static List<JsonContext> BuildContextTree(SpecContext context, List<SpecResult> allResults)
    {
        var contextList = new List<JsonContext>();
        BuildContextTreeRecursive(context, allResults, contextList, []);
        return contextList;
    }

    private static void BuildContextTreeRecursive(
        SpecContext context,
        List<SpecResult> allResults,
        List<JsonContext> targetList,
        List<string> currentPath)
    {
        var jsonContext = new JsonContext { Description = context.Description };
        currentPath.Add(context.Description);

        // Find specs that belong to this context
        foreach (var spec in context.Specs)
        {
            var result = allResults.FirstOrDefault(r =>
                r.Spec == spec && r.ContextPath.SequenceEqual(currentPath));

            jsonContext.Specs.Add(new JsonSpec
            {
                Description = spec.Description,
                Status = result?.Status.ToString().ToLowerInvariant() ?? "unknown",
                Error = result?.Exception?.Message
            });
        }

        // Recursively process child contexts
        foreach (var child in context.Children)
        {
            BuildContextTreeRecursive(child, allResults, jsonContext.Contexts, [.. currentPath]);
        }

        // Only add if there are specs or nested contexts
        if (jsonContext.Specs.Count > 0 || jsonContext.Contexts.Count > 0)
        {
            targetList.Add(jsonContext);
        }
    }
}

// JSON output models
internal class JsonReport
{
    public DateTime Timestamp { get; set; }
    public JsonSummary Summary { get; set; } = new();
    public List<JsonContext> Contexts { get; set; } = [];
}

internal class JsonSummary
{
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Pending { get; set; }
    public int Skipped { get; set; }
}

internal class JsonContext
{
    public string Description { get; set; } = "";
    public List<JsonSpec> Specs { get; set; } = [];
    public List<JsonContext> Contexts { get; set; } = [];
}

internal class JsonSpec
{
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Error { get; set; }
}
