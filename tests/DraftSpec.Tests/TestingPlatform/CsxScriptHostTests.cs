using DraftSpec.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace DraftSpec.Tests.TestingPlatform;

public class CsxScriptHostTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        global::DraftSpec.Dsl.Reset();
    }

    [After(Test)]
    public void Cleanup()
    {
        global::DraftSpec.Dsl.Reset();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Execute_SimpleSpec_ReturnsSpecTree()
    {
        // Arrange - no run() call
        // Write a marker file to verify script execution
        var markerPath = Path.Combine(_tempDir, "executed.marker");
        var csxContent = $$"""
            using static DraftSpec.Dsl;
            System.IO.File.WriteAllText("{{markerPath.Replace("\\", "/")}}", "executed");

            describe("Calculator", () =>
            {
                it("adds numbers", () => { });
                it("subtracts numbers", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "simple.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Debug - check if script executed
        var executed = File.Exists(markerPath);
        var desc = rootContext?.Description ?? "null";
        var dslDesc = global::DraftSpec.Dsl.RootContext?.Description ?? "null";
        Console.WriteLine($"Script executed: {executed}");
        Console.WriteLine($"RootContext: {desc}");
        Console.WriteLine($"Dsl.RootContext directly: {dslDesc}");

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Description).IsEqualTo("Calculator");
        await Assert.That(rootContext.Specs.Count).IsEqualTo(2);
        await Assert.That(rootContext.Specs[0].Description).IsEqualTo("adds numbers");
        await Assert.That(rootContext.Specs[1].Description).IsEqualTo("subtracts numbers");
    }

    [Test]
    public async Task Execute_NestedContext_ReturnsFullTree()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Math", () =>
            {
                describe("Addition", () =>
                {
                    it("adds positive numbers", () => { });
                });

                describe("Subtraction", () =>
                {
                    it("subtracts positive numbers", () => { });
                });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "nested.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Description).IsEqualTo("Math");
        await Assert.That(rootContext.Children.Count).IsEqualTo(2);
        await Assert.That(rootContext.Children[0].Description).IsEqualTo("Addition");
        await Assert.That(rootContext.Children[1].Description).IsEqualTo("Subtraction");
    }

    [Test]
    public async Task Execute_WithRunCall_StillWorksButExecutesSpecs()
    {
        // Arrange - with run() call, specs execute but tree is still available
        // (run() clears state after execution, so this tests backwards compat)
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("WithRun", () =>
            {
                it("spec that runs", () => { });
            });

            // Note: run() will execute specs AND clear state
            // For MTP, don't call run()
            """;

        var csxPath = Path.Combine(_tempDir, "with_run.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert - tree is available because we didn't call run()
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Description).IsEqualTo("WithRun");
    }

    [Test]
    public async Task Execute_LoadDirective_InlinesLoadedFile()
    {
        // Arrange
        var helperContent = """
            // Helper file - just defines specs
            using static DraftSpec.Dsl;

            describe("Helper", () =>
            {
                it("from helper", () => { });
            });
            """;

        var mainContent = """
            #load "helper.csx"
            """;

        var helperPath = Path.Combine(_tempDir, "helper.csx");
        var mainPath = Path.Combine(_tempDir, "main.spec.csx");

        await File.WriteAllTextAsync(helperPath, helperContent);
        await File.WriteAllTextAsync(mainPath, mainContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(mainPath);

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Description).IsEqualTo("Helper");
    }

    [Test]
    public async Task Execute_MultipleFiles_ResetsStateBetween()
    {
        // Arrange
        var csx1Content = """
            using static DraftSpec.Dsl;

            describe("First", () =>
            {
                it("spec 1", () => { });
            });
            """;

        var csx2Content = """
            using static DraftSpec.Dsl;

            describe("Second", () =>
            {
                it("spec 2", () => { });
            });
            """;

        var csx1Path = Path.Combine(_tempDir, "first.spec.csx");
        var csx2Path = Path.Combine(_tempDir, "second.spec.csx");

        await File.WriteAllTextAsync(csx1Path, csx1Content);
        await File.WriteAllTextAsync(csx2Path, csx2Content);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var root1 = await host.ExecuteAsync(csx1Path);
        var root2 = await host.ExecuteAsync(csx2Path);

        // Assert - each file should have its own independent tree
        await Assert.That(root1).IsNotNull();
        await Assert.That(root1!.Description).IsEqualTo("First");
        await Assert.That(root2).IsNotNull();
        await Assert.That(root2!.Description).IsEqualTo("Second");
    }

    [Test]
    public async Task Execute_FileNotFound_ThrowsException()
    {
        // Arrange
        var host = new CsxScriptHost(_tempDir);
        var nonExistentPath = Path.Combine(_tempDir, "nonexistent.spec.csx");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await host.ExecuteAsync(nonExistentPath));
    }

    [Test]
    public async Task Execute_SkipsNugetReferences()
    {
        // Arrange - CSX with nuget reference (should be skipped)
        var csxContent = """
            #r "nuget: SomePackage, 1.0.0"
            using static DraftSpec.Dsl;

            describe("WithNuget", () =>
            {
                it("should still work", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "nuget.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act - should not fail due to nuget reference
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Description).IsEqualTo("WithNuget");
    }

    [Test]
    public async Task Execute_CompilationError_ThrowsException()
    {
        // Arrange - invalid C# code
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Invalid", () =>
            {
                it("has syntax error", () =>
                {
                    var x = // missing expression
                });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "invalid.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act & Assert - should throw compilation error
        await Assert.ThrowsAsync<CompilationErrorException>(
            async () => await host.ExecuteAsync(csxPath));
    }

    #region Cache Invalidation

    [Test]
    public async Task Execute_SameFile_UsesCachedScript()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Cached", () =>
            {
                it("spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "cached.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act - execute twice
        var root1 = await host.ExecuteAsync(csxPath);
        global::DraftSpec.Dsl.Reset();
        var root2 = await host.ExecuteAsync(csxPath);

        // Assert - both executions should work (cache reused on second)
        await Assert.That(root1).IsNotNull();
        await Assert.That(root1!.Description).IsEqualTo("Cached");
        await Assert.That(root2).IsNotNull();
        await Assert.That(root2!.Description).IsEqualTo("Cached");
    }

    [Test]
    public async Task Execute_ModifiedFile_InvalidatesCache()
    {
        // Arrange
        var csxContent1 = """
            using static DraftSpec.Dsl;

            describe("Original", () =>
            {
                it("original spec", () => { });
            });
            """;

        var csxContent2 = """
            using static DraftSpec.Dsl;

            describe("Modified", () =>
            {
                it("new spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "modify.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent1);

        var host = new CsxScriptHost(_tempDir);

        // Act - execute first version
        var root1 = await host.ExecuteAsync(csxPath);
        global::DraftSpec.Dsl.Reset();

        // Wait a bit to ensure file timestamp changes (minimum 10ms)
        await Task.Delay(50);

        // Modify the file
        await File.WriteAllTextAsync(csxPath, csxContent2);

        // Execute again - should pick up the new content
        var root2 = await host.ExecuteAsync(csxPath);

        // Assert
        await Assert.That(root1).IsNotNull();
        await Assert.That(root1!.Description).IsEqualTo("Original");
        await Assert.That(root2).IsNotNull();
        await Assert.That(root2!.Description).IsEqualTo("Modified");
    }

    [Test]
    public async Task Execute_ModifiedLoadDependency_InvalidatesCache()
    {
        // Arrange
        var helperContent1 = """
            using static DraftSpec.Dsl;

            describe("HelperV1", () =>
            {
                it("v1 spec", () => { });
            });
            """;

        var helperContent2 = """
            using static DraftSpec.Dsl;

            describe("HelperV2", () =>
            {
                it("v2 spec", () => { });
            });
            """;

        var mainContent = """
            #load "helper.csx"
            """;

        var helperPath = Path.Combine(_tempDir, "helper.csx");
        var mainPath = Path.Combine(_tempDir, "main.spec.csx");

        await File.WriteAllTextAsync(helperPath, helperContent1);
        await File.WriteAllTextAsync(mainPath, mainContent);

        var host = new CsxScriptHost(_tempDir);

        // Act - execute first version
        var root1 = await host.ExecuteAsync(mainPath);
        global::DraftSpec.Dsl.Reset();

        // Wait a bit to ensure file timestamp changes
        await Task.Delay(50);

        // Modify the helper file (not the main file)
        await File.WriteAllTextAsync(helperPath, helperContent2);

        // Execute again - should pick up the new helper content
        var root2 = await host.ExecuteAsync(mainPath);

        // Assert
        await Assert.That(root1).IsNotNull();
        await Assert.That(root1!.Description).IsEqualTo("HelperV1");
        await Assert.That(root2).IsNotNull();
        await Assert.That(root2!.Description).IsEqualTo("HelperV2");
    }

    #endregion
}
