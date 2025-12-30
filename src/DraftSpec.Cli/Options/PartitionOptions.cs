using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli.Options;

/// <summary>
/// Composable options for CI parallelism via spec partitioning.
/// Used by run command.
/// </summary>
public class PartitionOptions
{
    /// <summary>
    /// Total number of partitions to divide specs into.
    /// Used with Index for CI parallel execution.
    /// </summary>
    public int? Total { get; set; }

    /// <summary>
    /// Zero-based index of this partition (0 to Total-1).
    /// </summary>
    public int? Index { get; set; }

    /// <summary>
    /// Strategy for partitioning: "file" (default) or "spec-count".
    /// - file: Round-robin by sorted file path (fast, deterministic)
    /// - spec-count: Balance by spec count per file (requires parsing)
    /// </summary>
    public PartitionStrategy Strategy { get; set; } = PartitionStrategy.File;

    /// <summary>
    /// Returns true if partitioning is enabled (both Total and Index are set).
    /// </summary>
    public bool IsEnabled => Total.HasValue && Index.HasValue;

    /// <summary>
    /// Validates the partition options.
    /// </summary>
    /// <returns>Error message if invalid, null if valid.</returns>
    public string? Validate()
    {
        if (Total.HasValue && !Index.HasValue)
            return "--partition requires --partition-index";

        if (!Total.HasValue && Index.HasValue)
            return "--partition-index requires --partition";

        if (Total.HasValue && Total.Value < 1)
            return "--partition must be at least 1";

        if (Index.HasValue && Index.Value < 0)
            return "--partition-index must be at least 0";

        if (Total.HasValue && Index.HasValue && Index.Value >= Total.Value)
            return $"--partition-index must be less than --partition ({Total.Value})";

        return null;
    }
}
