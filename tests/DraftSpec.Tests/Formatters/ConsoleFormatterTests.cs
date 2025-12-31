using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.Formatters.Console;

namespace DraftSpec.Tests.Formatters;

/// <summary>
/// Tests for ConsoleFormatter output.
/// </summary>
[NotInParallel]
public class ConsoleFormatterTests
{
    [Test]
    public async Task Format_PassingSpec_ShowsCheckmark()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "passes", Status = "passed", DurationMs = 10 }
        ]);

        var output = FormatToString(report);

        await Assert.That(output).Contains("✓");
        await Assert.That(output).Contains("passes");
    }

    [Test]
    public async Task Format_FailingSpec_ShowsX()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "fails", Status = "failed", Error = "error message" }
        ]);

        var output = FormatToString(report);

        await Assert.That(output).Contains("✗");
        await Assert.That(output).Contains("fails");
    }

    [Test]
    public async Task Format_PendingSpec_ShowsCircle()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "pending", Status = "pending" }
        ]);

        var output = FormatToString(report);

        await Assert.That(output).Contains("○");
        await Assert.That(output).Contains("pending");
    }

    [Test]
    public async Task Format_SkippedSpec_ShowsDash()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "skipped", Status = "skipped" }
        ]);

        var output = FormatToString(report);

        await Assert.That(output).Contains("-");
        await Assert.That(output).Contains("skipped");
    }

    [Test]
    public async Task Format_ShowsContextDescription()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ], "Calculator");

        var output = FormatToString(report);

        await Assert.That(output).Contains("Calculator");
    }

    [Test]
    public async Task Format_ShowsNestedContexts()
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

        var output = FormatToString(report);

        await Assert.That(output).Contains("Calculator");
        await Assert.That(output).Contains("add method");
    }

    [Test]
    public async Task Format_ShowsSummary()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 3, Passed = 2, Pending = 1, DurationMs = 100 },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "test",
                    Specs =
                    [
                        new SpecResultReport { Description = "spec1", Status = "passed" },
                        new SpecResultReport { Description = "spec2", Status = "passed" },
                        new SpecResultReport { Description = "spec3", Status = "pending" }
                    ]
                }
            ]
        };

        var output = FormatToString(report);

        await Assert.That(output).Contains("3 specs:");
        await Assert.That(output).Contains("passed");
        await Assert.That(output).Contains("pending");
    }

    [Test]
    public async Task Format_ShowsErrorMessage_OnFailure()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "fails", Status = "failed", Error = "specific error message" }
        ]);

        var output = FormatToString(report);

        await Assert.That(output).Contains("specific error message");
    }

    [Test]
    public async Task Format_ShowsDuration()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed", DurationMs = 150 }
        ]);

        var output = FormatToString(report);

        await Assert.That(output).Contains("150ms");
    }

    [Test]
    public async Task Format_WithNoColors_DoesNotThrow()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        // Should not throw when useColors = false
        var output = FormatToString(report, false);

        await Assert.That(output).Contains("✓");
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

    private static string FormatToString(SpecReport report, bool useColors = false)
    {
        var formatter = new ConsoleFormatter();
        using var sw = new StringWriter();
        formatter.Format(report, sw, useColors);
        return sw.ToString();
    }
}
