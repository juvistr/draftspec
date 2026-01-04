using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Run;
using DraftSpec.Cli.Services;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Run;

/// <summary>
/// Tests for <see cref="PartitionPhase"/>.
/// </summary>
public class PartitionPhaseTests
{
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
    }

    #region Partition Disabled Tests

    [Test]
    public async Task ExecuteAsync_PartitionNull_PassesThroughUnchanged()
    {
        var partitioner = new MockPartitioner([], 0);
        var phase = new PartitionPhase(partitioner);
        var context = CreateContext();
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_PartitionNotEnabled_PassesThroughUnchanged()
    {
        var partitioner = new MockPartitioner([], 0);
        var phase = new PartitionPhase(partitioner);
        var context = CreateContext();
        context.Set(ContextKeys.Partition, new PartitionOptions()); // IsEnabled = false
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsTrue();
    }

    #endregion

    #region Missing Requirements Tests

    [Test]
    public async Task ExecuteAsync_NoProjectPath_ReturnsError()
    {
        var partitioner = new MockPartitioner([], 0);
        var phase = new PartitionPhase(partitioner);
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.Partition, new PartitionOptions { Total = 2, Index = 0 });

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("ProjectPath not set");
    }

    [Test]
    public async Task ExecuteAsync_NoSpecFiles_ReturnsZero()
    {
        var partitioner = new MockPartitioner([], 0);
        var phase = new PartitionPhase(partitioner);
        var context = CreateContext();
        context.Set(ContextKeys.Partition, new PartitionOptions { Total = 2, Index = 0 });
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, []);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No spec files");
    }

    [Test]
    public async Task ExecuteAsync_NullSpecFiles_ReturnsZero()
    {
        var partitioner = new MockPartitioner([], 0);
        var phase = new PartitionPhase(partitioner);
        var context = CreateContext();
        context.Set(ContextKeys.Partition, new PartitionOptions { Total = 2, Index = 0 });
        // Don't set SpecFiles - it will be null

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No spec files");
    }

    #endregion

    #region Partition Success Tests

    [Test]
    public async Task ExecuteAsync_PartitionHasFiles_UpdatesSpecFiles()
    {
        var partitionFiles = new List<string> { TestPaths.Spec("a.spec.csx") };
        var partitioner = new MockPartitioner(partitionFiles, 3);
        var phase = new PartitionPhase(partitioner);
        var context = CreateContext();
        context.Set(ContextKeys.Partition, new PartitionOptions { Total = 3, Index = 0 });
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [
            TestPaths.Spec("a.spec.csx"),
            TestPaths.Spec("b.spec.csx"),
            TestPaths.Spec("c.spec.csx")
        ]);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var specFiles = context.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
        await Assert.That(specFiles!.Count).IsEqualTo(1);
        await Assert.That(specFiles[0]).IsEqualTo(partitionFiles[0]);
    }

    [Test]
    public async Task ExecuteAsync_PartitionHasFiles_DisplaysPartitionInfo()
    {
        var partitionFiles = new List<string> { TestPaths.Spec("a.spec.csx") };
        var partitioner = new MockPartitioner(partitionFiles, 3, totalSpecs: 10, partitionSpecs: 3);
        var phase = new PartitionPhase(partitioner);
        var context = CreateContext();
        context.Set(ContextKeys.Partition, new PartitionOptions { Total = 3, Index = 0 });
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("a.spec.csx")]);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("Partition 1/3");
        await Assert.That(_console.Output).Contains("1 files");
        await Assert.That(_console.Output).Contains("3/10");
    }

    [Test]
    public async Task ExecuteAsync_PartitionWithoutSpecCounts_OmitsSpecLine()
    {
        var partitionFiles = new List<string> { TestPaths.Spec("a.spec.csx") };
        // No totalSpecs/partitionSpecs - uses file-based partitioning
        var partitioner = new MockPartitioner(partitionFiles, 3);
        var phase = new PartitionPhase(partitioner);
        var context = CreateContext();
        context.Set(ContextKeys.Partition, new PartitionOptions { Total = 3, Index = 0 });
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("a.spec.csx")]);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("Partition 1/3");
        await Assert.That(_console.Output).Contains("1 files");
        // Should NOT contain spec count line when TotalSpecs is null
        await Assert.That(_console.Output).DoesNotContain("Specs:");
    }

    [Test]
    public async Task ExecuteAsync_PartitionHasFiles_ContinuesPipeline()
    {
        var partitionFiles = new List<string> { TestPaths.Spec("a.spec.csx") };
        var partitioner = new MockPartitioner(partitionFiles, 3);
        var phase = new PartitionPhase(partitioner);
        var context = CreateContext();
        context.Set(ContextKeys.Partition, new PartitionOptions { Total = 3, Index = 0 });
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("a.spec.csx")]);
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsTrue();
    }

    #endregion

    #region Empty Partition Tests

    [Test]
    public async Task ExecuteAsync_EmptyPartition_ReturnsZero()
    {
        var partitioner = new MockPartitioner([], 3);
        var phase = new PartitionPhase(partitioner);
        var context = CreateContext();
        context.Set(ContextKeys.Partition, new PartitionOptions { Total = 3, Index = 2 });
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("a.spec.csx")]);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No specs in this partition");
    }

    [Test]
    public async Task ExecuteAsync_EmptyPartition_DoesNotContinuePipeline()
    {
        var partitioner = new MockPartitioner([], 3);
        var phase = new PartitionPhase(partitioner);
        var context = CreateContext();
        context.Set(ContextKeys.Partition, new PartitionOptions { Total = 3, Index = 2 });
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("a.spec.csx")]);
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsFalse();
    }

    #endregion

    #region Helper Methods

    private CommandContext CreateContext(string? projectPath = null)
    {
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.ProjectPath, projectPath ?? TestPaths.ProjectDir);
        return context;
    }

    #endregion
}
