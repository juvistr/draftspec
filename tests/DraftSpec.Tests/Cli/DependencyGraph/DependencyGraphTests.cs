using DraftSpec.Cli.DependencyGraph;
using DepGraph = DraftSpec.Cli.DependencyGraph.DependencyGraph;

namespace DraftSpec.Tests.Cli.DependencyGraph;

/// <summary>
/// Tests for DependencyGraph data structure.
/// </summary>
public class DependencyGraphTests
{
    #region AddSpec and GetDependencies

    [Test]
    public async Task AddSpec_StoresDependencies()
    {
        var graph = new DepGraph();
        var spec = new SpecDependency(
            "/specs/test.spec.csx",
            ["/specs/helper.csx"],
            ["MyApp.Services"]);

        graph.AddSpec(spec);

        var deps = graph.GetDependencies("/specs/test.spec.csx");
        await Assert.That(deps).Contains("/specs/helper.csx");
    }

    [Test]
    public async Task AddSpec_StoresNamespaces()
    {
        var graph = new DepGraph();
        var spec = new SpecDependency(
            "/specs/test.spec.csx",
            [],
            ["MyApp.Services", "MyApp.Models"]);

        graph.AddSpec(spec);

        var namespaces = graph.GetNamespaces("/specs/test.spec.csx");
        await Assert.That(namespaces).Contains("MyApp.Services");
        await Assert.That(namespaces).Contains("MyApp.Models");
    }

    [Test]
    public async Task SpecFiles_ReturnsAllRegisteredSpecs()
    {
        var graph = new DepGraph();
        graph.AddSpec(new SpecDependency("/specs/a.spec.csx", [], []));
        graph.AddSpec(new SpecDependency("/specs/b.spec.csx", [], []));

        await Assert.That(graph.SpecFiles).Count().IsEqualTo(2);
    }

    #endregion

    #region GetAffectedSpecs - Direct Dependencies

    [Test]
    public async Task GetAffectedSpecs_SpecFileChanged_ReturnsItself()
    {
        var graph = new DepGraph();
        graph.AddSpec(new SpecDependency("/specs/test.spec.csx", [], []));

        var affected = graph.GetAffectedSpecs(["/specs/test.spec.csx"]);

        await Assert.That(affected).Contains("/specs/test.spec.csx");
    }

    [Test]
    public async Task GetAffectedSpecs_LoadDependencyChanged_ReturnsAffectedSpec()
    {
        var graph = new DepGraph();
        graph.AddSpec(new SpecDependency(
            "/specs/test.spec.csx",
            ["/specs/helper.csx"],
            []));

        var affected = graph.GetAffectedSpecs(["/specs/helper.csx"]);

        await Assert.That(affected).Contains("/specs/test.spec.csx");
    }

    [Test]
    public async Task GetAffectedSpecs_MultipleSpecsDependOnSameFile_ReturnsAll()
    {
        var graph = new DepGraph();
        graph.AddSpec(new SpecDependency("/specs/a.spec.csx", ["/specs/helper.csx"], []));
        graph.AddSpec(new SpecDependency("/specs/b.spec.csx", ["/specs/helper.csx"], []));
        graph.AddSpec(new SpecDependency("/specs/c.spec.csx", [], [])); // No dependency

        var affected = graph.GetAffectedSpecs(["/specs/helper.csx"]);

        await Assert.That(affected).Count().IsEqualTo(2);
        await Assert.That(affected).Contains("/specs/a.spec.csx");
        await Assert.That(affected).Contains("/specs/b.spec.csx");
    }

    [Test]
    public async Task GetAffectedSpecs_UnrelatedFileChanged_ReturnsEmpty()
    {
        var graph = new DepGraph();
        graph.AddSpec(new SpecDependency("/specs/test.spec.csx", ["/specs/helper.csx"], []));

        var affected = graph.GetAffectedSpecs(["/some/other/file.txt"]);

        await Assert.That(affected).IsEmpty();
    }

    #endregion

    #region GetAffectedSpecs - Namespace Dependencies

    [Test]
    public async Task GetAffectedSpecs_SourceFileInUsedNamespace_ReturnsAffectedSpec()
    {
        var graph = new DepGraph();
        graph.AddSpec(new SpecDependency("/specs/test.spec.csx", [], ["MyApp.Services"]));
        graph.AddNamespaceMapping("/src/TodoService.cs", "MyApp.Services");

        var affected = graph.GetAffectedSpecs(["/src/TodoService.cs"]);

        await Assert.That(affected).Contains("/specs/test.spec.csx");
    }

    [Test]
    public async Task GetAffectedSpecs_SourceFileInUnusedNamespace_ReturnsEmpty()
    {
        var graph = new DepGraph();
        graph.AddSpec(new SpecDependency("/specs/test.spec.csx", [], ["MyApp.Services"]));
        graph.AddNamespaceMapping("/src/OtherService.cs", "MyApp.Other");

        var affected = graph.GetAffectedSpecs(["/src/OtherService.cs"]);

        await Assert.That(affected).IsEmpty();
    }

    [Test]
    public async Task GetAffectedSpecs_MultipleFilesInSameNamespace_AllAffectSpec()
    {
        var graph = new DepGraph();
        graph.AddSpec(new SpecDependency("/specs/test.spec.csx", [], ["MyApp.Services"]));
        graph.AddNamespaceMapping("/src/ServiceA.cs", "MyApp.Services");
        graph.AddNamespaceMapping("/src/ServiceB.cs", "MyApp.Services");

        var affected1 = graph.GetAffectedSpecs(["/src/ServiceA.cs"]);
        var affected2 = graph.GetAffectedSpecs(["/src/ServiceB.cs"]);

        await Assert.That(affected1).Contains("/specs/test.spec.csx");
        await Assert.That(affected2).Contains("/specs/test.spec.csx");
    }

    #endregion

    #region GetAffectedSpecs - Multiple Changes

    [Test]
    public async Task GetAffectedSpecs_MultipleChanges_ReturnsCombinedResults()
    {
        var graph = new DepGraph();
        graph.AddSpec(new SpecDependency("/specs/a.spec.csx", ["/specs/helper1.csx"], []));
        graph.AddSpec(new SpecDependency("/specs/b.spec.csx", ["/specs/helper2.csx"], []));

        var affected = graph.GetAffectedSpecs(["/specs/helper1.csx", "/specs/helper2.csx"]);

        await Assert.That(affected).Count().IsEqualTo(2);
        await Assert.That(affected).Contains("/specs/a.spec.csx");
        await Assert.That(affected).Contains("/specs/b.spec.csx");
    }

    #endregion
}
