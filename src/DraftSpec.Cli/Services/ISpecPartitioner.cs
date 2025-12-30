using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli.Services;

/// <summary>
/// Partitions spec files for distributed CI execution.
/// </summary>
public interface ISpecPartitioner
{
    /// <summary>
    /// Partitions the given spec files into the specified number of partitions
    /// and returns the files for the requested partition index.
    /// </summary>
    /// <param name="specFiles">All discovered spec files.</param>
    /// <param name="totalPartitions">Total number of partitions.</param>
    /// <param name="partitionIndex">Zero-based index of the partition to return.</param>
    /// <param name="strategy">Partitioning strategy.</param>
    /// <param name="projectPath">Base project path for spec parsing (needed for spec-count strategy).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The partition result containing assigned files.</returns>
    Task<PartitionResult> PartitionAsync(
        IReadOnlyList<string> specFiles,
        int totalPartitions,
        int partitionIndex,
        PartitionStrategy strategy,
        string projectPath,
        CancellationToken ct = default);
}

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
