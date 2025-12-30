using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.TestingPlatform;

public class StaticSpecParserTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_static_{Guid.NewGuid():N}");
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

    [Test]
    public async Task ParseFileAsync_FindsSimpleSpecs()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;
            describe("Calculator", () =>
            {
                it("adds numbers", () => { });
                it("subtracts numbers", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "calc.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(2);
        await Assert.That(result.Specs[0].Description).IsEqualTo("adds numbers");
        await Assert.That(result.Specs[1].Description).IsEqualTo("subtracts numbers");
        await Assert.That(result.IsComplete).IsTrue();
    }

    [Test]
    public async Task ParseFileAsync_CapturesContextPath()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;
            describe("Parent", () =>
            {
                describe("Child", () =>
                {
                    it("grandchild spec", () => { });
                });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "nested.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(1);
        await Assert.That(result.Specs[0].ContextPath.Count).IsEqualTo(2);
        await Assert.That(result.Specs[0].ContextPath[0]).IsEqualTo("Parent");
        await Assert.That(result.Specs[0].ContextPath[1]).IsEqualTo("Child");
    }

    [Test]
    public async Task ParseFileAsync_IdentifiesFocusedSpecs()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;
            describe("Focused", () =>
            {
                it("normal spec", () => { });
                fit("focused spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "focused.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(2);
        await Assert.That(result.Specs[0].Type).IsEqualTo(StaticSpecType.Regular);
        await Assert.That(result.Specs[1].Type).IsEqualTo(StaticSpecType.Focused);
    }

    [Test]
    public async Task ParseFileAsync_IdentifiesSkippedSpecs()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;
            describe("Skipped", () =>
            {
                it("normal spec", () => { });
                xit("skipped spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "skipped.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(2);
        await Assert.That(result.Specs[0].Type).IsEqualTo(StaticSpecType.Regular);
        await Assert.That(result.Specs[1].Type).IsEqualTo(StaticSpecType.Skipped);
    }

    [Test]
    public async Task ParseFileAsync_IdentifiesPendingSpecs()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;
            describe("Pending", () =>
            {
                it("implemented spec", () => { });
                it("pending spec without body");
            });
            """;

        var csxPath = Path.Combine(_tempDir, "pending.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(2);
        await Assert.That(result.Specs[0].IsPending).IsFalse();
        await Assert.That(result.Specs[1].IsPending).IsTrue();
    }

    [Test]
    public async Task ParseFileAsync_CapturesLineNumbers()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;
            describe("Test", () =>
            {
                it("first spec", () => { });
                it("second spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "lines.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(2);
        await Assert.That(result.Specs[0].LineNumber).IsGreaterThan(0);
        await Assert.That(result.Specs[1].LineNumber).IsGreaterThan(result.Specs[0].LineNumber);
    }

    [Test]
    public async Task ParseFileAsync_ParsesFileWithCompilationError()
    {
        // Arrange - file with invalid code
        var csxContent = """
            using static DraftSpec.Dsl;
            describe("Broken", () =>
            {
                it("has error", () =>
                {
                    thisMethodDoesNotExist();
                });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "broken.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert - should still find the spec structure
        await Assert.That(result.Specs.Count).IsEqualTo(1);
        await Assert.That(result.Specs[0].Description).IsEqualTo("has error");
        await Assert.That(result.Specs[0].ContextPath[0]).IsEqualTo("Broken");
    }

    [Test]
    public async Task ParseFileAsync_WarnsOnDynamicDescriptions()
    {
        // Arrange - file with interpolated string description
        var csxContent = """
            using static DraftSpec.Dsl;
            var name = "test";
            describe("Static context", () =>
            {
                it($"dynamic {name}", () => { });
                it("static spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "dynamic.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert - should find the static spec and warn about dynamic one
        await Assert.That(result.Specs.Count).IsEqualTo(1);
        await Assert.That(result.Specs[0].Description).IsEqualTo("static spec");
        await Assert.That(result.IsComplete).IsFalse();
        await Assert.That(result.Warnings.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task ParseFileAsync_HandlesLoadDirectives()
    {
        // Arrange
        var helperContent = """
            using static DraftSpec.Dsl;
            // Helper file
            """;

        var helperPath = Path.Combine(_tempDir, "helper.csx");
        await File.WriteAllTextAsync(helperPath, helperContent);

        var csxContent = """
            #load "helper.csx"
            using static DraftSpec.Dsl;
            describe("Main", () =>
            {
                it("uses helper", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "main.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(1);
        await Assert.That(result.Specs[0].Description).IsEqualTo("uses helper");
    }

    [Test]
    public async Task ParseFileAsync_HandlesContextAlias()
    {
        // Arrange - context is an alias for describe
        var csxContent = """
            using static DraftSpec.Dsl;
            context("Feature", () =>
            {
                it("works", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "context.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(1);
        await Assert.That(result.Specs[0].ContextPath[0]).IsEqualTo("Feature");
    }

    [Test]
    public async Task ParseFileAsync_FileNotFound_ReturnsEmptyWithWarning()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir, "nonexistent.spec.csx");
        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(nonExistentPath);

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(0);
        await Assert.That(result.IsComplete).IsFalse();
        await Assert.That(result.Warnings.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task ParseFileAsync_CircularLoadDirective_DoesNotLoop()
    {
        // Arrange - create two files that load each other
        var fileAContent = """
            #load "b.csx"
            using static DraftSpec.Dsl;
            describe("FileA", () =>
            {
                it("spec in A", () => { });
            });
            """;

        var fileBContent = """
            #load "a.csx"
            using static DraftSpec.Dsl;
            describe("FileB", () =>
            {
                it("spec in B", () => { });
            });
            """;

        var fileAPath = Path.Combine(_tempDir, "a.csx");
        var fileBPath = Path.Combine(_tempDir, "b.csx");
        await File.WriteAllTextAsync(fileAPath, fileAContent);
        await File.WriteAllTextAsync(fileBPath, fileBContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act - should not infinite loop
        var result = await parser.ParseFileAsync(fileAPath);

        // Assert - should parse specs from both files, no stack overflow
        await Assert.That(result.Specs.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ParseFileAsync_RDirective_IsRemoved()
    {
        // Arrange - file with #r reference directive
        var csxContent = """
            #r "nuget: Newtonsoft.Json, 13.0.1"
            using static DraftSpec.Dsl;
            describe("WithNugetRef", () =>
            {
                it("works without nuget resolution", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "nuget.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert - should find the spec despite #r directive
        await Assert.That(result.Specs.Count).IsEqualTo(1);
        await Assert.That(result.Specs[0].Description).IsEqualTo("works without nuget resolution");
    }

    [Test]
    public async Task ParseFileAsync_MalformedLoadDirective_IsHandledGracefully()
    {
        // Arrange - file with malformed #load
        var csxContent = """
            #load "nonexistent_file.csx"
            using static DraftSpec.Dsl;
            describe("StillParses", () =>
            {
                it("even with missing load", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "missing_load.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act - should not throw even though loaded file doesn't exist
        var result = await parser.ParseFileAsync(csxPath);

        // Assert - should still find the spec in the main file
        await Assert.That(result.Specs.Count).IsEqualTo(1);
        await Assert.That(result.Specs[0].Description).IsEqualTo("even with missing load");
    }

    [Test]
    public async Task ParseFileAsync_EmptyFile_ReturnsEmptySpecs()
    {
        // Arrange
        var csxPath = Path.Combine(_tempDir, "empty.spec.csx");
        await File.WriteAllTextAsync(csxPath, "");

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(0);
        await Assert.That(result.IsComplete).IsTrue();
    }

    [Test]
    public async Task ParseFileAsync_MultipleRDirectives_AllRemoved()
    {
        // Arrange - file with multiple #r directives
        var csxContent = """
            #r "nuget: Newtonsoft.Json, 13.0.1"
            #r "nuget: Moq, 4.18.0"
            #r "path/to/some.dll"
            using static DraftSpec.Dsl;
            describe("MultiRef", () =>
            {
                it("still parses", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "multi_r.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var parser = new StaticSpecParser(_tempDir);

        // Act
        var result = await parser.ParseFileAsync(csxPath);

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(1);
        await Assert.That(result.IsComplete).IsTrue();
    }
}
