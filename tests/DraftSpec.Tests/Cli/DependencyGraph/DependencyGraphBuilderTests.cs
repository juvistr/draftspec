using DraftSpec.Cli.DependencyGraph;

namespace DraftSpec.Tests.Cli.DependencyGraph;

/// <summary>
/// Tests for DependencyGraphBuilder.
/// </summary>
public class DependencyGraphBuilderTests : IDisposable
{
    private readonly string _tempDir;

    public DependencyGraphBuilderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    #region Basic Building

    [Test]
    public async Task BuildAsync_EmptyDirectory_ReturnsEmptyGraph()
    {
        var builder = new DependencyGraphBuilder();

        var graph = await builder.BuildAsync(_tempDir);

        await Assert.That(graph.SpecFiles).IsEmpty();
    }

    [Test]
    public async Task BuildAsync_SingleSpecFile_AddsToGraph()
    {
        await CreateFileAsync("test.spec.csx", """
            describe("Test", () => { });
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(_tempDir);

        await Assert.That(graph.SpecFiles).Count().IsEqualTo(1);
    }

    [Test]
    public async Task BuildAsync_MultipleSpecFiles_AddsAllToGraph()
    {
        await CreateFileAsync("a.spec.csx", "describe(\"A\", () => { });");
        await CreateFileAsync("b.spec.csx", "describe(\"B\", () => { });");

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(_tempDir);

        await Assert.That(graph.SpecFiles).Count().IsEqualTo(2);
    }

    #endregion

    #region Load Directive Parsing

    [Test]
    public async Task BuildAsync_ExtractsLoadDependencies()
    {
        await CreateFileAsync("helper.csx", "var helper = true;");
        await CreateFileAsync("test.spec.csx", """
            #load "helper.csx"
            describe("Test", () => { });
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(_tempDir);

        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        var helperPath = Path.Combine(_tempDir, "helper.csx");

        var deps = graph.GetDependencies(specPath);
        await Assert.That(deps).Contains(helperPath);
    }

    [Test]
    public async Task BuildAsync_ExtractsTransitiveLoadDependencies()
    {
        await CreateFileAsync("level2.csx", "var level2 = true;");
        await CreateFileAsync("level1.csx", """
            #load "level2.csx"
            var level1 = true;
            """);
        await CreateFileAsync("test.spec.csx", """
            #load "level1.csx"
            describe("Test", () => { });
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(_tempDir);

        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        var level1Path = Path.Combine(_tempDir, "level1.csx");
        var level2Path = Path.Combine(_tempDir, "level2.csx");

        var deps = graph.GetDependencies(specPath);
        await Assert.That(deps).Contains(level1Path);
        await Assert.That(deps).Contains(level2Path);
    }

    [Test]
    public async Task BuildAsync_HandlesCircularLoadReferences()
    {
        await CreateFileAsync("a.csx", """
            #load "b.csx"
            var a = true;
            """);
        await CreateFileAsync("b.csx", """
            #load "a.csx"
            var b = true;
            """);
        await CreateFileAsync("test.spec.csx", """
            #load "a.csx"
            describe("Test", () => { });
            """);

        var builder = new DependencyGraphBuilder();

        // Should not throw or infinite loop
        var graph = await builder.BuildAsync(_tempDir);

        await Assert.That(graph.SpecFiles).Count().IsEqualTo(1);
    }

    #endregion

    #region Using Directive Parsing

    [Test]
    public async Task BuildAsync_ExtractsUsingDirectives()
    {
        await CreateFileAsync("test.spec.csx", """
            using MyApp.Services;
            using MyApp.Models;
            describe("Test", () => { });
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(_tempDir);

        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        var namespaces = graph.GetNamespaces(specPath);

        await Assert.That(namespaces).Contains("MyApp.Services");
        await Assert.That(namespaces).Contains("MyApp.Models");
    }

    [Test]
    public async Task BuildAsync_ExtractsStaticUsingDirectives()
    {
        await CreateFileAsync("test.spec.csx", """
            using static MyApp.Helpers.MathUtils;
            describe("Test", () => { });
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(_tempDir);

        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        var namespaces = graph.GetNamespaces(specPath);

        await Assert.That(namespaces).Contains("MyApp.Helpers.MathUtils");
    }

    [Test]
    public async Task BuildAsync_FiltersOutSystemNamespaces()
    {
        await CreateFileAsync("test.spec.csx", """
            using System;
            using System.Collections.Generic;
            using Microsoft.Extensions.Logging;
            using MyApp.Services;
            describe("Test", () => { });
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(_tempDir);

        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        var namespaces = graph.GetNamespaces(specPath);

        await Assert.That(namespaces).Contains("MyApp.Services");
        await Assert.That(namespaces).DoesNotContain("System");
        await Assert.That(namespaces).DoesNotContain("System.Collections.Generic");
        await Assert.That(namespaces).DoesNotContain("Microsoft.Extensions.Logging");
    }

    #endregion

    #region Namespace Mapping from Source Files

    [Test]
    public async Task BuildAsync_MapsNamespacesFromSourceFiles()
    {
        var srcDir = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(srcDir);

        await CreateFileAsync("src/TodoService.cs", """
            namespace MyApp.Services;
            public class TodoService { }
            """);
        await CreateFileAsync("test.spec.csx", """
            using MyApp.Services;
            describe("Test", () => { });
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(_tempDir, srcDir);

        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        var sourcePath = Path.Combine(srcDir, "TodoService.cs");

        // When the source file changes, the spec should be affected
        var affected = graph.GetAffectedSpecs([sourcePath]);
        await Assert.That(affected).Contains(specPath);
    }

    [Test]
    public async Task BuildAsync_HandlesTraditionalNamespaceSyntax()
    {
        var srcDir = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(srcDir);

        await CreateFileAsync("src/LegacyService.cs", """
            namespace MyApp.Legacy
            {
                public class LegacyService { }
            }
            """);
        await CreateFileAsync("test.spec.csx", """
            using MyApp.Legacy;
            describe("Test", () => { });
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(_tempDir, srcDir);

        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        var sourcePath = Path.Combine(srcDir, "LegacyService.cs");

        var affected = graph.GetAffectedSpecs([sourcePath]);
        await Assert.That(affected).Contains(specPath);
    }

    [Test]
    public async Task BuildAsync_SkipsGeneratedFiles()
    {
        var srcDir = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(srcDir);

        await CreateFileAsync("src/Generated.g.cs", """
            namespace MyApp.Generated;
            public class Generated { }
            """);
        await CreateFileAsync("src/Designer.designer.cs", """
            namespace MyApp.Designer;
            public class Designer { }
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(_tempDir, srcDir);

        // Generated files should not be in source files
        await Assert.That(graph.SourceFiles).IsEmpty();
    }

    #endregion

    #region Subdirectory Handling

    [Test]
    public async Task BuildAsync_FindsSpecsInSubdirectories()
    {
        var subDir = Path.Combine(_tempDir, "specs", "unit");
        Directory.CreateDirectory(subDir);

        await CreateFileAsync("specs/unit/test.spec.csx", """
            describe("Test", () => { });
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(_tempDir);

        await Assert.That(graph.SpecFiles).Count().IsEqualTo(1);
    }

    #endregion

    #region FindSourceDirectory (Automatic Source Detection)

    [Test]
    public async Task BuildAsync_FindsSiblingSrcDirectory()
    {
        // Structure: specs/ next to src/
        var specsDir = Path.Combine(_tempDir, "specs");
        var srcDir = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(specsDir);
        Directory.CreateDirectory(srcDir);

        await CreateFileAsync("specs/test.spec.csx", """
            using MyApp.Services;
            describe("Test", () => { });
            """);
        await CreateFileAsync("src/TodoService.cs", """
            namespace MyApp.Services;
            public class TodoService { }
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(specsDir); // No explicit sourceDirectory

        var specPath = Path.Combine(specsDir, "test.spec.csx");
        var sourcePath = Path.Combine(srcDir, "TodoService.cs");

        // Should automatically find src/ and map the namespace
        var affected = graph.GetAffectedSpecs([sourcePath]);
        await Assert.That(affected).Contains(specPath);
    }

    [Test]
    public async Task BuildAsync_FindsAdjacentProjectDirectory_WhenSpecsHasSuffix()
    {
        // Structure: TodoApi.Specs/ next to TodoApi/
        var projectDir = Path.Combine(_tempDir, "TodoApi");
        var specsDir = Path.Combine(_tempDir, "TodoApi.Specs");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(specsDir);

        await CreateFileAsync("TodoApi.Specs/test.spec.csx", """
            using TodoApi.Services;
            describe("Test", () => { });
            """);
        await CreateFileAsync("TodoApi/TodoService.cs", """
            namespace TodoApi.Services;
            public class TodoService { }
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(specsDir); // No explicit sourceDirectory

        var specPath = Path.Combine(specsDir, "test.spec.csx");
        var sourcePath = Path.Combine(projectDir, "TodoService.cs");

        // Should automatically find TodoApi/ and map the namespace
        var affected = graph.GetAffectedSpecs([sourcePath]);
        await Assert.That(affected).Contains(specPath);
    }

    [Test]
    public async Task BuildAsync_FindsParentDirectory_WhenHasCsFiles()
    {
        // Structure: specs/ inside a project with .cs files in parent
        var specsDir = Path.Combine(_tempDir, "specs");
        Directory.CreateDirectory(specsDir);

        await CreateFileAsync("specs/test.spec.csx", """
            using MyApp.Services;
            describe("Test", () => { });
            """);
        await CreateFileAsync("TodoService.cs", """
            namespace MyApp.Services;
            public class TodoService { }
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(specsDir); // No explicit sourceDirectory

        var specPath = Path.Combine(specsDir, "test.spec.csx");
        var sourcePath = Path.Combine(_tempDir, "TodoService.cs");

        // Should find parent directory and map the namespace
        var affected = graph.GetAffectedSpecs([sourcePath]);
        await Assert.That(affected).Contains(specPath);
    }

    [Test]
    public async Task BuildAsync_WithExplicitSourceDirectory_UsesProvided()
    {
        var specsDir = Path.Combine(_tempDir, "specs");
        var customSrcDir = Path.Combine(_tempDir, "custom-src");
        Directory.CreateDirectory(specsDir);
        Directory.CreateDirectory(customSrcDir);

        await CreateFileAsync("specs/test.spec.csx", """
            using MyApp.Services;
            describe("Test", () => { });
            """);
        await CreateFileAsync("custom-src/TodoService.cs", """
            namespace MyApp.Services;
            public class TodoService { }
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(specsDir, customSrcDir); // Explicit sourceDirectory

        var specPath = Path.Combine(specsDir, "test.spec.csx");
        var sourcePath = Path.Combine(customSrcDir, "TodoService.cs");

        var affected = graph.GetAffectedSpecs([sourcePath]);
        await Assert.That(affected).Contains(specPath);
    }

    [Test]
    public async Task BuildAsync_WithNoSourceDirectory_BuildsWithoutNamespaces()
    {
        // Structure: isolated specs directory with no adjacent source
        var specsDir = Path.Combine(_tempDir, "isolated", "specs");
        Directory.CreateDirectory(specsDir);

        await CreateFileAsync("isolated/specs/test.spec.csx", """
            using MyApp.Services;
            describe("Test", () => { });
            """);

        var builder = new DependencyGraphBuilder();
        var graph = await builder.BuildAsync(specsDir); // No explicit sourceDirectory

        // Should still work, just without namespace mappings
        await Assert.That(graph.SpecFiles).Count().IsEqualTo(1);
        await Assert.That(graph.SourceFiles).IsEmpty();
    }

    #endregion

    private async Task CreateFileAsync(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(fullPath, content);
    }
}
