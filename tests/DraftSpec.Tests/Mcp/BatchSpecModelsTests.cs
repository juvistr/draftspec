using System.Text.Json;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.Mcp.Models;

namespace DraftSpec.Tests.Mcp;

/// <summary>
/// Tests for batch spec execution models.
/// </summary>
public class BatchSpecModelsTests
{
    #region BatchSpecInput Tests

    [Test]
    public async Task BatchSpecInput_SerializesCorrectly()
    {
        var input = new BatchSpecInput
        {
            Name = "test-spec",
            Content = "describe(\"Test\", () => {});"
        };

        var json = JsonSerializer.Serialize(input, JsonOptionsProvider.Default);

        await Assert.That(json).Contains("\"name\":");
        await Assert.That(json).Contains("\"content\":");
        await Assert.That(json).Contains("test-spec");
    }

    [Test]
    public async Task BatchSpecInput_DeserializesCorrectly()
    {
        var json = """{"name": "my-spec", "content": "describe('X', () => {});"}""";

        var input = JsonSerializer.Deserialize<BatchSpecInput>(json, JsonOptionsProvider.Default);

        await Assert.That(input).IsNotNull();
        await Assert.That(input!.Name).IsEqualTo("my-spec");
        await Assert.That(input.Content).IsEqualTo("describe('X', () => {});");
    }

    #endregion

    #region BatchSpecResult Tests

    [Test]
    public async Task BatchSpecResult_AllPassed_WhenNoFailures()
    {
        var result = new BatchSpecResult
        {
            AllPassed = true,
            TotalSpecs = 3,
            PassedSpecs = 3,
            FailedSpecs = 0,
            TotalDurationMs = 100,
            Results = []
        };

        await Assert.That(result.AllPassed).IsTrue();
        await Assert.That(result.FailedSpecs).IsEqualTo(0);
    }

    [Test]
    public async Task BatchSpecResult_NotAllPassed_WhenHasFailures()
    {
        var result = new BatchSpecResult
        {
            AllPassed = false,
            TotalSpecs = 3,
            PassedSpecs = 2,
            FailedSpecs = 1,
            TotalDurationMs = 100,
            Results = []
        };

        await Assert.That(result.AllPassed).IsFalse();
        await Assert.That(result.FailedSpecs).IsEqualTo(1);
    }

    [Test]
    public async Task BatchSpecResult_SerializesWithResults()
    {
        var result = new BatchSpecResult
        {
            AllPassed = true,
            TotalSpecs = 2,
            PassedSpecs = 2,
            FailedSpecs = 0,
            TotalDurationMs = 150.5,
            Results =
            [
                new NamedSpecResult
                {
                    Name = "spec-1",
                    Success = true,
                    ExitCode = 0,
                    DurationMs = 75.2
                },
                new NamedSpecResult
                {
                    Name = "spec-2",
                    Success = true,
                    ExitCode = 0,
                    DurationMs = 75.3
                }
            ]
        };

        var json = JsonSerializer.Serialize(result, JsonOptionsProvider.Default);

        await Assert.That(json).Contains("\"allPassed\": true");
        await Assert.That(json).Contains("\"totalSpecs\": 2");
        await Assert.That(json).Contains("\"spec-1\"");
        await Assert.That(json).Contains("\"spec-2\"");
    }

    [Test]
    public async Task BatchSpecResult_EmptyResults_IsValid()
    {
        var result = new BatchSpecResult
        {
            AllPassed = true,
            TotalSpecs = 0,
            PassedSpecs = 0,
            FailedSpecs = 0,
            TotalDurationMs = 0,
            Results = []
        };

        await Assert.That(result.Results).IsEmpty();
        await Assert.That(result.AllPassed).IsTrue();
    }

    #endregion

    #region NamedSpecResult Tests

    [Test]
    public async Task NamedSpecResult_Success_HasNoError()
    {
        var result = new NamedSpecResult
        {
            Name = "passing-spec",
            Success = true,
            ExitCode = 0,
            DurationMs = 50,
            Error = null
        };

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Error).IsNull();
    }

    [Test]
    public async Task NamedSpecResult_Failure_HasError()
    {
        var result = new NamedSpecResult
        {
            Name = "failing-spec",
            Success = false,
            ExitCode = 1,
            DurationMs = 50,
            Error = new SpecError
            {
                Category = ErrorCategory.Assertion,
                Message = "Expected 1 to be 2"
            }
        };

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsNotNull();
        await Assert.That(result.Error!.Category).IsEqualTo(ErrorCategory.Assertion);
    }

    [Test]
    public async Task NamedSpecResult_WithReport_SerializesCorrectly()
    {
        var result = new NamedSpecResult
        {
            Name = "spec-with-report",
            Success = true,
            ExitCode = 0,
            DurationMs = 100,
            Report = new SpecReport
            {
                Summary = new SpecSummary
                {
                    Total = 5,
                    Passed = 5,
                    Failed = 0,
                    Pending = 0,
                    Skipped = 0,
                    DurationMs = 100
                }
            }
        };

        var json = JsonSerializer.Serialize(result, JsonOptionsProvider.Default);

        await Assert.That(json).Contains("\"report\":");
        await Assert.That(json).Contains("\"total\": 5");
        await Assert.That(json).Contains("\"passed\": 5");
    }

    #endregion

    #region JSON Roundtrip Tests

    [Test]
    public async Task BatchSpecResult_RoundTrip_PreservesData()
    {
        var original = new BatchSpecResult
        {
            AllPassed = false,
            TotalSpecs = 3,
            PassedSpecs = 2,
            FailedSpecs = 1,
            TotalDurationMs = 250.5,
            Results =
            [
                new NamedSpecResult
                {
                    Name = "spec-a",
                    Success = true,
                    ExitCode = 0,
                    DurationMs = 100
                },
                new NamedSpecResult
                {
                    Name = "spec-b",
                    Success = true,
                    ExitCode = 0,
                    DurationMs = 100
                },
                new NamedSpecResult
                {
                    Name = "spec-c",
                    Success = false,
                    ExitCode = 1,
                    DurationMs = 50,
                    Error = new SpecError
                    {
                        Category = ErrorCategory.Runtime,
                        Message = "NullReferenceException"
                    }
                }
            ]
        };

        var json = JsonSerializer.Serialize(original, JsonOptionsProvider.Default);
        var deserialized = JsonSerializer.Deserialize<BatchSpecResult>(json, JsonOptionsProvider.Default);

        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.AllPassed).IsEqualTo(original.AllPassed);
        await Assert.That(deserialized.TotalSpecs).IsEqualTo(original.TotalSpecs);
        await Assert.That(deserialized.PassedSpecs).IsEqualTo(original.PassedSpecs);
        await Assert.That(deserialized.FailedSpecs).IsEqualTo(original.FailedSpecs);
        await Assert.That(deserialized.Results.Count).IsEqualTo(3);
    }

    #endregion
}
