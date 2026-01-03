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
