using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.TestingPlatform;

public class LineNumberCaptureTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_linenumber_{Guid.NewGuid():N}");
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
    public async Task SpecDiscoverer_CapturesLineNumbersForSpecs()
    {
        // Arrange - Note: Line numbers are 1-based
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Calculator", () =>
            {
                it("adds numbers", () => expect(1 + 1).toBe(2));
                it("subtracts numbers", () => expect(5 - 3).toBe(2));
            });
            """;

        var csxPath = Path.Combine(_tempDir, "calc.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert - specs should have line numbers captured
        await Assert.That(specs.Count).IsEqualTo(2);

        // The "adds numbers" spec is on line 5
        var addsSpec = specs.First(s => s.Description == "adds numbers");
        await Assert.That(addsSpec.LineNumber).IsGreaterThan(0);

        // The "subtracts numbers" spec is on line 6
        var subtractsSpec = specs.First(s => s.Description == "subtracts numbers");
        await Assert.That(subtractsSpec.LineNumber).IsGreaterThan(0);

        // The subtract spec should be on a later line than the adds spec
        await Assert.That(subtractsSpec.LineNumber).IsGreaterThan(addsSpec.LineNumber);
    }

    [Test]
    public async Task SpecDiscoverer_CapturesLineNumbersForNestedDescribes()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Outer", () =>
            {
                describe("Inner", () =>
                {
                    it("nested spec", () => { });
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
        await Assert.That(specs[0].LineNumber).IsGreaterThan(0);
    }

    [Test]
    public async Task SpecExecutor_CapturesLineNumbersInResults()
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

        var csxPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var executor = new MtpSpecExecutor(_tempDir);

        // Act
        var result = await executor.ExecuteFileAsync(csxPath);

        // Assert - execution results should also have line numbers
        await Assert.That(result.Results.Count).IsEqualTo(2);
        await Assert.That(result.Results[0].Spec.LineNumber).IsGreaterThan(0);
        await Assert.That(result.Results[1].Spec.LineNumber).IsGreaterThan(0);
        await Assert.That(result.Results[1].Spec.LineNumber).IsGreaterThan(result.Results[0].Spec.LineNumber);
    }

    [Test]
    public async Task SpecDiscoverer_CapturesLineNumbersForFocusedSpecs()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Focused", () =>
            {
                fit("focused spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "focused.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(1);
        await Assert.That(specs[0].IsFocused).IsTrue();
        await Assert.That(specs[0].LineNumber).IsGreaterThan(0);
    }

    [Test]
    public async Task SpecDiscoverer_CapturesLineNumbersForSkippedSpecs()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Skipped", () =>
            {
                xit("skipped spec", () => { });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "skipped.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(1);
        await Assert.That(specs[0].IsSkipped).IsTrue();
        await Assert.That(specs[0].LineNumber).IsGreaterThan(0);
    }

    [Test]
    public async Task SpecDiscoverer_CapturesLineNumbersForPendingSpecs()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Pending", () =>
            {
                it("pending spec");
            });
            """;

        var csxPath = Path.Combine(_tempDir, "pending.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var discoverer = new SpecDiscoverer(_tempDir);

        // Act
        var specs = await discoverer.DiscoverFileAsync(csxPath);

        // Assert
        await Assert.That(specs.Count).IsEqualTo(1);
        await Assert.That(specs[0].IsPending).IsTrue();
        await Assert.That(specs[0].LineNumber).IsGreaterThan(0);
    }
}
