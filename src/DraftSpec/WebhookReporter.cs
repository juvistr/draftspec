using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DraftSpec.Formatters;

namespace DraftSpec.Plugins.Reporters;

/// <summary>
/// Reporter that sends spec results to external systems via HTTP webhooks.
/// Supports Slack, Discord, and generic JSON payloads.
/// </summary>
public class WebhookReporter : IReporter, IDisposable
{
    private readonly WebhookReporterOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// Create a WebhookReporter with the specified options.
    /// </summary>
    /// <param name="options">Webhook configuration options</param>
    public WebhookReporter(WebhookReporterOptions options)
        : this(options, null)
    {
    }

    /// <summary>
    /// Create a WebhookReporter with custom HttpClient (for testing).
    /// </summary>
    /// <param name="options">Webhook configuration options</param>
    /// <param name="httpClient">Custom HttpClient instance</param>
    public WebhookReporter(WebhookReporterOptions options, HttpClient? httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);

        _options = options;

        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(options.TimeoutMs)
            };
            _ownsHttpClient = true;
        }
    }

    /// <summary>
    /// Gets the reporter name identifier.
    /// </summary>
    public string Name => "webhook";

    /// <summary>
    /// Send the spec report to the configured webhook when the run completes.
    /// </summary>
    public async Task OnRunCompletedAsync(SpecReport report)
    {
        // Check if we should skip sending
        if (_options.SendOnFailureOnly && report.Summary.Success)
            return;

        var payload = FormatPayload(report);
        await SendWithRetryAsync(payload);
    }

    private async Task SendWithRetryAsync(string payload)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                // Create fresh content for each attempt (content gets disposed after send)
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, _options.Url);
                request.Content = content;

                // Add authorization header
                if (!string.IsNullOrEmpty(_options.AuthHeader))
                    request.Headers.TryAddWithoutValidation("Authorization", _options.AuthHeader);

                // Add custom headers
                if (_options.CustomHeaders != null)
                {
                    foreach (var (key, value) in _options.CustomHeaders)
                        request.Headers.TryAddWithoutValidation(key, value);
                }

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return; // Success
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastException = ex;

                if (attempt < _options.MaxRetries)
                    await Task.Delay(_options.RetryDelayMs);
            }
        }

        // All retries failed - throw with context
        throw new WebhookDeliveryException(
            $"Failed to deliver webhook after {_options.MaxRetries + 1} attempts",
            lastException);
    }

    private string FormatPayload(SpecReport report)
    {
        return _options.Format switch
        {
            WebhookPayloadFormat.Json => report.ToJson(),
            WebhookPayloadFormat.Slack => FormatSlackPayload(report),
            WebhookPayloadFormat.Discord => FormatDiscordPayload(report),
            WebhookPayloadFormat.Summary => FormatSummaryPayload(report),
            _ => report.ToJson()
        };
    }

    private static string FormatSlackPayload(SpecReport report)
    {
        var summary = report.Summary;
        var status = summary.Success ? ":white_check_mark: PASSED" : ":x: FAILED";
        var color = summary.Success ? "#36a64f" : "#dc3545";

        var payload = new
        {
            attachments = new[]
            {
                new
                {
                    color,
                    blocks = new object[]
                    {
                        new
                        {
                            type = "header",
                            text = new
                            {
                                type = "plain_text",
                                text = $"DraftSpec Results: {status}",
                                emoji = true
                            }
                        },
                        new
                        {
                            type = "section",
                            fields = new[]
                            {
                                new { type = "mrkdwn", text = $"*Total:* {summary.Total}" },
                                new { type = "mrkdwn", text = $"*Passed:* {summary.Passed}" },
                                new { type = "mrkdwn", text = $"*Failed:* {summary.Failed}" },
                                new { type = "mrkdwn", text = $"*Duration:* {summary.DurationMs:F0}ms" }
                            }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(payload, JsonOptionsProvider.Default);
    }

    private static string FormatDiscordPayload(SpecReport report)
    {
        var summary = report.Summary;
        var color = summary.Success ? 0x36a64f : 0xdc3545; // Green or Red

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = summary.Success ? "Tests Passed" : "Tests Failed",
                    color,
                    fields = new[]
                    {
                        new { name = "Total", value = summary.Total.ToString(), inline = true },
                        new { name = "Passed", value = summary.Passed.ToString(), inline = true },
                        new { name = "Failed", value = summary.Failed.ToString(), inline = true },
                        new { name = "Pending", value = summary.Pending.ToString(), inline = true },
                        new { name = "Skipped", value = summary.Skipped.ToString(), inline = true },
                        new { name = "Duration", value = $"{summary.DurationMs:F0}ms", inline = true }
                    },
                    timestamp = report.Timestamp.ToString("o")
                }
            }
        };

        return JsonSerializer.Serialize(payload, JsonOptionsProvider.Default);
    }

    private static string FormatSummaryPayload(SpecReport report)
    {
        var summary = report.Summary;

        var payload = new
        {
            success = summary.Success,
            total = summary.Total,
            passed = summary.Passed,
            failed = summary.Failed,
            pending = summary.Pending,
            skipped = summary.Skipped,
            durationMs = summary.DurationMs,
            timestamp = report.Timestamp,
            source = report.Source
        };

        return JsonSerializer.Serialize(payload, JsonOptionsProvider.Default);
    }

    private static void ValidateOptions(WebhookReporterOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Url))
            throw new ArgumentException("Webhook URL is required", nameof(options));

        if (!Uri.TryCreate(options.Url, UriKind.Absolute, out var uri))
            throw new ArgumentException("Webhook URL must be a valid absolute URI", nameof(options));

        if (uri.Scheme != "http" && uri.Scheme != "https")
            throw new ArgumentException("Webhook URL must use HTTP or HTTPS", nameof(options));

        if (options.TimeoutMs <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(options));

        if (options.MaxRetries < 0)
            throw new ArgumentException("MaxRetries cannot be negative", nameof(options));
    }

    /// <summary>
    /// Dispose the HttpClient if owned.
    /// </summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
