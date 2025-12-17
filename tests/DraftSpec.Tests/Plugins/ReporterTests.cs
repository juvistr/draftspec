using DraftSpec.Configuration;
using DraftSpec.Formatters;
using DraftSpec.Plugins;
using DraftSpec.Plugins.Reporters;

namespace DraftSpec.Tests.Plugins;

public class ReporterTests
{
    #region Reporter Lifecycle

    [Test]
    public async Task Reporter_ReceivesLifecycleEvents_InOrder()
    {
        var events = new List<string>();
        var reporter = new TrackingReporter(events);
        var config = new DraftSpecConfiguration();
        config.AddReporter(reporter);

        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec1", () => { }));
        context.AddSpec(new SpecDefinition("spec2", () => { }));

        var builder = new SpecRunnerBuilder().WithConfiguration(config);
        var runner = builder.Build();
        runner.Run(context);

        // Verify events: starting, spec completions, then run completed is called separately
        await Assert.That(events[0]).IsEqualTo("starting:2");
        await Assert.That(events[1]).IsEqualTo("spec:spec1");
        await Assert.That(events[2]).IsEqualTo("spec:spec2");
    }

    [Test]
    public async Task Reporter_OnRunStarting_ReceivesTotalSpecCount()
    {
        var reporter = new TrackingReporter([]);
        var config = new DraftSpecConfiguration();
        config.AddReporter(reporter);

        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec1", () => { }));
        context.AddSpec(new SpecDefinition("spec2", () => { }));
        context.AddSpec(new SpecDefinition("spec3", () => { }));

        var builder = new SpecRunnerBuilder().WithConfiguration(config);
        var runner = builder.Build();
        runner.Run(context);

        await Assert.That(reporter.ReceivedTotalSpecs).IsEqualTo(3);
    }

    [Test]
    public async Task Reporter_OnSpecCompleted_ReceivesEachResult()
    {
        var reporter = new TrackingReporter([]);
        var config = new DraftSpecConfiguration();
        config.AddReporter(reporter);

        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("passing", () => { }));
        context.AddSpec(new SpecDefinition("failing", () => throw new Exception("fail")));

        var builder = new SpecRunnerBuilder().WithConfiguration(config);
        var runner = builder.Build();
        runner.Run(context);

        await Assert.That(reporter.ReceivedResults.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(reporter.ReceivedResults[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(reporter.ReceivedResults[1].Status).IsEqualTo(SpecStatus.Failed);
    }

    [Test]
    public async Task Reporter_MultipleReporters_AllReceiveEvents()
    {
        var events1 = new List<string>();
        var events2 = new List<string>();
        var config = new DraftSpecConfiguration();
        config.AddReporter(new TrackingReporter(events1, "r1"));
        config.AddReporter(new TrackingReporter(events2, "r2"));

        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec", () => { }));

        var builder = new SpecRunnerBuilder().WithConfiguration(config);
        var runner = builder.Build();
        runner.Run(context);

        await Assert.That(events1).Contains("spec:spec");
        await Assert.That(events2).Contains("spec:spec");
    }

    #endregion

    #region FileReporter

    [Test]
    public async Task FileReporter_WritesToFile()
    {
        var tempDir = Path.GetTempPath();
        var tempFile = Path.Combine(tempDir, $"draftspec-test-{Guid.NewGuid()}.json");
        try
        {
            var reporter = new FileReporter(tempFile, new JsonFormatter(), tempDir);
            var report = new SpecReport
            {
                Timestamp = DateTime.UtcNow,
                Summary = new SpecSummary { Total = 1, Passed = 1 }
            };

            await reporter.OnRunCompletedAsync(report);

            await Assert.That(File.Exists(tempFile)).IsTrue();
            var content = await File.ReadAllTextAsync(tempFile);
            await Assert.That(content).Contains("\"total\"");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public async Task FileReporter_CreatesDirectoryIfNeeded()
    {
        var tempRoot = Path.GetTempPath();
        var tempDir = Path.Combine(tempRoot, $"draftspec-test-{Guid.NewGuid()}");
        var tempFile = Path.Combine(tempDir, "report.json");
        try
        {
            var reporter = new FileReporter(tempFile, new JsonFormatter(), tempRoot);
            var report = new SpecReport
            {
                Timestamp = DateTime.UtcNow,
                Summary = new SpecSummary { Total = 1, Passed = 1 }
            };

            await reporter.OnRunCompletedAsync(report);

            await Assert.That(File.Exists(tempFile)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task FileReporter_Name_ContainsFileName()
    {
        var tempDir = Path.GetTempPath();
        var tempFile = Path.Combine(tempDir, "results.json");
        var reporter = new FileReporter(tempFile, new JsonFormatter(), tempDir);

        await Assert.That(reporter.Name).Contains("results.json");
    }

    #endregion

    #region JsonFormatter

    [Test]
    public async Task JsonFormatter_FormatsReport()
    {
        var formatter = new JsonFormatter();
        var report = new SpecReport
        {
            Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Summary = new SpecSummary { Total = 5, Passed = 3, Failed = 2 }
        };

        var output = formatter.Format(report);

        await Assert.That(output).Contains("\"total\": 5");
        await Assert.That(output).Contains("\"passed\": 3");
        await Assert.That(output).Contains("\"failed\": 2");
    }

    [Test]
    public async Task JsonFormatter_FileExtension_IsJson()
    {
        var formatter = new JsonFormatter();

        await Assert.That(formatter.FileExtension).IsEqualTo(".json");
    }

    #endregion

    #region Test Helpers

    private class TrackingReporter : IReporter
    {
        private readonly List<string> _events;

        public TrackingReporter(List<string> events, string name = "tracking")
        {
            _events = events;
            Name = name;
        }

        public string Name { get; }
        public int ReceivedTotalSpecs { get; private set; }
        public List<SpecResult> ReceivedResults { get; } = [];

        public Task OnRunStartingAsync(RunStartingContext context)
        {
            ReceivedTotalSpecs = context.TotalSpecs;
            _events.Add($"starting:{context.TotalSpecs}");
            return Task.CompletedTask;
        }

        public Task OnSpecCompletedAsync(SpecResult result)
        {
            ReceivedResults.Add(result);
            _events.Add($"spec:{result.Spec.Description}");
            return Task.CompletedTask;
        }

        public Task OnRunCompletedAsync(SpecReport report)
        {
            _events.Add("completed");
            return Task.CompletedTask;
        }
    }

    #endregion
}