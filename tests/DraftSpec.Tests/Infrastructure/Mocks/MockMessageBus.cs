using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Messages;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IMessageBus for testing.
/// Captures all published messages.
/// </summary>
internal class MockMessageBus : IMessageBus
{
    public List<(IDataProducer Producer, IData Data)> PublishedMessages { get; } = [];

    public Task PublishAsync(IDataProducer dataProducer, IData data)
    {
        PublishedMessages.Add((dataProducer, data));
        return Task.CompletedTask;
    }
}
