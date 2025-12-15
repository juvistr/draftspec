namespace DraftSpec;

public static partial class Dsl
{
    private static readonly AsyncLocal<SpecRunnerBuilder?> RunnerBuilderLocal = new();

    internal static SpecRunnerBuilder? RunnerBuilder
    {
        get => RunnerBuilderLocal.Value;
        set => RunnerBuilderLocal.Value = value;
    }

    /// <summary>
    /// Configure the spec runner with middleware.
    /// Call before run().
    /// </summary>
    /// <example>
    /// configure(runner => runner.WithRetry(3).WithTimeout(5000));
    /// </example>
    public static void configure(Action<SpecRunnerBuilder> configureRunner)
    {
        var builder = RunnerBuilder ?? new SpecRunnerBuilder();
        configureRunner(builder);
        RunnerBuilder = builder;
    }
}
