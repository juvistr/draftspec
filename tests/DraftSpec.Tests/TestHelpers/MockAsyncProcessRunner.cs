using System.Diagnostics;
using System.Text;
using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.TestHelpers;

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

/// <summary>
/// Mock implementation of IAsyncProcessHandle for testing.
/// </summary>
public class MockAsyncProcessHandle : IAsyncProcessHandle
{
    private string _stdout = "";
    private string _stderr = "";
    private int _exitCode;
    private Exception? _throwOnWaitForExit;

    public bool WaitForExitCalled { get; private set; }
    public bool KillCalled { get; private set; }
    public bool KillEntireProcessTree { get; private set; }
    public bool Disposed { get; private set; }

    public MockAsyncProcessHandle WithStdout(string stdout)
    {
        _stdout = stdout;
        return this;
    }

    public MockAsyncProcessHandle WithStderr(string stderr)
    {
        _stderr = stderr;
        return this;
    }

    public MockAsyncProcessHandle WithExitCode(int exitCode)
    {
        _exitCode = exitCode;
        return this;
    }

    public MockAsyncProcessHandle ThrowsOnWaitForExit(Exception exception)
    {
        _throwOnWaitForExit = exception;
        return this;
    }

    public StreamReader StandardOutput => new(new MemoryStream(Encoding.UTF8.GetBytes(_stdout)));
    public StreamReader StandardError => new(new MemoryStream(Encoding.UTF8.GetBytes(_stderr)));
    public int ExitCode => _exitCode;

    public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        WaitForExitCalled = true;

        if (_throwOnWaitForExit is OperationCanceledException)
        {
            // Wait for the cancellation token to be triggered (simulates real timeout)
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw _throwOnWaitForExit;
            }
        }

        if (_throwOnWaitForExit is not null)
            throw _throwOnWaitForExit;
    }

    public void Kill(bool entireProcessTree = false)
    {
        KillCalled = true;
        KillEntireProcessTree = entireProcessTree;
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
