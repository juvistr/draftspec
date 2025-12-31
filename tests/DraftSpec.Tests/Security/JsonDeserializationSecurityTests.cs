using System.Text;
using System.Text.Json;
using DraftSpec.Formatters;

namespace DraftSpec.Tests.Security;

/// <summary>
/// Tests for JSON deserialization DoS vulnerability (CWE-400).
///
/// VULNERABILITY: The current implementation has no depth or size limits:
///   var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
///   return JsonSerializer.Deserialize<SpecReport>(json, options);
///
/// ATTACKS:
/// 1. Deeply nested JSON → stack overflow
/// 2. Huge JSON payload → memory exhaustion
///
/// These tests should FAIL with current implementation and PASS after fix.
/// </summary>
public class JsonDeserializationSecurityTests
{
    #region Depth Limit Tests (Should FAIL with current code)

    /// <summary>
    /// CRITICAL: Deeply nested JSON should be rejected to prevent stack overflow.
    /// Default .NET limit is 64 levels, but current code has no explicit limit.
    /// </summary>
    [Test]
    public async Task FromJson_DeeplyNestedJson_ShouldThrowWithMeaningfulError()
    {
        // Create JSON with 100+ levels of nesting (exceeds safe limit)
        var deepJson = GenerateDeeplyNestedJson(100);

        // This should throw due to depth limit
        var exception = await Assert.ThrowsAsync<JsonException>(() =>
        {
            SpecReport.FromJson(deepJson);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    /// <summary>
    /// Test that reasonable nesting depth (up to 64) still works.
    /// </summary>
    [Test]
    public async Task FromJson_ReasonableNestingDepth_ShouldSucceed()
    {
        // 30 levels is reasonable for a real spec hierarchy
        var json = GenerateValidNestedReport(30);

        var report = SpecReport.FromJson(json);

        await Assert.That(report).IsNotNull();
    }

    /// <summary>
    /// Test boundary: within the limit should work.
    /// Note: Each nesting level creates ~2 JSON depth (object + array),
    /// so 25 context levels = ~50 JSON depth, which is under the 64 limit.
    /// </summary>
    [Test]
    public async Task FromJson_AtDepthLimit_ShouldSucceed()
    {
        // 25 nesting levels = ~50 JSON depth (under 64 limit)
        var json = GenerateValidNestedReport(25);

        var report = SpecReport.FromJson(json);

        await Assert.That(report).IsNotNull();
    }

    /// <summary>
    /// Test boundary: just over the limit should fail.
    /// Note: Each nesting level creates ~2 JSON depth (object + array),
    /// so 35 context levels = ~70 JSON depth, which exceeds the 64 limit.
    /// </summary>
    [Test]
    public async Task FromJson_JustOverDepthLimit_ShouldThrow()
    {
        // 35 nesting levels = ~70 JSON depth (exceeds 64 limit)
        var json = GenerateValidNestedReport(35);

        var exception = await Assert.ThrowsAsync<JsonException>(() =>
        {
            SpecReport.FromJson(json);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region Size Limit Tests (Should FAIL with current code)

    /// <summary>
    /// CRITICAL: Huge JSON payloads should be rejected to prevent memory exhaustion.
    /// A 10MB limit is reasonable for spec reports.
    /// </summary>
    [Test]
    public async Task FromJson_OversizedPayload_ShouldThrowWithMeaningfulError()
    {
        // Create JSON > 10MB
        var hugeJson = GenerateOversizedJson(15_000_000); // 15MB

        // This should throw due to size limit (not implemented yet)
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            SpecReport.FromJson(hugeJson);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).Contains("large");
    }

    /// <summary>
    /// Test that reasonable size (under 10MB) still works.
    /// </summary>
    [Test]
    public async Task FromJson_ReasonableSize_ShouldSucceed()
    {
        // 1MB is reasonable
        var json = GenerateValidJsonOfSize(1_000_000);

        var report = SpecReport.FromJson(json);

        await Assert.That(report).IsNotNull();
    }

    /// <summary>
    /// Test boundary: exactly at the limit should work.
    /// </summary>
    [Test]
    public async Task FromJson_AtSizeLimit_ShouldSucceed()
    {
        // Just under 10MB should work
        var json = GenerateValidJsonOfSize(9_900_000);

        var report = SpecReport.FromJson(json);

        await Assert.That(report).IsNotNull();
    }

    /// <summary>
    /// Test boundary: just over the limit should fail.
    /// </summary>
    [Test]
    public async Task FromJson_JustOverSizeLimit_ShouldThrow()
    {
        // Just over 10MB should fail
        var json = GenerateOversizedJson(10_100_000);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            SpecReport.FromJson(json);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region Performance Tests

    /// <summary>
    /// Size check should fail fast, not after parsing entire payload.
    /// </summary>
    [Test]
    public async Task FromJson_OversizedPayload_ShouldFailFast()
    {
        var hugeJson = GenerateOversizedJson(50_000_000); // 50MB

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            SpecReport.FromJson(hugeJson);
        }
        catch
        {
            // Expected
        }

        sw.Stop();

        // Should fail within 500ms (size check is O(1), but string allocation takes time)
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(500);
    }

    #endregion

    #region Valid JSON Tests (Backwards Compatibility)

    /// <summary>
    /// Normal spec reports should still parse correctly.
    /// </summary>
    [Test]
    public async Task FromJson_ValidReport_ShouldParse()
    {
        var json = """
                   {
                       "timestamp": "2025-12-15T10:00:00Z",
                       "source": "test.spec.csx",
                       "summary": {
                           "total": 5,
                           "passed": 4,
                           "failed": 1,
                           "pending": 0,
                           "skipped": 0,
                           "durationMs": 123.45
                       },
                       "contexts": [
                           {
                               "description": "Test Suite",
                               "specs": [
                                   {
                                       "description": "should pass",
                                       "status": "passed",
                                       "durationMs": 10.5
                                   }
                               ],
                               "contexts": []
                           }
                       ]
                   }
                   """;

        var report = SpecReport.FromJson(json);

        await Assert.That(report).IsNotNull();
        await Assert.That(report.Summary.Total).IsEqualTo(5);
        await Assert.That(report.Summary.Passed).IsEqualTo(4);
    }

    /// <summary>
    /// Empty report should parse correctly.
    /// </summary>
    [Test]
    public async Task FromJson_EmptyReport_ShouldParse()
    {
        var json = """
                   {
                       "timestamp": "2025-12-15T10:00:00Z",
                       "summary": {
                           "total": 0,
                           "passed": 0,
                           "failed": 0,
                           "pending": 0,
                           "skipped": 0,
                           "durationMs": 0
                       },
                       "contexts": []
                   }
                   """;

        var report = SpecReport.FromJson(json);

        await Assert.That(report).IsNotNull();
        await Assert.That(report.Summary.Total).IsEqualTo(0);
    }

    /// <summary>
    /// JSON with unicode characters should parse correctly.
    /// </summary>
    [Test]
    public async Task FromJson_UnicodeContent_ShouldParse()
    {
        var json = """
                   {
                       "timestamp": "2025-12-15T10:00:00Z",
                       "source": "测试.spec.csx",
                       "summary": {
                           "total": 1,
                           "passed": 1,
                           "failed": 0,
                           "pending": 0,
                           "skipped": 0,
                           "durationMs": 1.0
                       },
                       "contexts": [
                           {
                               "description": "テスト 🧪",
                               "specs": [
                                   {
                                       "description": "should support émojis 🎉",
                                       "status": "passed",
                                       "durationMs": 1.0
                                   }
                               ],
                               "contexts": []
                           }
                       ]
                   }
                   """;

        var report = SpecReport.FromJson(json);

        await Assert.That(report).IsNotNull();
        await Assert.That(report.Contexts[0].Description).Contains("テスト");
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Invalid JSON should throw meaningful error.
    /// </summary>
    [Test]
    public async Task FromJson_InvalidJson_ShouldThrowJsonException()
    {
        var invalidJson = "{ not valid json }";

        var exception = await Assert.ThrowsAsync<JsonException>(() =>
        {
            SpecReport.FromJson(invalidJson);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    /// <summary>
    /// Empty string should throw meaningful error.
    /// </summary>
    [Test]
    public async Task FromJson_EmptyString_ShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<JsonException>(() =>
        {
            SpecReport.FromJson("");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    /// <summary>
    /// Null should throw ArgumentNullException.
    /// </summary>
    [Test]
    public async Task FromJson_Null_ShouldThrowArgumentNullException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            SpecReport.FromJson(null!);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region Helper Methods

    private static string GenerateDeeplyNestedJson(int depth)
    {
        var sb = new StringBuilder();

        // Open nested contexts
        sb.Append(
            "{\"timestamp\":\"2025-12-15T10:00:00Z\",\"summary\":{\"total\":0,\"passed\":0,\"failed\":0,\"pending\":0,\"skipped\":0,\"durationMs\":0},\"contexts\":[");

        for (var i = 0; i < depth; i++) sb.Append("{\"description\":\"level " + i + "\",\"specs\":[],\"contexts\":[");

        // Close all contexts
        for (var i = 0; i < depth; i++) sb.Append("]}");

        sb.Append("]}");

        return sb.ToString();
    }

    private static string GenerateValidNestedReport(int depth)
    {
        // Similar to above but ensures valid structure
        return GenerateDeeplyNestedJson(depth);
    }

    private static string GenerateOversizedJson(int targetSize)
    {
        var sb = new StringBuilder(targetSize + 1000);

        sb.Append(
            "{\"timestamp\":\"2025-12-15T10:00:00Z\",\"summary\":{\"total\":1,\"passed\":1,\"failed\":0,\"pending\":0,\"skipped\":0,\"durationMs\":0},\"contexts\":[{\"description\":\"Test\",\"specs\":[{\"description\":\"test with padding: ");

        // Add padding to reach target size
        var paddingNeeded = targetSize - sb.Length - 100;
        if (paddingNeeded > 0) sb.Append(new string('x', paddingNeeded));

        sb.Append("\",\"status\":\"passed\",\"durationMs\":1}],\"contexts\":[]}]}");

        return sb.ToString();
    }

    private static string GenerateValidJsonOfSize(int targetSize)
    {
        return GenerateOversizedJson(targetSize);
    }

    #endregion
}
