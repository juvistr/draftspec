using DraftSpec.Internal;

namespace DraftSpec;

public static partial class Dsl
{
    /// <summary>
    /// Run all collected specs and output results.
    /// Sets Environment.ExitCode to 1 if any specs failed.
    /// </summary>
    /// <param name="json">If true, output JSON instead of console format</param>
    public static void run(bool json = false)
    {
        SpecExecutor.ExecuteAndOutput(RootContext, json, ResetState);
    }

    private static void ResetState()
    {
        RootContext = null;
        CurrentContext = null;
    }
}
