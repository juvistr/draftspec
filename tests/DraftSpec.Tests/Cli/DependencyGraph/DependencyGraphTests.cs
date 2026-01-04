using DraftSpec.Cli.DependencyGraph;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;
using DepGraph = DraftSpec.Cli.DependencyGraph.DependencyGraph;

namespace DraftSpec.Tests.Cli.DependencyGraph;

/// <summary>
/// Tests for DependencyGraph data structure.
/// </summary>
public class DependencyGraphTests
{
    // Path constants to avoid repetition
    private static string TestSpec => TestPaths.Spec("test.spec.csx");
    private static string HelperFile => TestPaths.Spec("helper.csx");
    private static string SpecA => TestPaths.Spec("a.spec.csx");
    private static string SpecB => TestPaths.Spec("b.spec.csx");
    private static string SpecC => TestPaths.Spec("c.spec.csx");
    private static string Helper1 => TestPaths.Spec("helper1.csx");
    private static string Helper2 => TestPaths.Spec("helper2.csx");
    private static string SrcTodoService => TestPaths.Temp("src/TodoService.cs");
    private static string SrcOtherService => TestPaths.Temp("src/OtherService.cs");
    private static string SrcServiceA => TestPaths.Temp("src/ServiceA.cs");
    private static string SrcServiceB => TestPaths.Temp("src/ServiceB.cs");

    #region AddSpec and GetDependencies

    [Test]
    public async Task AddSpec_StoresDependencies()
    {
        var graph = new DepGraph(new MockPathComparer());
        var spec = new SpecDependency(
            TestSpec,
            [HelperFile],
            ["MyApp.Services"]);

        graph.AddSpec(spec);

        var deps = graph.GetDependencies(TestSpec);
        await Assert.That(deps).Contains(HelperFile);
    }

    [Test]
    public async Task AddSpec_StoresNamespaces()
    {
        var graph = new DepGraph(new MockPathComparer());
        var spec = new SpecDependency(
            TestSpec,
            [],
            ["MyApp.Services", "MyApp.Models"]);

        graph.AddSpec(spec);

        var namespaces = graph.GetNamespaces(TestSpec);
        await Assert.That(namespaces).Contains("MyApp.Services");
        await Assert.That(namespaces).Contains("MyApp.Models");
    }

    [Test]
    public async Task SpecFiles_ReturnsAllRegisteredSpecs()
    {
        var graph = new DepGraph(new MockPathComparer());
        graph.AddSpec(new SpecDependency(SpecA, [], []));
        graph.AddSpec(new SpecDependency(SpecB, [], []));

        await Assert.That(graph.SpecFiles).Count().IsEqualTo(2);
    }

    [Test]
    public async Task SourceFiles_ReturnsAllUniqueSourceFiles()
    {
        var graph = new DepGraph(new MockPathComparer());

        // Add source files to different namespaces
        graph.AddNamespaceMapping(SrcTodoService, "MyApp.Services");
        graph.AddNamespaceMapping(SrcOtherService, "MyApp.Other");

        // Add the same file to multiple namespaces (should be deduplicated)
        graph.AddNamespaceMapping(SrcServiceA, "MyApp.Services");
        graph.AddNamespaceMapping(SrcServiceA, "MyApp.Shared");

        var sourceFiles = graph.SourceFiles;

        // Should have 3 unique files (SrcTodoService, SrcOtherService, SrcServiceA)
        await Assert.That(sourceFiles).Count().IsEqualTo(3);
        await Assert.That(sourceFiles).Contains(SrcTodoService);
        await Assert.That(sourceFiles).Contains(SrcOtherService);
        await Assert.That(sourceFiles).Contains(SrcServiceA);
    }

    [Test]
    public async Task SourceFiles_EmptyWhenNoMappings_ReturnsEmpty()
    {
        var graph = new DepGraph(new MockPathComparer());

        var sourceFiles = graph.SourceFiles;

        await Assert.That(sourceFiles).IsEmpty();
    }

    [Test]
    public async Task GetDependencies_UnknownSpec_ReturnsEmpty()
    {
        var graph = new DepGraph(new MockPathComparer());

        var deps = graph.GetDependencies(TestPaths.Spec("unknown.spec.csx"));

        await Assert.That(deps).IsEmpty();
    }

    [Test]
    public async Task GetNamespaces_UnknownSpec_ReturnsEmpty()
    {
        var graph = new DepGraph(new MockPathComparer());

        var namespaces = graph.GetNamespaces(TestPaths.Spec("unknown.spec.csx"));

        await Assert.That(namespaces).IsEmpty();
    }

    #endregion

    #region GetAffectedSpecs - Direct Dependencies

    [Test]
    public async Task GetAffectedSpecs_SpecFileChanged_ReturnsItself()
    {
        var graph = new DepGraph(new MockPathComparer());
        graph.AddSpec(new SpecDependency(TestSpec, [], []));

        var affected = graph.GetAffectedSpecs([TestSpec]);

        await Assert.That(affected).Contains(TestSpec);
    }

    [Test]
    public async Task GetAffectedSpecs_LoadDependencyChanged_ReturnsAffectedSpec()
    {
        var graph = new DepGraph(new MockPathComparer());
        graph.AddSpec(new SpecDependency(
            TestSpec,
            [HelperFile],
            []));

        var affected = graph.GetAffectedSpecs([HelperFile]);

        await Assert.That(affected).Contains(TestSpec);
    }

    [Test]
    public async Task GetAffectedSpecs_MultipleSpecsDependOnSameFile_ReturnsAll()
    {
        var graph = new DepGraph(new MockPathComparer());
        graph.AddSpec(new SpecDependency(SpecA, [HelperFile], []));
        graph.AddSpec(new SpecDependency(SpecB, [HelperFile], []));
        graph.AddSpec(new SpecDependency(SpecC, [], [])); // No dependency

        var affected = graph.GetAffectedSpecs([HelperFile]);

        await Assert.That(affected).Count().IsEqualTo(2);
        await Assert.That(affected).Contains(SpecA);
        await Assert.That(affected).Contains(SpecB);
    }

    [Test]
    public async Task GetAffectedSpecs_UnrelatedFileChanged_ReturnsEmpty()
    {
        var graph = new DepGraph(new MockPathComparer());
        graph.AddSpec(new SpecDependency(TestSpec, [HelperFile], []));

        var affected = graph.GetAffectedSpecs([TestPaths.Temp("some/other/file.txt")]);

        await Assert.That(affected).IsEmpty();
    }

    #endregion

    #region GetAffectedSpecs - Namespace Dependencies

    [Test]
    public async Task GetAffectedSpecs_SourceFileInUsedNamespace_ReturnsAffectedSpec()
    {
        var graph = new DepGraph(new MockPathComparer());
        graph.AddSpec(new SpecDependency(TestSpec, [], ["MyApp.Services"]));
        graph.AddNamespaceMapping(SrcTodoService, "MyApp.Services");

        var affected = graph.GetAffectedSpecs([SrcTodoService]);

        await Assert.That(affected).Contains(TestSpec);
    }

    [Test]
    public async Task GetAffectedSpecs_SourceFileInUnusedNamespace_ReturnsEmpty()
    {
        var graph = new DepGraph(new MockPathComparer());
        graph.AddSpec(new SpecDependency(TestSpec, [], ["MyApp.Services"]));
        graph.AddNamespaceMapping(SrcOtherService, "MyApp.Other");

        var affected = graph.GetAffectedSpecs([SrcOtherService]);

        await Assert.That(affected).IsEmpty();
    }

    [Test]
    public async Task GetAffectedSpecs_MultipleFilesInSameNamespace_AllAffectSpec()
    {
        var graph = new DepGraph(new MockPathComparer());
        graph.AddSpec(new SpecDependency(TestSpec, [], ["MyApp.Services"]));
        graph.AddNamespaceMapping(SrcServiceA, "MyApp.Services");
        graph.AddNamespaceMapping(SrcServiceB, "MyApp.Services");

        var affected1 = graph.GetAffectedSpecs([SrcServiceA]);
        var affected2 = graph.GetAffectedSpecs([SrcServiceB]);

        await Assert.That(affected1).Contains(TestSpec);
        await Assert.That(affected2).Contains(TestSpec);
    }

    #endregion

    #region GetAffectedSpecs - Multiple Changes

    [Test]
    public async Task GetAffectedSpecs_MultipleChanges_ReturnsCombinedResults()
    {
        var graph = new DepGraph(new MockPathComparer());
        graph.AddSpec(new SpecDependency(SpecA, [Helper1], []));
        graph.AddSpec(new SpecDependency(SpecB, [Helper2], []));

        var affected = graph.GetAffectedSpecs([Helper1, Helper2]);

        await Assert.That(affected).Count().IsEqualTo(2);
        await Assert.That(affected).Contains(SpecA);
        await Assert.That(affected).Contains(SpecB);
    }

    #endregion
}
