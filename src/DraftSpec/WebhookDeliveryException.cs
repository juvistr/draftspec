using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DraftSpec.Formatters;

namespace DraftSpec.Plugins.Reporters;

/// <summary>
/// Exception thrown when webhook delivery fails after all retries.
/// </summary>
public class WebhookDeliveryException : Exception
{
    /// <summary>
    /// Create a new WebhookDeliveryException.
    /// </summary>
    /// <param name="message">Error message describing the failure</param>
    /// <param name="innerException">The underlying exception that caused the failure</param>
    public WebhookDeliveryException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
