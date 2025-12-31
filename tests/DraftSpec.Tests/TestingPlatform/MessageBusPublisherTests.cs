using DraftSpec.TestingPlatform;
using DraftSpec.Tests.Infrastructure.Mocks;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;

namespace DraftSpec.Tests.TestingPlatform;

/// <summary>
/// Tests for MessageBusPublisher.
/// Verifies the adapter correctly delegates to IMessageBus.
/// </summary>
public class MessageBusPublisherTests
{
    #region Constructor

    [Test]
    public void Constructor_NullMessageBus_ThrowsArgumentNullException()
    {
        var dataProducer = new MockDataProducer();
        var sessionUid = new SessionUid("test-session");

        Assert.Throws<ArgumentNullException>(() =>
            new MessageBusPublisher(null!, dataProducer, sessionUid));
    }

    [Test]
    public void Constructor_NullDataProducer_ThrowsArgumentNullException()
    {
        var messageBus = new MockMessageBus();
        var sessionUid = new SessionUid("test-session");

        Assert.Throws<ArgumentNullException>(() =>
            new MessageBusPublisher(messageBus, null!, sessionUid));
    }

    [Test]
    public async Task Constructor_ValidArguments_CreatesInstance()
    {
        var messageBus = new MockMessageBus();
        var dataProducer = new MockDataProducer();
        var sessionUid = new SessionUid("test-session");

        var publisher = new MessageBusPublisher(messageBus, dataProducer, sessionUid);

        await Assert.That(publisher).IsNotNull();
    }

    #endregion

    #region PublishAsync

    [Test]
    public async Task PublishAsync_PublishesToMessageBus()
    {
        var messageBus = new MockMessageBus();
        var dataProducer = new MockDataProducer();
        var sessionUid = new SessionUid("test-session");
        var publisher = new MessageBusPublisher(messageBus, dataProducer, sessionUid);

        var testNode = new TestNode
        {
            Uid = new TestNodeUid("test-1"),
            DisplayName = "Test 1"
        };

        await publisher.PublishAsync(testNode);

        await Assert.That(messageBus.PublishedMessages.Count).IsEqualTo(1);
    }

    [Test]
    public async Task PublishAsync_UsesCorrectDataProducer()
    {
        var messageBus = new MockMessageBus();
        var dataProducer = new MockDataProducer();
        var sessionUid = new SessionUid("test-session");
        var publisher = new MessageBusPublisher(messageBus, dataProducer, sessionUid);

        var testNode = new TestNode
        {
            Uid = new TestNodeUid("test-1"),
            DisplayName = "Test 1"
        };

        await publisher.PublishAsync(testNode);

        await Assert.That(messageBus.PublishedMessages[0].Producer).IsSameReferenceAs(dataProducer);
    }

    [Test]
    public async Task PublishAsync_CreatesTestNodeUpdateMessage()
    {
        var messageBus = new MockMessageBus();
        var dataProducer = new MockDataProducer();
        var sessionUid = new SessionUid("test-session");
        var publisher = new MessageBusPublisher(messageBus, dataProducer, sessionUid);

        var testNode = new TestNode
        {
            Uid = new TestNodeUid("test-1"),
            DisplayName = "Test 1"
        };

        await publisher.PublishAsync(testNode);

        await Assert.That(messageBus.PublishedMessages[0].Data).IsAssignableTo<TestNodeUpdateMessage>();
    }

    #endregion
}
