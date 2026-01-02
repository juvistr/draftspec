using DraftSpec.Scripting;
using DraftSpec.TestingPlatform;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.TestingPlatform;

public class SpecDiscovererTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_discoverer_{Guid.NewGuid():N}");
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
    public async Task DiscoverAsync_FindsAllSpecFiles()
    {
        // Arrange
        var specsDir = Path.Combine(_tempDir, "Specs");
        Directory.CreateDirectory(specsDir);

        await File.WriteAllTextAsync(Path.Combine(specsDir, "first.spec.csx"), """
            using static DraftSpec.Dsl;
            describe("First", () => { it("spec 1", () => { }); });
            """);

        await File.WriteAllTextAsync(Path.Combine(specsDir, "second.spec.csx"), """
            using static DraftSpec.Dsl;
            describe("Second", () => { it("spec 2", () => { }); });
            """);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var result = await discoverer.DiscoverAsync();

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(2);
        await Assert.That(result.Specs.Select(s => s.Description)).Contains("spec 1");
        await Assert.That(result.Specs.Select(s => s.Description)).Contains("spec 2");
    }

    [Test]
    public async Task DiscoverFileAsync_ReturnsSpecsFromSingleFile()
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

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(2);
        await Assert.That(specs[0].Description).IsEqualTo("adds numbers");
        await Assert.That(specs[1].Description).IsEqualTo("subtracts numbers");
    }

    [Test]
    public async Task DiscoverFileAsync_GeneratesStableIds()
    {
        // Arrange
        var specsDir = Path.Combine(_tempDir, "specs");
        Directory.CreateDirectory(specsDir);

        var csxContent = """
            using static DraftSpec.Dsl;

            describe("UserService", () =>
            {
                describe("CreateAsync", () =>
                {
                    it("creates a user with valid data", () => { });
                });
            });
            """;

        var csxPath = Path.Combine(specsDir, "UserService.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(1);
        await Assert.That(specs[0].Id).IsEqualTo("specs/UserService.spec.csx:UserService/CreateAsync/creates a user with valid data");
    }

    [Test]
    public async Task DiscoverFileAsync_CapturesContextPath()
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

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(1);
        await Assert.That(specs[0].ContextPath.Count).IsEqualTo(2);
        await Assert.That(specs[0].ContextPath[0]).IsEqualTo("Parent");
        await Assert.That(specs[0].ContextPath[1]).IsEqualTo("Child");
    }

    [Test]
    public async Task DiscoverFileAsync_GeneratesDisplayName()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Auth", () =>
            {
                describe("Login", () =>
                {
                    it("validates credentials", () => { });
                });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "auth.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        // DisplayName is now just the spec description (tree view shows hierarchy)
        await Assert.That(specs[0].DisplayName).IsEqualTo("validates credentials");
    }

    [Test]
    public async Task DiscoverFileAsync_CapturesPendingSpecs()
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

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(2);
        await Assert.That(specs[0].IsPending).IsFalse();
        await Assert.That(specs[1].IsPending).IsTrue();
    }

    [Test]
    public async Task DiscoverFileAsync_CapturesSkippedSpecs()
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

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(2);
        await Assert.That(specs[0].IsSkipped).IsFalse();
        await Assert.That(specs[1].IsSkipped).IsTrue();
    }

    [Test]
    public async Task DiscoverFileAsync_CapturesFocusedSpecs()
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

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(2);
        await Assert.That(specs[0].IsFocused).IsFalse();
        await Assert.That(specs[1].IsFocused).IsTrue();
    }

    [Test]
    public async Task DiscoverFileAsync_CapturesTags()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Tagged", () =>
            {
                it("spec without tags", () => { });
                tags(["unit", "fast"], () => {
                    it("spec with tags", () => { });
                });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "tagged.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(2);
        await Assert.That(specs[0].Tags.Count).IsEqualTo(0);
        await Assert.That(specs[1].Tags.Count).IsEqualTo(2);
        await Assert.That(specs[1].Tags).Contains("unit");
        await Assert.That(specs[1].Tags).Contains("fast");
    }

    [Test]
    public async Task DiscoverFileAsync_IncludesSourceFilePaths()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;
            describe("Test", () => { it("spec", () => { }); });
            """;

        var csxPath = Path.Combine(_tempDir, "source.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs[0].SourceFile).IsEqualTo(csxPath);
        await Assert.That(specs[0].RelativeSourceFile).IsEqualTo("source.spec.csx");
    }

    [Test]
    public async Task DiscoverAsync_IsolatesStateBetweenFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "a.spec.csx"), """
            using static DraftSpec.Dsl;
            describe("FileA", () => { it("spec a", () => { }); });
            """);

        await File.WriteAllTextAsync(Path.Combine(_tempDir, "b.spec.csx"), """
            using static DraftSpec.Dsl;
            describe("FileB", () => { it("spec b", () => { }); });
            """);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var result = await discoverer.DiscoverAsync();

        // Assert - each file should have independent specs, not combined
        await Assert.That(result.Specs.Count).IsEqualTo(2);

        var fileASpecs = result.Specs.Where(s => s.ContextPath[0] == "FileA").ToList();
        var fileBSpecs = result.Specs.Where(s => s.ContextPath[0] == "FileB").ToList();

        await Assert.That(fileASpecs.Count).IsEqualTo(1);
        await Assert.That(fileBSpecs.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DiscoverFileAsync_EmptyFile_ReturnsEmptyList()
    {
        // Arrange - file with no specs
        var csxContent = """
            using static DraftSpec.Dsl;
            // No describe blocks
            """;

        var csxPath = Path.Combine(_tempDir, "empty.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DiscoverAsync_HandlesNestedDirectories()
    {
        // Arrange
        var nestedDir = Path.Combine(_tempDir, "features", "auth");
        Directory.CreateDirectory(nestedDir);

        await File.WriteAllTextAsync(Path.Combine(nestedDir, "login.spec.csx"), """
            using static DraftSpec.Dsl;
            describe("Login", () => { it("works", () => { }); });
            """);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var result = await discoverer.DiscoverAsync();

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(1);
        // Path should use forward slashes
        await Assert.That(result.Specs[0].Id).Contains("features/auth/login.spec.csx");
    }

    [Test]
    public async Task DiscoverAsync_CollectsCompilationErrors()
    {
        // Arrange - file with invalid code (using non-existent method)
        var validSpec = Path.Combine(_tempDir, "valid.spec.csx");
        await File.WriteAllTextAsync(validSpec, """
            using static DraftSpec.Dsl;
            describe("Valid", () => { it("works", () => { }); });
            """);

        var invalidSpec = Path.Combine(_tempDir, "invalid.spec.csx");
        await File.WriteAllTextAsync(invalidSpec, """
            using static DraftSpec.Dsl;
            describe("Invalid", () => { it("fails", () => {
                nonExistentMethod();
            }); });
            """);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var result = await discoverer.DiscoverAsync();

        // Assert - should have specs from both files
        // The invalid file's spec is discovered via static parsing with a compilation error
        await Assert.That(result.Specs.Count).IsEqualTo(2);

        var validSpecResult = result.Specs.Single(s => s.Description == "works");
        var invalidSpecResult = result.Specs.Single(s => s.Description == "fails");

        await Assert.That(validSpecResult.HasCompilationError).IsFalse();
        await Assert.That(invalidSpecResult.HasCompilationError).IsTrue();
        await Assert.That(invalidSpecResult.CompilationError).Contains("nonExistentMethod");

        // No errors in the list since specs were found statically
        await Assert.That(result.HasErrors).IsFalse();
    }

    [Test]
    public async Task DiscoveryError_GeneratesStableId()
    {
        // Arrange
        var error = new DiscoveryError
        {
            SourceFile = "/project/specs/broken.spec.csx",
            RelativeSourceFile = "specs/broken.spec.csx",
            Message = "Compilation failed"
        };

        // Assert
        await Assert.That(error.Id).IsEqualTo("specs/broken.spec.csx:DISCOVERY_ERROR");
        await Assert.That(error.DisplayName).IsEqualTo("[Discovery Error] specs/broken.spec.csx");
    }

    #region Unit tests with MockScriptHost

    [Test]
    public async Task DiscoverFileAsync_WithMock_CallsExecuteOnScriptHost()
    {
        // Arrange
        var csxPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(csxPath, "// empty");

        var mockHost = new MockScriptHost()
            .WithNoSpecs(csxPath);

        var discoverer = new SpecDiscoverer(_tempDir, mockHost);

        // Act
        await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(mockHost.ExecuteCalls).Count().IsEqualTo(1);
        await Assert.That(mockHost.ExecuteCalls[0]).IsEqualTo(csxPath);
    }

    [Test]
    public async Task DiscoverFileAsync_WithMock_CallsResetBeforeAndAfter()
    {
        // Arrange
        var csxPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(csxPath, "// empty");

        var mockHost = new MockScriptHost()
            .WithNoSpecs(csxPath);
        var mockStateManager = new MockSpecStateManager();

        var discoverer = new SpecDiscoverer(_tempDir, mockHost, stateManager: mockStateManager);

        // Act
        await discoverer.DiscoverFileAsync(csxPath);

        // Assert - ResetState called before and after execution
        await Assert.That(mockStateManager.ResetCallCount).IsEqualTo(2);
    }

    [Test]
    public async Task DiscoverFileAsync_WithMock_ReturnsSpecsFromContext()
    {
        // Arrange
        var csxPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(csxPath, "// not executed");

        var context = new SpecContext("TestContext");
        context.AddSpec(new SpecDefinition("test spec", () => { }));

        var mockHost = new MockScriptHost()
            .WithResult(csxPath, context);

        var discoverer = new SpecDiscoverer(_tempDir, mockHost);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(1);
        await Assert.That(specs[0].Description).IsEqualTo("test spec");
    }

    [Test]
    public async Task DiscoverFileAsync_WithMock_WhenContextIsNull_ReturnsEmptyList()
    {
        // Arrange
        var csxPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(csxPath, "// empty");

        var mockHost = new MockScriptHost()
            .WithNoSpecs(csxPath);

        var discoverer = new SpecDiscoverer(_tempDir, mockHost);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DiscoverAsync_WithMock_CallsResetForEachFile()
    {
        // Arrange
        var file1 = Path.Combine(_tempDir, "a.spec.csx");
        var file2 = Path.Combine(_tempDir, "b.spec.csx");
        await File.WriteAllTextAsync(file1, "// file 1");
        await File.WriteAllTextAsync(file2, "// file 2");

        var mockHost = new MockScriptHost()
            .WithNoSpecs(file1)
            .WithNoSpecs(file2);
        var mockStateManager = new MockSpecStateManager();

        var discoverer = new SpecDiscoverer(_tempDir, mockHost, stateManager: mockStateManager);

        // Act
        await discoverer.DiscoverAsync();

        // Assert - ResetState called before and after each file (2 files * 2 calls = 4)
        await Assert.That(mockStateManager.ResetCallCount).IsEqualTo(4);
    }

    [Test]
    public async Task DiscoverAsync_WithMock_WhenExecutionThrows_FallsBackToStaticParsing()
    {
        // Arrange
        var csxPath = Path.Combine(_tempDir, "broken.spec.csx");
        await File.WriteAllTextAsync(csxPath, """
            using static DraftSpec.Dsl;
            describe("BrokenContext", () => {
                it("broken spec", () => { });
            });
            """);

        var mockHost = new MockScriptHost()
            .ThrowsFor(csxPath, new InvalidOperationException("Compilation failed"));

        var discoverer = new SpecDiscoverer(_tempDir, mockHost);

        // Act
        var result = await discoverer.DiscoverAsync();

        // Assert - spec discovered via static parsing with compilation error
        await Assert.That(result.Specs.Count).IsEqualTo(1);
        await Assert.That(result.Specs[0].HasCompilationError).IsTrue();
        await Assert.That(result.Specs[0].CompilationError).Contains("Compilation failed");
    }

    [Test]
    public async Task DiscoverAsync_WithMock_WhenExecutionThrows_StillCallsReset()
    {
        // Arrange
        var csxPath = Path.Combine(_tempDir, "broken.spec.csx");
        await File.WriteAllTextAsync(csxPath, """
            using static DraftSpec.Dsl;
            describe("Test", () => { it("spec", () => { }); });
            """);

        var mockHost = new MockScriptHost()
            .ThrowsFor(csxPath, new InvalidOperationException("Error"));
        var mockStateManager = new MockSpecStateManager();

        var discoverer = new SpecDiscoverer(_tempDir, mockHost, stateManager: mockStateManager);

        // Act
        await discoverer.DiscoverAsync();

        // Assert - ResetState still called before and after (2 times)
        await Assert.That(mockStateManager.ResetCallCount).IsEqualTo(2);
    }

    [Test]
    public async Task DiscoverAsync_WithMock_AggregatesSpecsFromMultipleFiles()
    {
        // Arrange
        var file1 = Path.Combine(_tempDir, "one.spec.csx");
        var file2 = Path.Combine(_tempDir, "two.spec.csx");
        await File.WriteAllTextAsync(file1, "// file 1");
        await File.WriteAllTextAsync(file2, "// file 2");

        var context1 = new SpecContext("Context1");
        context1.AddSpec(new SpecDefinition("spec 1", () => { }));

        var context2 = new SpecContext("Context2");
        context2.AddSpec(new SpecDefinition("spec 2", () => { }));

        var mockHost = new MockScriptHost()
            .WithResult(file1, context1)
            .WithResult(file2, context2);

        var discoverer = new SpecDiscoverer(_tempDir, mockHost);

        // Act
        var result = await discoverer.DiscoverAsync();

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(2);
        await Assert.That(result.Specs.Select(s => s.Description)).Contains("spec 1");
        await Assert.That(result.Specs.Select(s => s.Description)).Contains("spec 2");
    }

    #endregion

    #region Unit tests with MockSpecStateManager

    [Test]
    public async Task DiscoverAsync_WithMockStateManager_ResetsStateBeforeEachFile()
    {
        // Arrange
        var file1 = Path.Combine(_tempDir, "first.spec.csx");
        var file2 = Path.Combine(_tempDir, "second.spec.csx");
        await File.WriteAllTextAsync(file1, "// file 1");
        await File.WriteAllTextAsync(file2, "// file 2");

        var context1 = new SpecContext("First");
        context1.AddSpec(new SpecDefinition("spec 1", () => { }));

        var context2 = new SpecContext("Second");
        context2.AddSpec(new SpecDefinition("spec 2", () => { }));

        var mockHost = new MockScriptHost()
            .WithResult(file1, context1)
            .WithResult(file2, context2);
        var mockStateManager = new MockSpecStateManager();

        var discoverer = new SpecDiscoverer(_tempDir, mockHost, stateManager: mockStateManager);

        // Act
        await discoverer.DiscoverAsync();

        // Assert - ResetState called 4 times (before + after each of 2 files)
        await Assert.That(mockStateManager.ResetCallCount).IsEqualTo(4);
    }

    [Test]
    public async Task DiscoverFileAsync_WithMockStateManager_ResetsStateOnException()
    {
        // Arrange
        var csxPath = Path.Combine(_tempDir, "error.spec.csx");
        await File.WriteAllTextAsync(csxPath, "// will throw");

        var mockHost = new MockScriptHost()
            .ThrowsFor(csxPath, new InvalidOperationException("Script error"));
        var mockStateManager = new MockSpecStateManager();

        var discoverer = new SpecDiscoverer(_tempDir, mockHost, stateManager: mockStateManager);

        // Act & Assert - should throw but still reset state
        try
        {
            await discoverer.DiscoverFileAsync(csxPath);
        }
        catch
        {
            // Expected
        }

        // ResetState should still be called (before + after in finally block)
        await Assert.That(mockStateManager.ResetCallCount).IsEqualTo(2);
    }

    #endregion

    #region Unit tests with MockSpecFileProvider

    [Test]
    public async Task DiscoverAsync_WithMockFileProvider_UsesProviderToFindFiles()
    {
        // Arrange
        var mockFileProvider = new MockSpecFileProvider()
            .WithSpecFiles(_tempDir, Path.Combine(_tempDir, "test.spec.csx"));

        var context = new SpecContext("TestContext");
        context.AddSpec(new SpecDefinition("test spec", () => { }));

        var mockHost = new MockScriptHost()
            .WithResult(Path.Combine(_tempDir, "test.spec.csx"), context);

        var discoverer = new SpecDiscoverer(_tempDir, mockHost, mockFileProvider);

        // Act
        await discoverer.DiscoverAsync();

        // Assert - file provider was called to find spec files
        await Assert.That(mockFileProvider.GetSpecFilesCalls).Count().IsEqualTo(1);
        await Assert.That(mockFileProvider.GetSpecFilesCalls[0]).IsEqualTo(_tempDir);
    }

    [Test]
    public async Task DiscoverAsync_WithMockFileProvider_DiscoversSpecsFromReturnedFiles()
    {
        // Arrange
        var file1 = Path.Combine(_tempDir, "first.spec.csx");
        var file2 = Path.Combine(_tempDir, "second.spec.csx");

        var mockFileProvider = new MockSpecFileProvider()
            .WithSpecFiles(_tempDir, file1, file2);

        var context1 = new SpecContext("First");
        context1.AddSpec(new SpecDefinition("spec 1", () => { }));

        var context2 = new SpecContext("Second");
        context2.AddSpec(new SpecDefinition("spec 2", () => { }));

        var mockHost = new MockScriptHost()
            .WithResult(file1, context1)
            .WithResult(file2, context2);

        var discoverer = new SpecDiscoverer(_tempDir, mockHost, mockFileProvider);

        // Act
        var result = await discoverer.DiscoverAsync();

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(2);
        await Assert.That(result.Specs.Select(s => s.Description)).Contains("spec 1");
        await Assert.That(result.Specs.Select(s => s.Description)).Contains("spec 2");
    }

    [Test]
    public async Task DiscoverAsync_WithMockFileProvider_NoSpecFiles_ReturnsEmpty()
    {
        // Arrange
        var mockFileProvider = new MockSpecFileProvider()
            .WithNoSpecFiles();

        var mockHost = new MockScriptHost();

        var discoverer = new SpecDiscoverer(_tempDir, mockHost, mockFileProvider);

        // Act
        var result = await discoverer.DiscoverAsync();

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(0);
        await Assert.That(result.Errors.Count).IsEqualTo(0);
        await Assert.That(mockHost.ExecuteCalls.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DiscoverFileAsync_WithMockFileProvider_UsesProviderForAbsolutePath()
    {
        // Arrange - use cross-platform path construction
        var relativePath = Path.Combine("specs", "test.spec.csx");
        var absolutePath = Path.GetFullPath(Path.Combine(_tempDir, relativePath));

        // Create the file so static parser can read it if needed
        var specsDir = Path.Combine(_tempDir, "specs");
        Directory.CreateDirectory(specsDir);
        await File.WriteAllTextAsync(absolutePath, "// empty");

        var mockFileProvider = new MockSpecFileProvider()
            .WithExistingFile(absolutePath);

        var mockHost = new MockScriptHost()
            .WithNoSpecs(absolutePath);

        var discoverer = new SpecDiscoverer(_tempDir, mockHost, mockFileProvider);

        // Act
        await discoverer.DiscoverFileAsync(relativePath);

        // Assert - script host received the absolute path
        await Assert.That(mockHost.ExecuteCalls).Count().IsEqualTo(1);
        await Assert.That(mockHost.ExecuteCalls[0]).IsEqualTo(absolutePath);
    }

    [Test]
    public async Task DiscoverAsync_WithMockFileProvider_ProcessesAllReturnedFiles()
    {
        // Arrange - return 3 files from the provider
        var files = new[]
        {
            Path.Combine(_tempDir, "a.spec.csx"),
            Path.Combine(_tempDir, "b.spec.csx"),
            Path.Combine(_tempDir, "c.spec.csx")
        };

        var mockFileProvider = new MockSpecFileProvider()
            .WithSpecFiles(_tempDir, files);

        var mockHost = new MockScriptHost();
        foreach (var file in files)
        {
            var context = new SpecContext(Path.GetFileNameWithoutExtension(file));
            context.AddSpec(new SpecDefinition($"spec in {Path.GetFileName(file)}", () => { }));
            mockHost.WithResult(file, context);
        }

        var discoverer = new SpecDiscoverer(_tempDir, mockHost, mockFileProvider);

        // Act
        var result = await discoverer.DiscoverAsync();

        // Assert
        await Assert.That(result.Specs.Count).IsEqualTo(3);
        await Assert.That(mockHost.ExecuteCalls.Count).IsEqualTo(3);
    }

    #endregion
}
