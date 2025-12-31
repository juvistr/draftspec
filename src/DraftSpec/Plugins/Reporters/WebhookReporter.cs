using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DraftSpec.Formatters;

namespace DraftSpec.Plugins.Reporters;

/// <summary>
/// Payload format for webhook notifications.
/// </summary>
public enum WebhookPayloadFormat
{
    /// <summary>
    /// Full SpecReport as JSON.
    /// </summary>
    Json,

    /// <summary>
    /// Slack-formatted message with blocks.
    /// </summary>
    Slack,

    /// <summary>
    /// Discord embed with color-coded status.
    /// </summary>
    Discord,

    /// <summary>
    /// Minimal summary JSON payload.
    /// </summary>
    Summary
}
