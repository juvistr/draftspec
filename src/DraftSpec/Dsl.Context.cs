using System.Runtime.CompilerServices;

namespace DraftSpec;

public static partial class Dsl
{
    /// <summary>
    /// Define a spec group. Creates root context on first call.
    /// </summary>
    public static void describe(string description, Action body, [CallerLineNumber] int lineNumber = 0)
    {
        if (RootContext is null)
        {
            // First describe call - create root
            RootContext = new SpecContext(description) { LineNumber = lineNumber };
            ExecuteInContext(RootContext, restoreTo: null, body);
        }
        else if (CurrentContext is null)
        {
            // Another top-level describe - add as child of root
            var context = new SpecContext(description, RootContext) { LineNumber = lineNumber };
            ExecuteInContext(context, restoreTo: null, body);
        }
        else
        {
            // Nested describe
            var parent = CurrentContext;
            var context = new SpecContext(description, parent) { LineNumber = lineNumber };
            ExecuteInContext(context, restoreTo: parent, body);
        }
    }

    /// <summary>
    /// Execute body within the given context, restoring CurrentContext afterward.
    /// </summary>
    private static void ExecuteInContext(SpecContext context, SpecContext? restoreTo, Action body)
    {
        CurrentContext = context;
        try
        {
            body();
        }
        finally
        {
            CurrentContext = restoreTo;
        }
    }

    /// <summary>
    /// Alias for describe - used for sub-groupings.
    /// </summary>
    public static void context(string description, Action body, [CallerLineNumber] int lineNumber = 0)
    {
        describe(description, body, lineNumber);
    }
}
