using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Wraps IMessageBus in ITestNodePublisher for use with TestOrchestrator.
/// Acts as an adapter between the MTP infrastructure and the testable orchestration layer.
/// </summary>
internal class MessageBusPublisher : ITestNodePublisher
{
    private readonly IMessageBus _messageBus;
    private readonly IDataProducer _dataProducer;
    private readonly SessionUid _sessionUid;

    /// <summary>
    /// Creates a new message bus publisher.
    /// </summary>
    /// <param name="messageBus">The MTP message bus to publish to.</param>
    /// <param name="dataProducer">The data producer (typically the test framework).</param>
    /// <param name="sessionUid">The session UID for the test session.</param>
    public MessageBusPublisher(IMessageBus messageBus, IDataProducer dataProducer, SessionUid sessionUid)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _dataProducer = dataProducer ?? throw new ArgumentNullException(nameof(dataProducer));
        _sessionUid = sessionUid;
    }

    /// <inheritdoc />
    public async Task PublishAsync(TestNode node, CancellationToken cancellationToken = default)
    {
        await _messageBus.PublishAsync(
            _dataProducer,
            new TestNodeUpdateMessage(_sessionUid, node));
    }
}
