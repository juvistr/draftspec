using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Options.Enums;

/// <summary>
/// Strategy for partitioning specs across CI parallel jobs.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PartitionStrategy>))]
public enum PartitionStrategy
{
    /// <summary>
    /// Round-robin by sorted file path. Fast and deterministic.
    /// </summary>
    File,

    /// <summary>
    /// Balance by spec count per file. Requires parsing but better load balancing.
    /// </summary>
    SpecCount
}
