using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.TestingPlatform;

public class MtpSpecExecutorTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_executor_{Guid.NewGuid():N}");
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
    public async Task ExecuteFileAsync_ReturnsResultsForAllSpecs()
    {
        // Arrange
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

        var executor = new MtpSpecExecutor(_tempDir);

        // Act
        var result = await executor.ExecuteFileAsync(csxPath);

        // Assert
        await Assert.That(result.Results.Count).IsEqualTo(2);
        await Assert.That(result.Results.All(r => r.Status == SpecStatus.Passed)).IsTrue();
    }

    [Test]
    public async Task ExecuteFileAsync_CapturesFailedSpecs()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Failing", () =>
            {
                it("fails assertion", () => expect(1).toBe(2));
            });
            """;

        var csxPath = Path.Combine(_tempDir, "fail.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var executor = new MtpSpecExecutor(_tempDir);

        // Act
        var result = await executor.ExecuteFileAsync(csxPath);

        // Assert
        await Assert.That(result.Results.Count).IsEqualTo(1);
        await Assert.That(result.Results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(result.Results[0].Exception).IsNotNull();
    }

    [Test]
    public async Task ExecuteFileAsync_CapturesPendingSpecs()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Pending", () =>
            {
                it("pending spec without body");
            });
            """;

        var csxPath = Path.Combine(_tempDir, "pending.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var executor = new MtpSpecExecutor(_tempDir);

        // Act
        var result = await executor.ExecuteFileAsync(csxPath);

        // Assert
        await Assert.That(result.Results.Count).IsEqualTo(1);
        await Assert.That(result.Results[0].Status).IsEqualTo(SpecStatus.Pending);
    }

    [Test]
    public async Task ExecuteFileAsync_CapturesSkippedSpecs()
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

        var executor = new MtpSpecExecutor(_tempDir);

        // Act
        var result = await executor.ExecuteFileAsync(csxPath);

        // Assert
        await Assert.That(result.Results.Count).IsEqualTo(1);
        await Assert.That(result.Results[0].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task ExecuteFileAsync_WithFilter_RunsOnlyRequestedSpecs()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Math", () =>
            {
                it("adds", () => expect(1 + 1).toBe(2));
                it("subtracts", () => expect(5 - 3).toBe(2));
                it("multiplies", () => expect(2 * 3).toBe(6));
            });
            """;

        var csxPath = Path.Combine(_tempDir, "math.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var executor = new MtpSpecExecutor(_tempDir);

        // Only request the "adds" spec
        var requestedIds = new HashSet<string>
        {
            "math.spec.csx:Math/adds"
        };

        // Act
        var result = await executor.ExecuteFileAsync(csxPath, requestedIds);

        // Assert - only requested spec should be in results (non-requested specs are not reported)
        await Assert.That(result.Results.Count).IsEqualTo(1);
        await Assert.That(result.Results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(result.Results[0].Spec.Description).IsEqualTo("adds");
    }

    [Test]
    public async Task ExecuteFileAsync_IncludesTimingInfo()
    {
        // Arrange
        var csxContent = """
            using static DraftSpec.Dsl;

            describe("Timing", () =>
            {
                it("takes some time", async () =>
                {
                    await System.Threading.Tasks.Task.Delay(50);
                });
            });
            """;

        var csxPath = Path.Combine(_tempDir, "timing.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var executor = new MtpSpecExecutor(_tempDir);

        // Act
        var result = await executor.ExecuteFileAsync(csxPath);

        // Assert
        await Assert.That(result.Results.Count).IsEqualTo(1);
        await Assert.That(result.Results[0].Duration.TotalMilliseconds).IsGreaterThanOrEqualTo(40);
    }

    [Test]
    public async Task ExecuteFileAsync_IncludesRelativeSourceFile()
    {
        // Arrange
        var specsDir = Path.Combine(_tempDir, "specs");
        Directory.CreateDirectory(specsDir);

        var csxContent = """
            using static DraftSpec.Dsl;
            describe("Test", () => { it("works", () => { }); });
            """;

        var csxPath = Path.Combine(specsDir, "nested.spec.csx");
        await File.WriteAllTextAsync(csxPath, csxContent);

        var executor = new MtpSpecExecutor(_tempDir);

        // Act
        var result = await executor.ExecuteFileAsync(csxPath);

        // Assert
        await Assert.That(result.RelativeSourceFile).IsEqualTo(Path.Combine("specs", "nested.spec.csx"));
    }

    [Test]
    public async Task ExecuteByIdsAsync_GroupsByFileAndExecutes()
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

        var executor = new MtpSpecExecutor(_tempDir);

        var requestedIds = new[]
        {
            "a.spec.csx:FileA/spec a",
            "b.spec.csx:FileB/spec b"
        };

        // Act
        var results = await executor.ExecuteByIdsAsync(requestedIds);

        // Assert
        await Assert.That(results.Count).IsEqualTo(2);

        var passedResults = results.SelectMany(r => r.Results).Where(r => r.Status == SpecStatus.Passed).ToList();
        await Assert.That(passedResults.Count).IsEqualTo(2);
    }
}
