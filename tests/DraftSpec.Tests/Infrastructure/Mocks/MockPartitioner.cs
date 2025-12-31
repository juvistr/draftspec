using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Services;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of ISpecPartitioner for testing.
/// Returns configurable partition results.
/// </summary>
internal class MockPartitioner : ISpecPartitioner
{
    private readonly PartitionResult _result;

    /// <summary>
    /// Creates a mock partitioner that returns the specified result.
    /// </summary>
    public MockPartitioner(PartitionResult result)
    {
        _result = result;
    }

    /// <summary>
    /// Creates a mock partitioner that returns a result with the specified files.
    /// </summary>
    public MockPartitioner(IReadOnlyList<string> files, int totalFiles, int? totalSpecs = null, int? partitionSpecs = null)
        : this(new PartitionResult(files, totalFiles, totalSpecs, partitionSpecs))
    {
    }

    public Task<PartitionResult> PartitionAsync(
        IReadOnlyList<string> specFiles,
        int totalPartitions,
        int partitionIndex,
        PartitionStrategy strategy,
        string projectPath,
        CancellationToken ct = default)
    {
        return Task.FromResult(_result);
    }
}
