using System.Text.Json;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec.Tests.Formatters;

/// <summary>
/// Tests for JsonFormatter output.
/// </summary>
public class JsonFormatterTests
{
    [Test]
    public async Task Format_WithValidReport_ReturnsValidJson()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "passes", Status = "passed", DurationMs = 10 }
        ]);

        var formatter = new JsonFormatter();
        var output = formatter.Format(report);

        // Should be valid JSON
        var parsed = JsonSerializer.Deserialize<SpecReport>(output, JsonOptionsProvider.Default);
        await Assert.That(parsed).IsNotNull();
        await Assert.That(parsed!.Summary.Total).IsEqualTo(1);
        await Assert.That(parsed.Summary.Passed).IsEqualTo(1);
    }

    [Test]
    public async Task Format_WithEmptyReport_ReturnsValidJsonWithEmptyContexts()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 0 },
            Contexts = []
        };

        var formatter = new JsonFormatter();
        var output = formatter.Format(report);

        var parsed = JsonSerializer.Deserialize<SpecReport>(output, JsonOptionsProvider.Default);
        await Assert.That(parsed).IsNotNull();
        await Assert.That(parsed!.Contexts).IsEmpty();
    }

    [Test]
    public async Task FileExtension_ReturnsJson()
    {
        var formatter = new JsonFormatter();

        await Assert.That(formatter.FileExtension).IsEqualTo(".json");
    }

    [Test]
    public async Task Constructor_WithNullOptions_UsesDefaults()
    {
        // When null is passed, should use default options (not throw)
        var formatter = new JsonFormatter(null!);

        var report = CreateReport([
            new SpecResultReport { Description = "test", Status = "passed" }
        ]);

        var output = formatter.Format(report);

        // Should produce valid JSON
        await Assert.That(output).Contains("\"description\"");
    }

    [Test]
    public async Task Constructor_WithCustomOptions_UsesProvidedOptions()
    {
        var customOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Use PascalCase instead of camelCase
            WriteIndented = false
        };

        var formatter = new JsonFormatter(customOptions);

        var report = CreateReport([
            new SpecResultReport { Description = "test", Status = "passed" }
        ]);

        var output = formatter.Format(report);

        // With null PropertyNamingPolicy, should use PascalCase
        await Assert.That(output).Contains("\"Description\"");
    }

    [Test]
    public async Task Format_WithNestedContexts_PreservesStructure()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 1, Passed = 1 },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Calculator",
                    Contexts =
                    [
                        new SpecContextReport
                        {
                            Description = "add method",
                            Specs = [new SpecResultReport { Description = "returns sum", Status = "passed" }]
                        }
                    ]
                }
            ]
        };

        var formatter = new JsonFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("Calculator");
        await Assert.That(output).Contains("add method");
        await Assert.That(output).Contains("returns sum");
    }

    private static SpecReport CreateReport(List<SpecResultReport> specs, string contextDescription = "test")
    {
        return new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary
            {
                Total = specs.Count,
                Passed = specs.Count(s => s.Status == "passed"),
                Failed = specs.Count(s => s.Status == "failed"),
                Pending = specs.Count(s => s.Status == "pending"),
                Skipped = specs.Count(s => s.Status == "skipped"),
                DurationMs = specs.Sum(s => s.DurationMs ?? 0)
            },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = contextDescription,
                    Specs = specs
                }
            ]
        };
    }
}
