using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IDataProducer for testing.
/// </summary>
internal class MockDataProducer : IDataProducer
{
    public string Uid => "MockDataProducer";
    public string Version => "1.0.0";
    public string DisplayName => "Mock Data Producer";
    public string Description => "Mock data producer for testing";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);
}
