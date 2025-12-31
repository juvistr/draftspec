namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock DSL manager that tracks Reset() calls.
/// </summary>
class MockDslManager : IDslManager
{
    public int ResetCalls { get; private set; }

    public void Reset()
    {
        ResetCalls++;
    }
}
