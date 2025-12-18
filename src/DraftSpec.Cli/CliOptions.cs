using DraftSpec.Cli.Configuration;

namespace DraftSpec.Cli;

public class CliOptions
{
    /// <summary>
    /// Tracks which options were explicitly set via CLI (vs defaults).
    /// Used to determine if config file values should apply.
    /// </summary>
    public HashSet<string> ExplicitlySet { get; } = [];

    public string Command { get; set; } = "";
    public string Path { get; set; } = ".";
    public string Format { get; set; } = "console";
    public string? OutputFile { get; set; }
    public string? CssUrl { get; set; }
    public bool ShowHelp { get; set; }
    public string? Error { get; set; }
    public bool Force { get; set; }
    public string? SpecName { get; set; }
    public bool Parallel { get; set; }

    /// <summary>
    /// Additional reporter names to use (comma-separated).
    /// Example: "file:results.json,slack"
    /// </summary>
    public string? Reporters { get; set; }

    /// <summary>
    /// Disable dotnet-script caching, forcing recompilation on every run.
    /// </summary>
    public bool NoCache { get; set; }

    /// <summary>
    /// Stop execution after first spec failure.
    /// Remaining specs will be reported as skipped.
    /// </summary>
    public bool Bail { get; set; }

    /// <summary>
    /// Comma-separated list of tags to include.
    /// Only specs with any of these tags will run.
    /// </summary>
    public string? FilterTags { get; set; }

    /// <summary>
    /// Comma-separated list of tags to exclude.
    /// Specs with any of these tags will be skipped.
    /// </summary>
    public string? ExcludeTags { get; set; }

    /// <summary>
    /// Regex pattern to match spec names (context path + description).
    /// Only specs matching this pattern will run.
    /// </summary>
    public string? FilterName { get; set; }

    /// <summary>
    /// Regex pattern to exclude spec names (context path + description).
    /// Specs matching this pattern will be skipped.
    /// </summary>
    public string? ExcludeName { get; set; }

    /// <summary>
    /// Apply default values from a project configuration file.
    /// Only applies values that weren't explicitly set via CLI.
    /// </summary>
    /// <param name="config">The project configuration to apply.</param>
    public void ApplyDefaults(DraftSpecProjectConfig config)
    {
        if (!ExplicitlySet.Contains(nameof(Parallel)) && config.Parallel.HasValue)
            Parallel = config.Parallel.Value;

        if (!ExplicitlySet.Contains(nameof(Bail)) && config.Bail.HasValue)
            Bail = config.Bail.Value;

        if (!ExplicitlySet.Contains(nameof(NoCache)) && config.NoCache.HasValue)
            NoCache = config.NoCache.Value;

        if (!ExplicitlySet.Contains(nameof(Format)) && !string.IsNullOrEmpty(config.Format))
            Format = config.Format;

        if (!ExplicitlySet.Contains(nameof(OutputFile)) && !string.IsNullOrEmpty(config.OutputDirectory))
            OutputFile = config.OutputDirectory;

        if (!ExplicitlySet.Contains(nameof(FilterTags)) && config.Tags?.Include is { Count: > 0 })
            FilterTags = string.Join(",", config.Tags.Include);

        if (!ExplicitlySet.Contains(nameof(ExcludeTags)) && config.Tags?.Exclude is { Count: > 0 })
            ExcludeTags = string.Join(",", config.Tags.Exclude);

        if (!ExplicitlySet.Contains(nameof(Reporters)) && config.Reporters is { Count: > 0 })
            Reporters = string.Join(",", config.Reporters);
    }
}