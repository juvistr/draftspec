namespace DraftSpec;

public static partial class Dsl
{
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
}
