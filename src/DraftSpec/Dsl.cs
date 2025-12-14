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
        else
        {
            // Nested describe
            var parent = CurrentContext!;
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

    /// <summary>
    /// Run all collected specs and output results.
    /// </summary>
    public static void run()
    {
        if (RootContext is null)
        {
            Console.WriteLine("No specs defined.");
            return;
        }

        var runner = new SpecRunner();
        var results = runner.Run(RootContext);

        OutputResults(results);

        // Reset for next run
        RootContext = null;
        CurrentContext = null;
    }

    private static void EnsureContext()
    {
        if (CurrentContext is null)
            throw new InvalidOperationException("Must be called inside a describe() block");
    }

    private static void OutputResults(List<SpecResult> results)
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
        if (passed > 0) { Console.ForegroundColor = ConsoleColor.Green; Console.Write($"{passed} passed"); Console.ResetColor(); }
        if (failed > 0) { Console.Write(", "); Console.ForegroundColor = ConsoleColor.Red; Console.Write($"{failed} failed"); Console.ResetColor(); }
        if (pending > 0) { Console.Write(", "); Console.ForegroundColor = ConsoleColor.Yellow; Console.Write($"{pending} pending"); Console.ResetColor(); }
        if (skipped > 0) { Console.Write(", "); Console.ForegroundColor = ConsoleColor.DarkGray; Console.Write($"{skipped} skipped"); Console.ResetColor(); }
        Console.WriteLine();
    }
}
