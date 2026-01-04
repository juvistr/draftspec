using DraftSpec.Cli;
using DraftSpec.Cli.Services;
using DraftSpec.Cli.Watch;
using DraftSpec.TestingPlatform;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Watch;

/// <summary>
/// Tests for WatchEventProcessor - the extracted file change decision logic.
/// </summary>
public class WatchEventProcessorTests
{
    // Common test paths
    private static string BasePath => TestPaths.SpecsDir;
    private static string TestSpec1 => TestPaths.Spec("test1.spec.csx");
    private static string TestSpec2 => TestPaths.Spec("test2.spec.csx");
    private static string SourceFile => TestPaths.Temp("src/MyClass.cs");

    #region RunAll - Non-Spec File Changes

    [Test]
    public async Task ProcessChangeAsync_SourceFileChanged_ReturnsRunAll()
    {
        // Arrange
        var processor = CreateProcessor();
        var change = new FileChangeInfo(SourceFile, IsSpecFile: false);
        var allSpecFiles = new List<string> { TestSpec1, TestSpec2 };

        // Act
        var action = await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: false, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(action.Type).IsEqualTo(WatchActionType.RunAll);
    }

    [Test]
    public async Task ProcessChangeAsync_NullFilePath_ReturnsRunAll()
    {
        // Arrange
        var processor = CreateProcessor();
        var change = new FileChangeInfo(FilePath: null, IsSpecFile: false);
        var allSpecFiles = new List<string> { TestSpec1 };

        // Act
        var action = await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: false, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(action.Type).IsEqualTo(WatchActionType.RunAll);
    }

    [Test]
    public async Task ProcessChangeAsync_SpecFileNotInList_ReturnsRunAll()
    {
        // Arrange
        var processor = CreateProcessor();
        var unknownSpec = TestPaths.Spec("unknown.spec.csx");
        var change = new FileChangeInfo(Path.GetFullPath(unknownSpec), IsSpecFile: true);
        var allSpecFiles = new List<string> { TestSpec1 }; // unknownSpec is NOT in this list

        // Act
        var action = await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: false, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(action.Type).IsEqualTo(WatchActionType.RunAll);
    }

    [Test]
    public async Task ProcessChangeAsync_EmptySpecFilesList_ReturnsRunAll()
    {
        // Arrange
        var processor = CreateProcessor();
        var change = new FileChangeInfo(Path.GetFullPath(TestSpec1), IsSpecFile: true);
        var allSpecFiles = new List<string>(); // Empty

        // Act
        var action = await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: false, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(action.Type).IsEqualTo(WatchActionType.RunAll);
    }

    #endregion

    #region RunFile - Non-Incremental Mode

    [Test]
    public async Task ProcessChangeAsync_SpecFileChanged_NonIncremental_ReturnsRunFile()
    {
        // Arrange
        var processor = CreateProcessor();
        var change = new FileChangeInfo(Path.GetFullPath(TestSpec1), IsSpecFile: true);
        var allSpecFiles = new List<string> { TestSpec1, TestSpec2 };

        // Act
        var action = await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: false, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(action.Type).IsEqualTo(WatchActionType.RunFile);
        await Assert.That(action.FilePath).IsEqualTo(TestSpec1);
        await Assert.That(action.Message).IsNull();
    }

    [Test]
    public async Task ProcessChangeAsync_PathComparison_CaseInsensitive()
    {
        // Arrange
        var processor = CreateProcessor();
        // Use different casing in the change path
        var upperCasePath = Path.GetFullPath(TestSpec1).ToUpperInvariant();
        var change = new FileChangeInfo(upperCasePath, IsSpecFile: true);
        var allSpecFiles = new List<string> { TestSpec1 };

        // Act
        var action = await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: false, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(action.Type).IsEqualTo(WatchActionType.RunFile);
        await Assert.That(action.FilePath).IsEqualTo(TestSpec1);
    }

    #endregion

    #region Skip - Incremental Mode No Changes

    [Test]
    public async Task ProcessChangeAsync_Incremental_NoChanges_ReturnsSkip()
    {
        // Arrange
        var changeTracker = new MockSpecChangeTracker()
            .WithNextChangeSet(new SpecChangeSet(TestSpec1, [], HasDynamicSpecs: false, DependencyChanged: false));
        var processor = CreateProcessor(changeTracker: changeTracker);
        var change = new FileChangeInfo(Path.GetFullPath(TestSpec1), IsSpecFile: true);
        var allSpecFiles = new List<string> { TestSpec1 };

        // Act
        var action = await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: true, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(action.Type).IsEqualTo(WatchActionType.Skip);
        await Assert.That(action.Message).IsEqualTo("No spec changes detected.");
    }

    #endregion

    #region RunFile - Incremental Mode Requires Full Run

    [Test]
    public async Task ProcessChangeAsync_Incremental_DynamicSpecs_ReturnsRunFileWithReason()
    {
        // Arrange
        var changeTracker = new MockSpecChangeTracker()
            .WithNextChangeSet(new SpecChangeSet(
                TestSpec1,
                [new SpecChange("test spec", ["Context"], SpecChangeType.Added)],
                HasDynamicSpecs: true,
                DependencyChanged: false));
        var processor = CreateProcessor(changeTracker: changeTracker);
        var change = new FileChangeInfo(Path.GetFullPath(TestSpec1), IsSpecFile: true);
        var allSpecFiles = new List<string> { TestSpec1 };

        // Act
        var action = await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: true, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(action.Type).IsEqualTo(WatchActionType.RunFile);
        await Assert.That(action.FilePath).IsEqualTo(TestSpec1);
        await Assert.That(action.Message).IsEqualTo("Full run required: dynamic specs detected");
    }

    [Test]
    public async Task ProcessChangeAsync_Incremental_DependencyChanged_ReturnsRunFileWithReason()
    {
        // Arrange
        var changeTracker = new MockSpecChangeTracker()
            .WithNextChangeSet(new SpecChangeSet(
                TestSpec1,
                [new SpecChange("test spec", ["Context"], SpecChangeType.Added)],
                HasDynamicSpecs: false,
                DependencyChanged: true));
        var processor = CreateProcessor(changeTracker: changeTracker);
        var change = new FileChangeInfo(Path.GetFullPath(TestSpec1), IsSpecFile: true);
        var allSpecFiles = new List<string> { TestSpec1 };

        // Act
        var action = await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: true, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(action.Type).IsEqualTo(WatchActionType.RunFile);
        await Assert.That(action.FilePath).IsEqualTo(TestSpec1);
        await Assert.That(action.Message).IsEqualTo("Full run required: dependency changed");
    }

    #endregion

    #region RunFiltered - Incremental Mode With Specific Changes

    [Test]
    public async Task ProcessChangeAsync_Incremental_SpecsChanged_ReturnsRunFiltered()
    {
        // Arrange
        var specChanges = new List<SpecChange>
        {
            new("creates a todo", ["TodoService"], SpecChangeType.Added)
        };
        var changeTracker = new MockSpecChangeTracker()
            .WithNextChangeSet(new SpecChangeSet(TestSpec1, specChanges, HasDynamicSpecs: false, DependencyChanged: false));
        var parserFactory = new MockStaticSpecParserFactory().WithSpecCount(1);
        var processor = CreateProcessor(changeTracker: changeTracker, parserFactory: parserFactory);
        var change = new FileChangeInfo(Path.GetFullPath(TestSpec1), IsSpecFile: true);
        var allSpecFiles = new List<string> { TestSpec1 };

        // Act
        var action = await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: true, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(action.Type).IsEqualTo(WatchActionType.RunFiltered);
        await Assert.That(action.FilePath).IsEqualTo(TestSpec1);
        await Assert.That(action.FilterPattern).IsNotNull();
        await Assert.That(action.Message).IsEqualTo("Incremental: 1 spec(s) changed");
        await Assert.That(action.ParseResultToRecord).IsNotNull();
    }

    [Test]
    public async Task ProcessChangeAsync_Incremental_MultipleSpecsChanged_ReturnsFilterPatternForAll()
    {
        // Arrange
        var specChanges = new List<SpecChange>
        {
            new("creates a todo", ["TodoService"], SpecChangeType.Added),
            new("deletes a todo", ["TodoService"], SpecChangeType.Modified)
        };
        var changeTracker = new MockSpecChangeTracker()
            .WithNextChangeSet(new SpecChangeSet(TestSpec1, specChanges, HasDynamicSpecs: false, DependencyChanged: false));
        var parserFactory = new MockStaticSpecParserFactory().WithSpecCount(2);
        var processor = CreateProcessor(changeTracker: changeTracker, parserFactory: parserFactory);
        var change = new FileChangeInfo(Path.GetFullPath(TestSpec1), IsSpecFile: true);
        var allSpecFiles = new List<string> { TestSpec1 };

        // Act
        var action = await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: true, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(action.Type).IsEqualTo(WatchActionType.RunFiltered);
        await Assert.That(action.Message).IsEqualTo("Incremental: 2 spec(s) changed");
        // Filter pattern should contain both descriptions
        await Assert.That(action.FilterPattern).Contains("creates\\ a\\ todo");
        await Assert.That(action.FilterPattern).Contains("deletes\\ a\\ todo");
    }

    [Test]
    public async Task ProcessChangeAsync_Incremental_DeletedSpecsExcluded_FromFilterPattern()
    {
        // Arrange - one added, one deleted
        var specChanges = new List<SpecChange>
        {
            new("creates a todo", ["TodoService"], SpecChangeType.Added),
            new("old spec", ["TodoService"], SpecChangeType.Deleted)
        };
        var changeTracker = new MockSpecChangeTracker()
            .WithNextChangeSet(new SpecChangeSet(TestSpec1, specChanges, HasDynamicSpecs: false, DependencyChanged: false));
        var parserFactory = new MockStaticSpecParserFactory().WithSpecCount(1);
        var processor = CreateProcessor(changeTracker: changeTracker, parserFactory: parserFactory);
        var change = new FileChangeInfo(Path.GetFullPath(TestSpec1), IsSpecFile: true);
        var allSpecFiles = new List<string> { TestSpec1 };

        // Act
        var action = await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: true, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(action.Type).IsEqualTo(WatchActionType.RunFiltered);
        // Only 1 spec to run (deleted is excluded from SpecsToRun)
        await Assert.That(action.Message).IsEqualTo("Incremental: 1 spec(s) changed");
    }

    #endregion

    #region Parser Factory Usage

    [Test]
    public async Task ProcessChangeAsync_Incremental_UsesParserFactory()
    {
        // Arrange
        var parserFactory = new MockStaticSpecParserFactory();
        var changeTracker = new MockSpecChangeTracker();
        var processor = CreateProcessor(changeTracker: changeTracker, parserFactory: parserFactory);
        var change = new FileChangeInfo(Path.GetFullPath(TestSpec1), IsSpecFile: true);
        var allSpecFiles = new List<string> { TestSpec1 };

        // Act
        await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: true, noCache: false, CancellationToken.None);

        // Assert
        await Assert.That(parserFactory.CreateCalls).Count().IsEqualTo(1);
        await Assert.That(parserFactory.Parser.ParseFileCalls).Count().IsEqualTo(1);
    }

    [Test]
    public async Task ProcessChangeAsync_Incremental_RespectsCacheFlag()
    {
        // Arrange
        var parserFactory = new MockStaticSpecParserFactory();
        var changeTracker = new MockSpecChangeTracker();
        var processor = CreateProcessor(changeTracker: changeTracker, parserFactory: parserFactory);
        var change = new FileChangeInfo(Path.GetFullPath(TestSpec1), IsSpecFile: true);
        var allSpecFiles = new List<string> { TestSpec1 };

        // Act - noCache = true
        await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: true, noCache: true, CancellationToken.None);

        // Assert
        await Assert.That(parserFactory.CreateCalls).Count().IsEqualTo(1);
        var (_, useCache) = parserFactory.CreateCalls[0];
        await Assert.That(useCache).IsFalse(); // noCache=true means useCache=false
    }

    [Test]
    public async Task ProcessChangeAsync_NonIncremental_DoesNotUseParserFactory()
    {
        // Arrange
        var parserFactory = new MockStaticSpecParserFactory();
        var processor = CreateProcessor(parserFactory: parserFactory);
        var change = new FileChangeInfo(Path.GetFullPath(TestSpec1), IsSpecFile: true);
        var allSpecFiles = new List<string> { TestSpec1 };

        // Act - non-incremental mode
        await processor.ProcessChangeAsync(
            change, allSpecFiles, BasePath, incremental: false, noCache: false, CancellationToken.None);

        // Assert - parser factory should not be used
        await Assert.That(parserFactory.CreateCalls).Count().IsEqualTo(0);
    }

    [Test]
    public async Task ProcessChangeAsync_Incremental_BasePathIsFile_UsesParentDirectory()
    {
        // Arrange - use a file path as basePath instead of directory
        var parserFactory = new MockStaticSpecParserFactory();
        var changeTracker = new MockSpecChangeTracker();
        var processor = CreateProcessor(changeTracker: changeTracker, parserFactory: parserFactory);

        // Create a temp file to test with
        var tempDir = Path.GetTempPath();
        var tempFile = Path.Combine(tempDir, $"test_{Guid.NewGuid()}.csproj");
        File.WriteAllText(tempFile, "<Project></Project>");

        try
        {
            var specFile = Path.Combine(tempDir, "test.spec.csx");
            var change = new FileChangeInfo(specFile, IsSpecFile: true);
            var allSpecFiles = new List<string> { specFile };

            // Act - pass file path as basePath
            await processor.ProcessChangeAsync(
                change, allSpecFiles, tempFile, incremental: true, noCache: false, CancellationToken.None);

            // Assert - parser should be created with the directory, not the file
            await Assert.That(parserFactory.CreateCalls).Count().IsEqualTo(1);
            var (baseDir, _) = parserFactory.CreateCalls[0];
            await Assert.That(baseDir).IsEqualTo(tempDir.TrimEnd(Path.DirectorySeparatorChar));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region BuildFilterPattern

    [Test]
    public async Task BuildFilterPattern_EmptyList_ReturnsMatchNothing()
    {
        var pattern = WatchEventProcessor.BuildFilterPattern([]);
        await Assert.That(pattern).IsEqualTo("^$");
    }

    [Test]
    public async Task BuildFilterPattern_SingleSpec_ReturnsExactMatch()
    {
        var specs = new List<SpecChange>
        {
            new("creates a todo", ["Context"], SpecChangeType.Added)
        };

        var pattern = WatchEventProcessor.BuildFilterPattern(specs);

        await Assert.That(pattern).IsEqualTo(@"^(creates\ a\ todo)$");
    }

    [Test]
    public async Task BuildFilterPattern_MultipleSpecs_ReturnsAlternation()
    {
        var specs = new List<SpecChange>
        {
            new("creates a todo", ["Context"], SpecChangeType.Added),
            new("deletes a todo", ["Context"], SpecChangeType.Modified)
        };

        var pattern = WatchEventProcessor.BuildFilterPattern(specs);

        await Assert.That(pattern).IsEqualTo(@"^(creates\ a\ todo|deletes\ a\ todo)$");
    }

    [Test]
    public async Task BuildFilterPattern_SpecialCharacters_AreEscaped()
    {
        var specs = new List<SpecChange>
        {
            new("returns 3.14", ["Math"], SpecChangeType.Added)
        };

        var pattern = WatchEventProcessor.BuildFilterPattern(specs);

        await Assert.That(pattern).IsEqualTo(@"^(returns\ 3\.14)$");
    }

    [Test]
    public async Task BuildFilterPattern_ResultIsValidRegex()
    {
        var specs = new List<SpecChange>
        {
            new("test with [special] chars (here)", ["Test"], SpecChangeType.Added)
        };

        var pattern = WatchEventProcessor.BuildFilterPattern(specs);

        // Should not throw when compiled as regex
        var regex = new System.Text.RegularExpressions.Regex(pattern);
        await Assert.That(regex).IsNotNull();
    }

    #endregion

    #region Helper Methods

    private static WatchEventProcessor CreateProcessor(
        ISpecChangeTracker? changeTracker = null,
        IStaticSpecParserFactory? parserFactory = null)
    {
        return new WatchEventProcessor(
            changeTracker ?? new MockSpecChangeTracker(),
            parserFactory ?? new MockStaticSpecParserFactory());
    }

    #endregion
}
