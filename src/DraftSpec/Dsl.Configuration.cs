using DraftSpec.Configuration;

namespace DraftSpec;

public static partial class Dsl
{
    private static readonly AsyncLocal<SpecRunnerBuilder?> RunnerBuilderLocal = new();
    private static readonly AsyncLocal<DraftSpecConfiguration?> ConfigurationLocal = new();

    internal static SpecRunnerBuilder? RunnerBuilder
    {
        get => RunnerBuilderLocal.Value;
        set => RunnerBuilderLocal.Value = value;
    }

    internal static DraftSpecConfiguration? Configuration
    {
        get => ConfigurationLocal.Value;
        set => ConfigurationLocal.Value = value;
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

    /// <summary>
    /// Configure DraftSpec with plugins, formatters, reporters, and services.
    /// Call before run().
    /// </summary>
    /// <example>
    /// configure(config => {
    ///     config.UsePlugin&lt;SlackReporterPlugin&gt;();
    ///     config.AddReporter(new FileReporter("results.json"));
    ///     config.AddFormatter("custom", new MyFormatter());
    /// });
    /// </example>
    public static void configure(Action<DraftSpecConfiguration> configureConfig)
    {
        var config = Configuration ?? new DraftSpecConfiguration();
        configureConfig(config);
        Configuration = config;

        // Ensure the builder has the configuration
        var builder = RunnerBuilder ?? new SpecRunnerBuilder();
        builder.WithConfiguration(config);
        RunnerBuilder = builder;
    }
}