using System.Net;
using System.Text.Json;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.Plugins.Reporters;

namespace DraftSpec.Tests.Plugins.Reporters;

/// <summary>
/// Tests for WebhookReporter.
/// </summary>
public class WebhookReporterTests
{
    #region Construction and Validation

    [Test]
    public async Task Constructor_ValidOptions_Succeeds()
    {
        var options = new WebhookReporterOptions { Url = "https://example.com/webhook" };

        using var reporter = new WebhookReporter(options);

        await Assert.That(reporter.Name).IsEqualTo("webhook");
    }

    [Test]
    public async Task Constructor_NullUrl_ThrowsArgumentException()
    {
        await Assert.That(() => new WebhookReporter(new WebhookReporterOptions { Url = null! }))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_EmptyUrl_ThrowsArgumentException()
    {
        await Assert.That(() => new WebhookReporter(new WebhookReporterOptions { Url = "" }))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_InvalidUrl_ThrowsArgumentException()
    {
        await Assert.That(() => new WebhookReporter(new WebhookReporterOptions { Url = "not-a-url" }))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_NonHttpUrl_ThrowsArgumentException()
    {
        await Assert.That(() => new WebhookReporter(new WebhookReporterOptions { Url = "ftp://example.com" }))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_NegativeTimeout_ThrowsArgumentException()
    {
        await Assert.That(() => new WebhookReporter(new WebhookReporterOptions
            {
                Url = "https://example.com",
                TimeoutMs = -1
            }))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_NegativeRetries_ThrowsArgumentException()
    {
        await Assert.That(() => new WebhookReporter(new WebhookReporterOptions
            {
                Url = "https://example.com",
                MaxRetries = -1
            }))
            .Throws<ArgumentException>();
    }

    #endregion

    #region Payload Formats

    [Test]
    public async Task OnRunCompletedAsync_JsonFormat_SendsFullReport()
    {
        var handler = new MockHttpHandler();
        using var httpClient = new HttpClient(handler);

        var options = new WebhookReporterOptions
        {
            Url = "https://example.com/webhook",
            Format = WebhookPayloadFormat.Json
        };

        using var reporter = new WebhookReporter(options, httpClient);
        var report = CreateTestReport(passed: 3, failed: 1);

        await reporter.OnRunCompletedAsync(report);

        await Assert.That(handler.RequestCount).IsEqualTo(1);
        var payload = handler.LastRequestContent!;
        await Assert.That(payload).Contains("\"summary\"");
        await Assert.That(payload).Contains("\"contexts\"");
    }

    [Test]
    public async Task OnRunCompletedAsync_SlackFormat_SendsSlackPayload()
    {
        var handler = new MockHttpHandler();
        using var httpClient = new HttpClient(handler);

        var options = new WebhookReporterOptions
        {
            Url = "https://hooks.slack.com/webhook",
            Format = WebhookPayloadFormat.Slack
        };

        using var reporter = new WebhookReporter(options, httpClient);
        var report = CreateTestReport(passed: 5, failed: 0);

        await reporter.OnRunCompletedAsync(report);

        var payload = handler.LastRequestContent!;
        await Assert.That(payload).Contains("attachments");
        await Assert.That(payload).Contains("blocks");
        await Assert.That(payload).Contains("PASSED");
    }

    [Test]
    public async Task OnRunCompletedAsync_SlackFormat_FailedTests_ShowsFailed()
    {
        var handler = new MockHttpHandler();
        using var httpClient = new HttpClient(handler);

        var options = new WebhookReporterOptions
        {
            Url = "https://hooks.slack.com/webhook",
            Format = WebhookPayloadFormat.Slack
        };

        using var reporter = new WebhookReporter(options, httpClient);
        var report = CreateTestReport(passed: 3, failed: 2);

        await reporter.OnRunCompletedAsync(report);

        var payload = handler.LastRequestContent!;
        await Assert.That(payload).Contains("FAILED");
        await Assert.That(payload).Contains("#dc3545"); // Red color
    }

    [Test]
    public async Task OnRunCompletedAsync_DiscordFormat_SendsEmbed()
    {
        var handler = new MockHttpHandler();
        using var httpClient = new HttpClient(handler);

        var options = new WebhookReporterOptions
        {
            Url = "https://discord.com/api/webhooks/123",
            Format = WebhookPayloadFormat.Discord
        };

        using var reporter = new WebhookReporter(options, httpClient);
        var report = CreateTestReport(passed: 10, failed: 0);

        await reporter.OnRunCompletedAsync(report);

        var payload = handler.LastRequestContent!;
        await Assert.That(payload).Contains("embeds");
        await Assert.That(payload).Contains("Tests Passed");
        await Assert.That(payload).Contains("fields");
    }

    [Test]
    public async Task OnRunCompletedAsync_DiscordFormat_FailedTests_ShowsRed()
    {
        var handler = new MockHttpHandler();
        using var httpClient = new HttpClient(handler);

        var options = new WebhookReporterOptions
        {
            Url = "https://discord.com/api/webhooks/123",
            Format = WebhookPayloadFormat.Discord
        };

        using var reporter = new WebhookReporter(options, httpClient);
        var report = CreateTestReport(passed: 5, failed: 5);

        await reporter.OnRunCompletedAsync(report);

        var payload = handler.LastRequestContent!;
        await Assert.That(payload).Contains("Tests Failed");
        // Verify red color is present (0xdc3545 = 14435653)
        var json = JsonDocument.Parse(payload);
        var color = json.RootElement.GetProperty("embeds")[0].GetProperty("color").GetInt32();
        await Assert.That(color).IsEqualTo(0xdc3545);
    }

    [Test]
    public async Task OnRunCompletedAsync_SummaryFormat_SendsMinimalPayload()
    {
        var handler = new MockHttpHandler();
        using var httpClient = new HttpClient(handler);

        var options = new WebhookReporterOptions
        {
            Url = "https://example.com/webhook",
            Format = WebhookPayloadFormat.Summary
        };

        using var reporter = new WebhookReporter(options, httpClient);
        var report = CreateTestReport(passed: 8, failed: 2);

        await reporter.OnRunCompletedAsync(report);

        var payload = handler.LastRequestContent!;
        var json = JsonDocument.Parse(payload);

        await Assert.That(json.RootElement.GetProperty("success").GetBoolean()).IsFalse();
        await Assert.That(json.RootElement.GetProperty("total").GetInt32()).IsEqualTo(10);
        await Assert.That(json.RootElement.GetProperty("passed").GetInt32()).IsEqualTo(8);
        await Assert.That(json.RootElement.GetProperty("failed").GetInt32()).IsEqualTo(2);
    }

    #endregion

    #region SendOnFailureOnly

    [Test]
    public async Task OnRunCompletedAsync_SendOnFailureOnly_PassingTests_DoesNotSend()
    {
        var handler = new MockHttpHandler();
        using var httpClient = new HttpClient(handler);

        var options = new WebhookReporterOptions
        {
            Url = "https://example.com/webhook",
            SendOnFailureOnly = true
        };

        using var reporter = new WebhookReporter(options, httpClient);
        var report = CreateTestReport(passed: 10, failed: 0);

        await reporter.OnRunCompletedAsync(report);

        await Assert.That(handler.RequestCount).IsEqualTo(0);
    }

    [Test]
    public async Task OnRunCompletedAsync_SendOnFailureOnly_FailingTests_Sends()
    {
        var handler = new MockHttpHandler();
        using var httpClient = new HttpClient(handler);

        var options = new WebhookReporterOptions
        {
            Url = "https://example.com/webhook",
            SendOnFailureOnly = true
        };

        using var reporter = new WebhookReporter(options, httpClient);
        var report = CreateTestReport(passed: 8, failed: 2);

        await reporter.OnRunCompletedAsync(report);

        await Assert.That(handler.RequestCount).IsEqualTo(1);
    }

    #endregion

    #region Headers

    [Test]
    public async Task OnRunCompletedAsync_WithAuthHeader_SendsAuthorization()
    {
        var handler = new MockHttpHandler();
        using var httpClient = new HttpClient(handler);

        var options = new WebhookReporterOptions
        {
            Url = "https://example.com/webhook",
            AuthHeader = "Bearer token123"
        };

        using var reporter = new WebhookReporter(options, httpClient);
        var report = CreateTestReport(passed: 1, failed: 0);

        await reporter.OnRunCompletedAsync(report);

        await Assert.That(handler.LastAuthHeader).IsEqualTo("Bearer token123");
    }

    [Test]
    public async Task OnRunCompletedAsync_WithCustomHeaders_SendsHeaders()
    {
        var handler = new MockHttpHandler();
        using var httpClient = new HttpClient(handler);

        var options = new WebhookReporterOptions
        {
            Url = "https://example.com/webhook",
            CustomHeaders = new Dictionary<string, string>
            {
                ["X-Custom-Header"] = "custom-value",
                ["X-Another"] = "another-value"
            }
        };

        using var reporter = new WebhookReporter(options, httpClient);
        var report = CreateTestReport(passed: 1, failed: 0);

        await reporter.OnRunCompletedAsync(report);

        await Assert.That(handler.LastCustomHeaders!).ContainsKey("X-Custom-Header");
        await Assert.That(handler.LastCustomHeaders!["X-Custom-Header"]).IsEqualTo("custom-value");
    }

    #endregion

    #region Retry Logic

    [Test]
    public async Task OnRunCompletedAsync_ServerError_Retries()
    {
        var handler = new MockHttpHandler
        {
            FailCount = 1,
            FailStatusCode = HttpStatusCode.InternalServerError
        };
        using var httpClient = new HttpClient(handler);

        var options = new WebhookReporterOptions
        {
            Url = "https://example.com/webhook",
            MaxRetries = 2,
            RetryDelayMs = 10 // Short delay for tests
        };

        using var reporter = new WebhookReporter(options, httpClient);
        var report = CreateTestReport(passed: 1, failed: 0);

        await reporter.OnRunCompletedAsync(report);

        await Assert.That(handler.RequestCount).IsEqualTo(2); // 1 fail + 1 success
    }

    [Test]
    public async Task OnRunCompletedAsync_AllRetriesFail_ThrowsWebhookDeliveryException()
    {
        var handler = new MockHttpHandler
        {
            FailCount = 10, // More than retries
            FailStatusCode = HttpStatusCode.ServiceUnavailable
        };
        using var httpClient = new HttpClient(handler);

        var options = new WebhookReporterOptions
        {
            Url = "https://example.com/webhook",
            MaxRetries = 2,
            RetryDelayMs = 10
        };

        using var reporter = new WebhookReporter(options, httpClient);
        var report = CreateTestReport(passed: 1, failed: 0);

        await Assert.That(async () => await reporter.OnRunCompletedAsync(report))
            .Throws<WebhookDeliveryException>();

        await Assert.That(handler.RequestCount).IsEqualTo(3); // 1 initial + 2 retries
    }

    #endregion

    #region Helpers

    private static SpecReport CreateTestReport(int passed, int failed, int pending = 0, int skipped = 0)
    {
        return new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Source = "test",
            Summary = new SpecSummary
            {
                Total = passed + failed + pending + skipped,
                Passed = passed,
                Failed = failed,
                Pending = pending,
                Skipped = skipped,
                DurationMs = 1234.5
            },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Test Suite",
                    Specs = [],
                    Contexts = []
                }
            ]
        };
    }

    /// <summary>
    /// Mock HTTP handler for testing webhook delivery.
    /// </summary>
    private class MockHttpHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public string? LastRequestContent { get; private set; }
        public string? LastAuthHeader { get; private set; }
        public Dictionary<string, string>? LastCustomHeaders { get; private set; }
        public int FailCount { get; set; }
        public HttpStatusCode FailStatusCode { get; set; } = HttpStatusCode.InternalServerError;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;

            // Capture request details
            if (request.Content != null)
                LastRequestContent = await request.Content.ReadAsStringAsync(cancellationToken);

            if (request.Headers.Authorization != null)
                LastAuthHeader = request.Headers.Authorization.ToString();

            LastCustomHeaders = request.Headers
                .Where(h => h.Key.StartsWith("X-"))
                .ToDictionary(h => h.Key, h => string.Join(",", h.Value));

            // Simulate failures
            if (RequestCount <= FailCount)
            {
                return new HttpResponseMessage(FailStatusCode);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    #endregion
}
