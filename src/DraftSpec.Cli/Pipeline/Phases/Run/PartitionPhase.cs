using DraftSpec.Cli.Options;
using DraftSpec.Cli.Services;

namespace DraftSpec.Cli.Pipeline.Phases.Run;

/// <summary>
/// Partitions spec files for distributed CI execution.
/// When partition options are enabled, filters spec files to a subset.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[SpecFiles]</c>, <c>Items[ProjectPath]</c></para>
/// <para><b>Optional:</b> <c>Items[Partition]</c></para>
/// <para><b>Modifies:</b> <c>Items[SpecFiles]</c> (filters to partition subset)</para>
/// <para><b>Short-circuits:</b> Returns 0 if partition is empty</para>
/// </remarks>
public class PartitionPhase : ICommandPhase
{
    private readonly ISpecPartitioner _partitioner;

    public PartitionPhase(ISpecPartitioner partitioner)
    {
        _partitioner = partitioner;
    }

    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var partition = context.Get<PartitionOptions>(ContextKeys.Partition);
        if (partition == null || !partition.IsEnabled)
            return await pipeline(context, ct);

        var projectPath = context.Get<string>(ContextKeys.ProjectPath);
        if (string.IsNullOrEmpty(projectPath))
        {
            context.Console.WriteError("ProjectPath not set. Run PathResolutionPhase first.");
            return 1;
        }

        var specFiles = context.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
        if (specFiles == null || specFiles.Count == 0)
        {
            context.Console.WriteLine("No spec files to partition.");
            return 0;
        }

        var result = await _partitioner.PartitionAsync(
            specFiles,
            partition.Total!.Value,
            partition.Index!.Value,
            partition.Strategy,
            projectPath,
            ct);

        // Display partition info
        context.Console.ForegroundColor = ConsoleColor.DarkGray;
        context.Console.WriteLine($"Partition {partition.Index!.Value + 1}/{partition.Total!.Value}: {result.Files.Count} files");
        if (result.TotalSpecs.HasValue)
            context.Console.WriteLine($"  Specs: {result.PartitionSpecs}/{result.TotalSpecs}");
        context.Console.ResetColor();

        if (result.Files.Count == 0)
        {
            context.Console.WriteLine("No specs in this partition.");
            return 0;
        }

        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, result.Files);
        return await pipeline(context, ct);
    }
}
