using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DraftSpec.Formatters;

namespace DraftSpec.Plugins.Reporters;

/// <summary>
/// Configuration options for the webhook reporter.
/// </summary>
public class WebhookReporterOptions
{
    /// <summary>
    /// The webhook URL to POST to.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Optional authorization header value (e.g., "Bearer token123").
    /// </summary>
    public string? AuthHeader { get; init; }

    /// <summary>
    /// The payload format to use. Default: Json.
    /// </summary>
    public WebhookPayloadFormat Format { get; init; } = WebhookPayloadFormat.Json;

    /// <summary>
    /// Only send webhook on test failures. Default: false.
    /// </summary>
    public bool SendOnFailureOnly { get; init; }

    /// <summary>
    /// HTTP timeout in milliseconds. Default: 5000.
    /// </summary>
    public int TimeoutMs { get; init; } = 5000;

    /// <summary>
    /// Number of retry attempts on failure. Default: 1.
    /// </summary>
    public int MaxRetries { get; init; } = 1;

    /// <summary>
    /// Delay between retries in milliseconds. Default: 1000.
    /// </summary>
    public int RetryDelayMs { get; init; } = 1000;

    /// <summary>
    /// Custom headers to include in the request.
    /// </summary>
    public Dictionary<string, string>? CustomHeaders { get; init; }
}
