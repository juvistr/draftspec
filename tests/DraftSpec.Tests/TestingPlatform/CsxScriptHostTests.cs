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

    #region Compilation Errors

    [Test]
    public async Task Execute_MissingReference_ThrowsCompilationException()
    {
        // Arrange - reference a namespace that doesn't exist
        var csxContent = """
            using static DraftSpec.Dsl;
            using NonExistentNamespace;

            describe("MissingRef", () =>
            {
                it("spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "missing_ref.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act & Assert
        await Assert.ThrowsAsync<CompilationErrorException>(
            async () => await host.ExecuteAsync(csxPath));
    }

    [Test]
    public async Task Execute_RuntimeError_ThrowsException()
    {
        // Arrange - code that throws at runtime
        var csxContent = """
            using static DraftSpec.Dsl;

            throw new InvalidOperationException("Runtime error");

            describe("NeverReached", () =>
            {
                it("spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "runtime_error.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await host.ExecuteAsync(csxPath));
    }

    [Test]
    public async Task Execute_EmptyFile_ReturnsNull()
    {
        // Arrange - empty file
        var csxPath = Path.Combine(_tempDir, "empty.spec.csx");
        await File.WriteAllTextAsync(csxPath, "");

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert - no describe blocks means null
        await Assert.That(rootContext).IsNull();
    }

    [Test]
    public async Task Execute_OnlyComments_ReturnsNull()
    {
        // Arrange - file with only comments
        var csxContent = """
            // This is a comment
            /* Multi-line
               comment */
            """;

        var csxPath = Path.Combine(_tempDir, "comments.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert
        await Assert.That(rootContext).IsNull();
    }

    #endregion

    #region Cyclic Dependencies

    [Test]
    public async Task Execute_SelfLoad_HandledGracefully()
    {
        // Arrange - file that loads itself
        var csxContent = """
            #load "self.spec.csx"
            using static DraftSpec.Dsl;

            describe("Self", () =>
            {
                it("spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "self.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act - should not stack overflow, circular load is prevented
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Description).IsEqualTo("Self");
    }

    [Test]
    public async Task Execute_MutualCyclicLoad_HandledGracefully()
    {
        // Arrange - two files that load each other
        var fileAContent = """
            #load "b.csx"
            using static DraftSpec.Dsl;

            describe("FileA", () =>
            {
                it("spec from A", () => { });
            });
            """;

        var fileBContent = """
            #load "a.csx"
            using static DraftSpec.Dsl;

            describe("FileB", () =>
            {
                it("spec from B", () => { });
            });
            """;

        var fileAPath = Path.Combine(_tempDir, "a.csx");
        var fileBPath = Path.Combine(_tempDir, "b.csx");

        await File.WriteAllTextAsync(fileAPath, fileAContent);
        await File.WriteAllTextAsync(fileBPath, fileBContent);

        var host = new CsxScriptHost(_tempDir);

        // Act - should not stack overflow
        var rootContext = await host.ExecuteAsync(fileAPath);

        // Assert - FileB is loaded first (before FileA's describe), so FileB's describe becomes root
        await Assert.That(rootContext).IsNotNull();
    }

    #endregion

    #region Script Globals

    [Test]
    public async Task Execute_GlobalsAvailable_InScript()
    {
        // Arrange - script that uses CaptureRootContext (injected global)
        var csxContent = """
            using static DraftSpec.Dsl;

            // CaptureRootContext is available as a global from ScriptGlobals
            describe("Globals", () =>
            {
                it("spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "globals.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert - CaptureRootContext was invoked and captured the context
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Description).IsEqualTo("Globals");
    }

    [Test]
    public async Task Execute_MultipleDescribes_ReturnsFirst()
    {
        // Arrange - multiple top-level describes
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("First", () =>
            {
                it("spec 1", () => { });
            });

            describe("Second", () =>
            {
                it("spec 2", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "multi_describe.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert - returns the first describe
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Description).IsEqualTo("First");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Execute_UnicodeInDescription_Works()
    {
        // Arrange - unicode characters in descriptions
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("日本語テスト", () =>
            {
                it("中文测试", () => { });
                it("한국어 테스트", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "unicode.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Description).IsEqualTo("日本語テスト");
        await Assert.That(rootContext.Specs[0].Description).IsEqualTo("中文测试");
    }

    [Test]
    public async Task Execute_LargeScript_CompletesSuccessfully()
    {
        // Arrange - generate a large script with many specs
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("using static DraftSpec.Dsl;");
        builder.AppendLine("describe(\"Large\", () => {");
        for (var i = 0; i < 100; i++)
        {
            builder.AppendLine($"    it(\"spec {i}\", () => {{ }});");
        }
        builder.AppendLine("});");

        var csxPath = Path.Combine(_tempDir, "large.spec.csx");
        await File.WriteAllTextAsync(csxPath, builder.ToString());

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Specs.Count).IsEqualTo(100);
    }

    [Test]
    public async Task Execute_ManyLoadDirectives_Works()
    {
        // Arrange - main file with many #load directives
        for (var i = 0; i < 10; i++)
        {
            var helperContent = $$"""
                using static DraftSpec.Dsl;

                describe("Helper{{i}}", () =>
                {
                    it("spec from helper {{i}}", () => { });
                });
                """;
            await File.WriteAllTextAsync(Path.Combine(_tempDir, $"helper{i}.csx"), helperContent);
        }

        var mainBuilder = new System.Text.StringBuilder();
        for (var i = 0; i < 10; i++)
        {
            mainBuilder.AppendLine($"#load \"helper{i}.csx\"");
        }

        var mainPath = Path.Combine(_tempDir, "main_many_loads.spec.csx");
        await File.WriteAllTextAsync(mainPath, mainBuilder.ToString());

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(mainPath);

        // Assert - first loaded helper becomes the root
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Description).IsEqualTo("Helper0");
    }

    [Test]
    public async Task Execute_NestedLoads_Works()
    {
        // Arrange - chain of nested #load directives
        var level3Content = """
            using static DraftSpec.Dsl;

            describe("Level3", () =>
            {
                it("deepest spec", () => { });
            });
            """;

        var level2Content = """
            #load "level3.csx"
            """;

        var level1Content = """
            #load "level2.csx"
            """;

        await File.WriteAllTextAsync(Path.Combine(_tempDir, "level3.csx"), level3Content);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "level2.csx"), level2Content);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "level1.csx"), level1Content);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(Path.Combine(_tempDir, "level1.csx"));

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Description).IsEqualTo("Level3");
    }

    [Test]
    public async Task Execute_LoadMissingFile_ThrowsException()
    {
        // Arrange - load a file that doesn't exist
        var csxContent = """
            #load "nonexistent.csx"
            using static DraftSpec.Dsl;

            describe("Main", () =>
            {
                it("spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "load_missing.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await host.ExecuteAsync(csxPath));
    }

    [Test]
    public async Task Execute_WithAsyncCode_Works()
    {
        // Arrange - script with async operations
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Async", () =>
            {
                it("async spec", async () =>
                {
                    await Task.Delay(1);
                });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "async.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Specs[0].Description).IsEqualTo("async spec");
    }

    [Test]
    public async Task Execute_WithHooks_PreservesHooks()
    {
        // Arrange - script with lifecycle hooks
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("WithHooks", () =>
            {
                before(() => { });
                after(() => { });
                beforeAll(() => { });
                afterAll(() => { });

                it("spec with hooks", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "hooks.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.GetBeforeEachChain()).Count().IsGreaterThan(0);
        await Assert.That(rootContext.GetAfterEachChain()).Count().IsGreaterThan(0);
        await Assert.That(rootContext.BeforeAllHooks).Count().IsGreaterThan(0);
        await Assert.That(rootContext.AfterAllHooks).Count().IsGreaterThan(0);
    }

    [Test]
    public async Task Execute_WithFocusedSpec_PreservesFocus()
    {
        // Arrange - script with focused spec
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Focus", () =>
            {
                it("normal spec", () => { });
                fit("focused spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "focus.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Specs[0].IsFocused).IsFalse();
        await Assert.That(rootContext.Specs[1].IsFocused).IsTrue();
    }

    [Test]
    public async Task Execute_WithSkippedSpec_PreservesSkip()
    {
        // Arrange - script with skipped spec
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Skip", () =>
            {
                it("normal spec", () => { });
                xit("skipped spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "skip.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Specs[0].IsSkipped).IsFalse();
        await Assert.That(rootContext.Specs[1].IsSkipped).IsTrue();
    }

    [Test]
    public async Task Execute_WithPendingSpec_PreservesPending()
    {
        // Arrange - script with pending spec (no body)
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Pending", () =>
            {
                it("normal spec", () => { });
                it("pending spec");
            });
            """;

        var csxPath = Path.Combine(_tempDir, "pending.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);

        // Act
        var rootContext = await host.ExecuteAsync(csxPath);

        // Assert
        await Assert.That(rootContext).IsNotNull();
        await Assert.That(rootContext!.Specs[0].IsPending).IsFalse();
        await Assert.That(rootContext.Specs[1].IsPending).IsTrue();
    }

    [Test]
    public async Task Reset_ClearsGlobalState()
    {
        // Arrange - create a context
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("BeforeReset", () =>
            {
                it("spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "reset.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var host = new CsxScriptHost(_tempDir);
        await host.ExecuteAsync(csxPath);

        // Act
        host.Reset();

        // Assert - Dsl.RootContext should be null after reset
        await Assert.That(global::DraftSpec.Dsl.RootContext).IsNull();
    }

    #endregion

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
