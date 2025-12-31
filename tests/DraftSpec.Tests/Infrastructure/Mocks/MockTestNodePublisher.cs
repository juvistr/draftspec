using DraftSpec.TestingPlatform;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of ITestNodePublisher for testing.
/// Captures all published test nodes.
/// </summary>
internal class MockTestNodePublisher : ITestNodePublisher
{
    public List<TestNode> PublishedNodes { get; } = [];

    public Task PublishAsync(TestNode node, CancellationToken cancellationToken = default)
    {
        PublishedNodes.Add(node);
        return Task.CompletedTask;
    }
}
