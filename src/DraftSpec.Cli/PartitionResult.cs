using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli.Services;

/// <summary>
/// Result of partitioning spec files.
/// </summary>
/// <param name="Files">The spec files assigned to this partition.</param>
/// <param name="TotalFiles">Total files across all partitions.</param>
/// <param name="TotalSpecs">Total specs across all partitions (only for spec-count strategy).</param>
/// <param name="PartitionSpecs">Specs in this partition (only for spec-count strategy).</param>
public record PartitionResult(
    IReadOnlyList<string> Files,
    int TotalFiles,
    int? TotalSpecs = null,
    int? PartitionSpecs = null);
