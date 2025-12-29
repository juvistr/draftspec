using System.Text.Json;
using DraftSpec.Formatters;
using DraftSpec.Internal;
using static DraftSpec.Dsl;

namespace DraftSpec.Tests.Output;

/// <summary>
/// Tests for spec report structure and JSON serialization.
/// </summary>
public class OutputTests
{
    [Before(Test)]
    public void SetUp()
    {
        Reset();
    }

    #region Report Structure

    [Test]
    public async Task Report_PassingSpec_HasPassedStatus()
    {
        describe("test", () => { it("passes", () => { }); });

        var report = SpecExecutor.Execute(RootContext!);

        await Assert.That(report.Contexts[0].Specs[0].Status).IsEqualTo("passed");
    }

    [Test]
    public async Task Report_FailingSpec_HasFailedStatus()
    {
        describe("test", () => { it("fails", () => throw new Exception("error")); });

        var report = SpecExecutor.Execute(RootContext!);

        await Assert.That(report.Contexts[0].Specs[0].Status).IsEqualTo("failed");
    }

    [Test]
    public async Task Report_PendingSpec_HasPendingStatus()
    {
        describe("test", () => { it("pending"); });

        var report = SpecExecutor.Execute(RootContext!);

        await Assert.That(report.Contexts[0].Specs[0].Status).IsEqualTo("pending");
    }

    [Test]
    public async Task Report_SkippedSpec_HasSkippedStatus()
    {
        describe("test", () => { xit("skipped", () => { }); });

        var report = SpecExecutor.Execute(RootContext!);

        await Assert.That(report.Contexts[0].Specs[0].Status).IsEqualTo("skipped");
    }

    [Test]
    public async Task Report_ContainsContextDescription()
    {
        describe("Calculator", () => { it("adds numbers", () => { }); });

        var report = SpecExecutor.Execute(RootContext!);

        await Assert.That(report.Contexts[0].Description).IsEqualTo("Calculator");
    }

    [Test]
    public async Task Report_ContainsNestedContexts()
    {
        describe("Calculator", () => { describe("add method", () => { it("returns sum", () => { }); }); });

        var report = SpecExecutor.Execute(RootContext!);

        await Assert.That(report.Contexts[0].Description).IsEqualTo("Calculator");
        await Assert.That(report.Contexts[0].Contexts[0].Description).IsEqualTo("add method");
    }

    [Test]
    public async Task Report_ContainsSummary()
    {
        describe("test", () =>
        {
            it("passes", () => { });
            it("pending");
        });

        var report = SpecExecutor.Execute(RootContext!);

        await Assert.That(report.Summary.Total).IsEqualTo(2);
        await Assert.That(report.Summary.Passed).IsEqualTo(1);
        await Assert.That(report.Summary.Pending).IsEqualTo(1);
    }

    [Test]
    public async Task Report_ContainsErrorMessage_OnFailure()
    {
        describe("test", () => { it("fails", () => throw new InvalidOperationException("specific error message")); });

        var report = SpecExecutor.Execute(RootContext!);

        await Assert.That(report.Contexts[0].Specs[0].Error).Contains("specific error message");
    }

    #endregion

    #region JSON Output

    [Test]
    public async Task JsonOutput_ContainsTimestamp()
    {
        describe("test", () => { it("spec", () => { }); });

        var report = SpecExecutor.Execute(RootContext!);
        var json = report.ToJson();
        var doc = JsonDocument.Parse(json);

        await Assert.That(doc.RootElement.TryGetProperty("timestamp", out _)).IsTrue();
    }

    [Test]
    public async Task JsonOutput_ContainsSummary()
    {
        describe("test", () =>
        {
            it("passes", () => { });
            it("pending");
        });

        var report = SpecExecutor.Execute(RootContext!);
        var json = report.ToJson();
        var doc = JsonDocument.Parse(json);
        var summary = doc.RootElement.GetProperty("summary");

        await Assert.That(summary.GetProperty("total").GetInt32()).IsEqualTo(2);
        await Assert.That(summary.GetProperty("passed").GetInt32()).IsEqualTo(1);
        await Assert.That(summary.GetProperty("pending").GetInt32()).IsEqualTo(1);
    }

    [Test]
    public async Task JsonOutput_ContainsContexts()
    {
        describe("Calculator", () => { it("works", () => { }); });

        var report = SpecExecutor.Execute(RootContext!);
        var json = report.ToJson();
        var doc = JsonDocument.Parse(json);
        var contexts = doc.RootElement.GetProperty("contexts");

        await Assert.That(contexts.GetArrayLength()).IsGreaterThan(0);
        await Assert.That(contexts[0].GetProperty("description").GetString()).IsEqualTo("Calculator");
    }

    [Test]
    public async Task JsonOutput_ContainsSpecDetails()
    {
        describe("test", () => { it("spec description", () => { }); });

        var report = SpecExecutor.Execute(RootContext!);
        var json = report.ToJson();
        var doc = JsonDocument.Parse(json);
        var specs = doc.RootElement.GetProperty("contexts")[0].GetProperty("specs");

        await Assert.That(specs.GetArrayLength()).IsGreaterThan(0);
        await Assert.That(specs[0].GetProperty("description").GetString()).IsEqualTo("spec description");
        await Assert.That(specs[0].GetProperty("status").GetString()).IsEqualTo("passed");
    }

    [Test]
    public async Task JsonOutput_ContainsDuration()
    {
        describe("test", () => { it("spec", () => Thread.Sleep(5)); });

        var report = SpecExecutor.Execute(RootContext!);
        var json = report.ToJson();
        var doc = JsonDocument.Parse(json);
        var spec = doc.RootElement.GetProperty("contexts")[0].GetProperty("specs")[0];

        await Assert.That(spec.TryGetProperty("durationMs", out var duration)).IsTrue();
        await Assert.That(duration.GetDouble()).IsGreaterThan(0);
    }

    [Test]
    public async Task JsonOutput_ContainsErrorMessage_OnFailure()
    {
        describe("test", () => { it("fails", () => throw new Exception("json error")); });

        var report = SpecExecutor.Execute(RootContext!);
        var json = report.ToJson();
        var doc = JsonDocument.Parse(json);
        var spec = doc.RootElement.GetProperty("contexts")[0].GetProperty("specs")[0];

        await Assert.That(spec.GetProperty("status").GetString()).IsEqualTo("failed");
        await Assert.That(spec.GetProperty("error").GetString()).IsEqualTo("json error");
    }

    [Test]
    public async Task JsonOutput_NestedContexts_PreservesHierarchy()
    {
        describe("outer", () => { describe("inner", () => { it("spec", () => { }); }); });

        var report = SpecExecutor.Execute(RootContext!);
        var json = report.ToJson();
        var doc = JsonDocument.Parse(json);
        var outer = doc.RootElement.GetProperty("contexts")[0];
        var inner = outer.GetProperty("contexts")[0];

        await Assert.That(outer.GetProperty("description").GetString()).IsEqualTo("outer");
        await Assert.That(inner.GetProperty("description").GetString()).IsEqualTo("inner");
    }

    #endregion
}
