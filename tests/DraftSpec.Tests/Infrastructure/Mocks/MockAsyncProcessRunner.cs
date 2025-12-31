using System.Diagnostics;
using System.Text;
using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IAsyncProcessRunner for testing.
/// </summary>
public class MockAsyncProcessRunner : IAsyncProcessRunner
{
    private readonly Queue<IAsyncProcessHandle> _handles = new();
    private Exception? _throwOnStart;

    public List<ProcessStartInfo> StartCalls { get; } = [];

    public MockAsyncProcessRunner ReturnsHandle(IAsyncProcessHandle handle)
    {
        _handles.Enqueue(handle);
        return this;
    }

    public MockAsyncProcessRunner ThrowsOnStart(Exception exception)
    {
        _throwOnStart = exception;
        return this;
    }

    public Task<IAsyncProcessHandle> StartAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default)
    {
        StartCalls.Add(startInfo);

        if (_throwOnStart is not null)
            throw _throwOnStart;

        if (_handles.Count == 0)
            throw new InvalidOperationException("No mock handles configured");

        return Task.FromResult(_handles.Dequeue());
    }
}
