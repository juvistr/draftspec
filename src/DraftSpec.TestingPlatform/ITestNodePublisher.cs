using Microsoft.Testing.Platform.Extensions.Messages;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Publishes test nodes to the test framework infrastructure.
/// Abstracts the IMessageBus dependency for testability.
/// </summary>
internal interface ITestNodePublisher
{
    /// <summary>
    /// Publishes a test node to the test runner.
    /// </summary>
    /// <param name="node">The test node to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(TestNode node, CancellationToken cancellationToken = default);
}
