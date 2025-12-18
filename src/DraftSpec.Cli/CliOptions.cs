namespace DraftSpec.Cli;

public class CliOptions
{
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
}