using System.Text.Json;
using DraftSpec.Cli.Configuration;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using FsCheck;
using FsCheck.Fluent;

namespace DraftSpec.Tests.Properties;

/// <summary>
/// Property-based tests for JSON serialization round-trips.
/// These tests verify that serialize/deserialize preserves all data.
/// </summary>
public class SerializationPropertyTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    [Test]
    public void SpecSummary_RoundTrip_PreservesAllValues()
    {
        // Property: All summary values survive round-trip
        Prop.ForAll<int, int, int>((passed, failed, pending) =>
        {
            var p = Math.Abs(passed % 1000);
            var f = Math.Abs(failed % 1000);
            var pe = Math.Abs(pending % 1000);
            var s = Math.Abs((passed + failed) % 1000);
            var d = Math.Abs(pending % 10000);

            var original = new SpecSummary
            {
                Total = p + f + pe + s,
                Passed = p,
                Failed = f,
                Pending = pe,
                Skipped = s,
                DurationMs = d
            };

            var json = JsonSerializer.Serialize(original, Options);
            var restored = JsonSerializer.Deserialize<SpecSummary>(json, Options)!;

            return original.Total == restored.Total &&
                   original.Passed == restored.Passed &&
                   original.Failed == restored.Failed &&
                   original.Pending == restored.Pending &&
                   original.Skipped == restored.Skipped &&
                   Math.Abs(original.DurationMs - restored.DurationMs) < 0.001;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void SpecReport_RoundTrip_PreservesStructure()
    {
        // Property: Report structure survives ToJson/FromJson
        Prop.ForAll<int, int>((passed, failed) =>
        {
            var p = Math.Abs(passed % 100);
            var f = Math.Abs(failed % 100);

            var original = new SpecReport
            {
                Timestamp = DateTime.UtcNow,
                Source = "test-source",
                Contexts = [],
                Summary = new SpecSummary
                {
                    Total = p + f,
                    Passed = p,
                    Failed = f,
                    DurationMs = 100
                }
            };

            var json = original.ToJson();
            var restored = SpecReport.FromJson(json);

            return original.Source == restored.Source &&
                   original.Summary.Total == restored.Summary.Total &&
                   original.Summary.Passed == restored.Summary.Passed &&
                   original.Summary.Failed == restored.Summary.Failed;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task SpecReport_RoundTrip_PreservesTimestamp()
    {
        // Property: DateTime is preserved through round-trip
        var original = new SpecReport
        {
            Timestamp = new DateTime(2025, 6, 15, 12, 30, 45, DateTimeKind.Utc),
            Source = "test",
            Contexts = [],
            Summary = new SpecSummary()
        };

        var json = original.ToJson();
        var restored = SpecReport.FromJson(json);

        await Assert.That(restored.Timestamp.Year).IsEqualTo(2025);
        await Assert.That(restored.Timestamp.Month).IsEqualTo(6);
        await Assert.That(restored.Timestamp.Day).IsEqualTo(15);
    }

    [Test]
    public async Task SpecContextReport_RoundTrip_PreservesNesting()
    {
        // Property: Nested context reports survive round-trip
        var original = new SpecContextReport
        {
            Description = "Parent",
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Child 1",
                    Contexts = [],
                    Specs = [new SpecResultReport { Description = "spec 1", Status = "passed" }]
                },
                new SpecContextReport
                {
                    Description = "Child 2",
                    Contexts = [],
                    Specs = []
                }
            ],
            Specs = []
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<SpecContextReport>(json, Options)!;

        await Assert.That(restored.Description).IsEqualTo("Parent");
        await Assert.That(restored.Contexts.Count).IsEqualTo(2);
        await Assert.That(restored.Contexts[0].Description).IsEqualTo("Child 1");
        await Assert.That(restored.Contexts[0].Specs.Count).IsEqualTo(1);
    }

    [Test]
    public void DraftSpecProjectConfig_RoundTrip_PreservesNullables()
    {
        // Property: Nullable properties that are set survive round-trip
        Prop.ForAll<int, bool>((timeout, parallel) =>
        {
            var t = Math.Abs(timeout % 30000) + 1; // Ensure positive

            var original = new DraftSpecProjectConfig
            {
                Timeout = t,
                Parallel = parallel,
                MaxParallelism = null, // Explicitly null
                SpecPattern = "**/*.spec.csx"
            };

            var json = JsonSerializer.Serialize(original, Options);
            var restored = JsonSerializer.Deserialize<DraftSpecProjectConfig>(json, Options)!;

            return restored.Timeout == t &&
                   restored.Parallel == parallel &&
                   restored.MaxParallelism == null &&
                   restored.SpecPattern == "**/*.spec.csx";
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task DraftSpecProjectConfig_RoundTrip_PreservesStringLists()
    {
        // Property: List<string> properties survive round-trip
        var original = new DraftSpecProjectConfig
        {
            Reporters = ["console", "json", "html"]
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DraftSpecProjectConfig>(json, Options)!;

        Assert.NotNull(restored.Reporters);
        await Assert.That(restored.Reporters.Count).IsEqualTo(3);
        await Assert.That(restored.Reporters[0]).IsEqualTo("console");
        await Assert.That(restored.Reporters[1]).IsEqualTo("json");
        await Assert.That(restored.Reporters[2]).IsEqualTo("html");
    }

    [Test]
    public async Task SpecResultReport_RoundTrip_PreservesStatus()
    {
        // Property: All status values round-trip correctly
        var statuses = new[] { "passed", "failed", "pending", "skipped" };

        foreach (var status in statuses)
        {
            var original = new SpecResultReport
            {
                Description = "Test spec",
                Status = status,
                DurationMs = 123.45
            };

            var json = JsonSerializer.Serialize(original, Options);
            var restored = JsonSerializer.Deserialize<SpecResultReport>(json, Options)!;

            await Assert.That(restored.Status).IsEqualTo(status);
            await Assert.That(restored.Description).IsEqualTo("Test spec");
        }
    }

    [Test]
    public void SpecResultReport_RoundTrip_PreservesErrorMessage()
    {
        // Property: Error messages survive round-trip
        Prop.ForAll<NonNull<string>>(error =>
        {
            var original = new SpecResultReport
            {
                Description = "Failing spec",
                Status = "failed",
                Error = error.Get
            };

            var json = JsonSerializer.Serialize(original, Options);
            var restored = JsonSerializer.Deserialize<SpecResultReport>(json, Options)!;

            return restored.Error == error.Get;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void CoverageConfig_RoundTrip_PreservesThresholds()
    {
        // Property: Coverage thresholds survive round-trip
        Prop.ForAll<int, int>((line, branch) =>
        {
            var l = Math.Abs(line % 100);
            var b = Math.Abs(branch % 100);

            var original = new CoverageConfig
            {
                Enabled = true,
                Output = "coverage",
                Format = "cobertura",
                Thresholds = new ThresholdsConfig
                {
                    Line = l,
                    Branch = b
                }
            };

            var json = JsonSerializer.Serialize(original, Options);
            var restored = JsonSerializer.Deserialize<CoverageConfig>(json, Options)!;

            return restored.Enabled == true &&
                   restored.Thresholds != null &&
                   Math.Abs((restored.Thresholds.Line ?? 0) - l) < 0.001 &&
                   Math.Abs((restored.Thresholds.Branch ?? 0) - b) < 0.001;
        }).QuickCheckThrowOnFailure();
    }
}
