using DraftSpec.Cli.Services;

namespace DraftSpec.Tests.Cli.Services;

/// <summary>
/// Tests for <see cref="CoverageMapService"/>.
/// </summary>
public class CoverageMapServiceTests
{
    private readonly string _tempDir;

    public CoverageMapServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"CoverageMapServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Directive Stripping Tests

    [Test]
    public async Task ComputeCoverageAsync_SpecWithLoadDirective_StripsDirective()
    {
        // Arrange
        var service = new CoverageMapService();
        var sourceFile = CreateSourceFile("public class Foo { public void Bar() { } }");
        var specFile = CreateSpecFile(@"#load ""spec_helper.csx""
using static DraftSpec.Dsl;
describe(""Foo"", () => { it(""works"", () => { }); });");

        // Act - should not throw despite #load directive
        var result = await service.ComputeCoverageAsync(
            [sourceFile],
            [specFile],
            _tempDir);

        // Assert - parsed successfully (directive was stripped)
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task ComputeCoverageAsync_SpecWithReferenceDirective_StripsDirective()
    {
        // Arrange
        var service = new CoverageMapService();
        var sourceFile = CreateSourceFile("public class Foo { public void Bar() { } }");
        var specFile = CreateSpecFile(@"#r ""nuget: SomePackage, 1.0.0""
using static DraftSpec.Dsl;
describe(""Foo"", () => { it(""works"", () => { }); });");

        // Act - should not throw despite #r directive
        var result = await service.ComputeCoverageAsync(
            [sourceFile],
            [specFile],
            _tempDir);

        // Assert - parsed successfully (directive was stripped)
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task ComputeCoverageAsync_SpecWithBothDirectives_StripsAllDirectives()
    {
        // Arrange
        var service = new CoverageMapService();
        var sourceFile = CreateSourceFile("public class Foo { public void Bar() { } }");
        var specFile = CreateSpecFile(@"#load ""spec_helper.csx""
#r ""nuget: SomePackage, 1.0.0""
using static DraftSpec.Dsl;
describe(""Foo"", () => { it(""works"", () => { }); });");

        // Act - should not throw despite both directives
        var result = await service.ComputeCoverageAsync(
            [sourceFile],
            [specFile],
            _tempDir);

        // Assert - parsed successfully (both directives were stripped)
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task ComputeCoverageAsync_SpecWithIndentedDirectives_StripsDirectives()
    {
        // Arrange
        var service = new CoverageMapService();
        var sourceFile = CreateSourceFile("public class Foo { public void Bar() { } }");
        var specFile = CreateSpecFile(@"  #load ""spec_helper.csx""
    #r ""nuget: SomePackage, 1.0.0""
using static DraftSpec.Dsl;
describe(""Foo"", () => { it(""works"", () => { }); });");

        // Act - should strip even indented directives
        var result = await service.ComputeCoverageAsync(
            [sourceFile],
            [specFile],
            _tempDir);

        // Assert
        await Assert.That(result).IsNotNull();
    }

    #endregion

    #region Namespace Filter Tests

    [Test]
    public async Task ComputeCoverageAsync_WithNamespaceFilter_FiltersToMatchingMethods()
    {
        // Arrange
        var service = new CoverageMapService();
        var sourceFile = CreateSourceFile(@"
namespace MyApp.Services {
    public class UserService { public void CreateUser() { } }
}
namespace MyApp.Controllers {
    public class UserController { public void Index() { } }
}
namespace OtherApp {
    public class OtherService { public void DoSomething() { } }
}");
        var specFile = CreateSpecFile(@"describe(""Test"", () => { it(""works"", () => { }); });");

        // Act
        var result = await service.ComputeCoverageAsync(
            [sourceFile],
            [specFile],
            _tempDir,
            namespaceFilter: "MyApp.Services");

        // Assert - only MyApp.Services methods should be included
        await Assert.That(result.AllMethods.Count).IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Method.Namespace).IsEqualTo("MyApp.Services");
    }

    [Test]
    public async Task ComputeCoverageAsync_WithMultipleNamespaceFilters_FiltersToAllMatching()
    {
        // Arrange
        var service = new CoverageMapService();
        var sourceFile = CreateSourceFile(@"
namespace MyApp.Services {
    public class UserService { public void CreateUser() { } }
}
namespace MyApp.Controllers {
    public class UserController { public void Index() { } }
}
namespace OtherApp {
    public class OtherService { public void DoSomething() { } }
}");
        var specFile = CreateSpecFile(@"describe(""Test"", () => { it(""works"", () => { }); });");

        // Act - filter by two namespaces (comma-separated)
        var result = await service.ComputeCoverageAsync(
            [sourceFile],
            [specFile],
            _tempDir,
            namespaceFilter: "MyApp.Services, MyApp.Controllers");

        // Assert - both MyApp.Services and MyApp.Controllers methods should be included
        await Assert.That(result.AllMethods.Count).IsEqualTo(2);
        var namespaces = result.AllMethods.Select(m => m.Method.Namespace).ToList();
        await Assert.That(namespaces).Contains("MyApp.Services");
        await Assert.That(namespaces).Contains("MyApp.Controllers");
    }

    #endregion

    #region Helper Methods

    private string CreateSourceFile(string content, string? fileName = null)
    {
        fileName ??= $"Source_{Guid.NewGuid():N}.cs";
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    private string CreateSpecFile(string content, string? fileName = null)
    {
        fileName ??= $"Spec_{Guid.NewGuid():N}.spec.csx";
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    #endregion
}
