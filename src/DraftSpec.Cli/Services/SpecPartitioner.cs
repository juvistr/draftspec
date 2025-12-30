using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Services;

/// <summary>
/// Partitions spec files for distributed CI execution.
/// Supports two strategies:
/// - file: Round-robin by sorted file path (fast, deterministic)
/// - spec-count: Balance by spec count per file (requires parsing)
/// </summary>
public class SpecPartitioner : ISpecPartitioner
{
    /// <inheritdoc />
    public async Task<PartitionResult> PartitionAsync(
        IReadOnlyList<string> specFiles,
        int totalPartitions,
        int partitionIndex,
        string strategy,
        string projectPath,
        CancellationToken ct = default)
    {
        if (specFiles.Count == 0)
        {
            return new PartitionResult([], 0);
        }

        // Sort files for deterministic partitioning
        var sortedFiles = specFiles.OrderBy(f => f, StringComparer.Ordinal).ToList();

        return strategy.ToLowerInvariant() switch
        {
            "spec-count" => await PartitionBySpecCountAsync(sortedFiles, totalPartitions, partitionIndex, projectPath, ct),
            _ => PartitionByFile(sortedFiles, totalPartitions, partitionIndex)
        };
    }

    /// <summary>
    /// Round-robin partitioning by file path (fast, deterministic).
    /// Files are assigned to partitions in sorted order: file[i] goes to partition i % totalPartitions.
    /// </summary>
    private static PartitionResult PartitionByFile(
        IReadOnlyList<string> sortedFiles,
        int totalPartitions,
        int partitionIndex)
    {
        var partitionFiles = sortedFiles
            .Where((_, i) => i % totalPartitions == partitionIndex)
            .ToList();

        return new PartitionResult(partitionFiles, sortedFiles.Count);
    }

    /// <summary>
    /// Greedy partitioning by spec count (balanced, requires parsing).
    /// Assigns files to the partition with the lowest current spec count.
    /// </summary>
    private static async Task<PartitionResult> PartitionBySpecCountAsync(
        IReadOnlyList<string> sortedFiles,
        int totalPartitions,
        int partitionIndex,
        string projectPath,
        CancellationToken ct)
    {
        // Parse each file to get spec count
        var parser = new StaticSpecParser(projectPath);
        var fileSpecCounts = new List<(string File, int Count)>();

        foreach (var file in sortedFiles)
        {
            ct.ThrowIfCancellationRequested();
            var result = await parser.ParseFileAsync(file, ct);
            fileSpecCounts.Add((file, result.Specs.Count));
        }

        // Sort by spec count descending (largest first for better balancing)
        var sortedByCount = fileSpecCounts
            .OrderByDescending(f => f.Count)
            .ThenBy(f => f.File, StringComparer.Ordinal) // Secondary sort for determinism
            .ToList();

        // Greedy assignment: give each file to the partition with lowest current total
        var partitionTotals = new int[totalPartitions];
        var partitionAssignments = new List<string>[totalPartitions];
        for (var i = 0; i < totalPartitions; i++)
            partitionAssignments[i] = [];

        foreach (var (file, count) in sortedByCount)
        {
            // Find partition with lowest total
            var minPartition = 0;
            for (var i = 1; i < totalPartitions; i++)
            {
                if (partitionTotals[i] < partitionTotals[minPartition])
                    minPartition = i;
            }

            partitionAssignments[minPartition].Add(file);
            partitionTotals[minPartition] += count;
        }

        var totalSpecs = fileSpecCounts.Sum(f => f.Count);
        var partitionSpecs = partitionTotals[partitionIndex];

        // Return files for requested partition, sorted for consistent ordering
        var resultFiles = partitionAssignments[partitionIndex]
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        return new PartitionResult(resultFiles, sortedFiles.Count, totalSpecs, partitionSpecs);
    }
}
