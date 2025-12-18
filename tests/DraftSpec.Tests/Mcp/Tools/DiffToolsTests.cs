using System.Text.Json;
using DraftSpec.Mcp.Tools;

namespace DraftSpec.Tests.Mcp.Tools;

/// <summary>
/// Integration tests for DiffTools MCP methods.
/// </summary>
public class DiffToolsTests
{
    #region Input Parsing

    [Test]
    public async Task DiffSpecs_ValidJson_ParsesCorrectly()
    {
        var baseline = CreateSpecResultJson(passed: 1, failed: 0);
        var current = CreateSpecResultJson(passed: 1, failed: 0);

        var result = DiffTools.DiffSpecs(baseline, current);
        var json = JsonDocument.Parse(result);

        // Should not contain error
        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsFalse();
    }

    [Test]
    public async Task DiffSpecs_InvalidBaselineJson_ReturnsError()
    {
        var result = DiffTools.DiffSpecs("not valid json", CreateSpecResultJson(passed: 1, failed: 0));
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out var error)).IsTrue();
        await Assert.That(error.GetString()).Contains("baseline");
    }

    [Test]
    public async Task DiffSpecs_InvalidCurrentJson_ReturnsError()
    {
        var result = DiffTools.DiffSpecs(CreateSpecResultJson(passed: 1, failed: 0), "not valid json");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out var error)).IsTrue();
        await Assert.That(error.GetString()).Contains("current");
    }

    [Test]
    public async Task DiffSpecs_BothNull_ReturnsEmptyDiff()
    {
        var result = DiffTools.DiffSpecs("", "");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("hasRegressions").GetBoolean()).IsFalse();
    }

    #endregion

    #region Regression Detection

    [Test]
    public async Task DiffSpecs_Regression_DetectsCorrectly()
    {
        var baseline = CreateSpecReportJson("Test > spec1", "passed");
        var current = CreateSpecReportJson("Test > spec1", "failed");

        var result = DiffTools.DiffSpecs(baseline, current);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("hasRegressions").GetBoolean()).IsTrue();
        await Assert.That(json.RootElement.GetProperty("newFailing").GetInt32()).IsEqualTo(1);
    }

    [Test]
    public async Task DiffSpecs_Fix_DetectsCorrectly()
    {
        var baseline = CreateSpecReportJson("Test > spec1", "failed");
        var current = CreateSpecReportJson("Test > spec1", "passed");

        var result = DiffTools.DiffSpecs(baseline, current);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("hasRegressions").GetBoolean()).IsFalse();
        await Assert.That(json.RootElement.GetProperty("newPassing").GetInt32()).IsEqualTo(1);
    }

    [Test]
    public async Task DiffSpecs_NoChanges_ReportsCorrectly()
    {
        var baseline = CreateSpecReportJson("Test > spec1", "passed");
        var current = CreateSpecReportJson("Test > spec1", "passed");

        var result = DiffTools.DiffSpecs(baseline, current);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("hasRegressions").GetBoolean()).IsFalse();
        await Assert.That(json.RootElement.GetProperty("stillPassing").GetInt32()).IsEqualTo(1);
    }

    #endregion

    #region Summary

    [Test]
    public async Task DiffSpecs_IncludesSummary()
    {
        var baseline = CreateSpecReportJson("Test > spec1", "passed");
        var current = CreateSpecReportJson("Test > spec1", "failed");

        var result = DiffTools.DiffSpecs(baseline, current);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("summary", out var summary)).IsTrue();
        await Assert.That(summary.GetString()!.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task DiffSpecs_IncludesChanges()
    {
        var baseline = CreateSpecReportJson("Test > spec1", "passed");
        var current = CreateSpecReportJson("Test > spec1", "failed");

        var result = DiffTools.DiffSpecs(baseline, current);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("changes", out var changes)).IsTrue();
        await Assert.That(changes.GetArrayLength()).IsEqualTo(1);
    }

    #endregion

    #region RunSpecResult Format

    [Test]
    public async Task DiffSpecs_AcceptsRunSpecResultFormat()
    {
        // Test that it can parse the full run_spec result format (with report wrapper)
        var baseline = CreateRunSpecResultJson("Test > spec1", "passed");
        var current = CreateRunSpecResultJson("Test > spec1", "failed");

        var result = DiffTools.DiffSpecs(baseline, current);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("hasRegressions").GetBoolean()).IsTrue();
    }

    #endregion

    #region Helpers

    private static string CreateSpecResultJson(int passed, int failed)
    {
        return $$"""
            {
                "success": {{(failed == 0).ToString().ToLower()}},
                "report": {
                    "summary": { "total": {{passed + failed}}, "passed": {{passed}}, "failed": {{failed}} },
                    "contexts": []
                }
            }
            """;
    }

    private static string CreateSpecReportJson(string specPath, string status)
    {
        var parts = specPath.Split(" > ");
        var contextDesc = parts[0];
        var specDesc = parts.Length > 1 ? parts[^1] : parts[0];

        return $$"""
            {
                "summary": { "total": 1, "passed": {{(status == "passed" ? 1 : 0)}}, "failed": {{(status == "failed" ? 1 : 0)}} },
                "contexts": [
                    {
                        "description": "{{contextDesc}}",
                        "specs": [
                            { "description": "{{specDesc}}", "status": "{{status}}" }
                        ],
                        "contexts": []
                    }
                ]
            }
            """;
    }

    private static string CreateRunSpecResultJson(string specPath, string status)
    {
        var parts = specPath.Split(" > ");
        var contextDesc = parts[0];
        var specDesc = parts.Length > 1 ? parts[^1] : parts[0];

        return $$"""
            {
                "success": {{(status == "passed").ToString().ToLower()}},
                "exitCode": {{(status == "passed" ? 0 : 1)}},
                "report": {
                    "summary": { "total": 1, "passed": {{(status == "passed" ? 1 : 0)}}, "failed": {{(status == "failed" ? 1 : 0)}} },
                    "contexts": [
                        {
                            "description": "{{contextDesc}}",
                            "specs": [
                                { "description": "{{specDesc}}", "status": "{{status}}" }
                            ],
                            "contexts": []
                        }
                    ]
                }
            }
            """;
    }

    #endregion
}
