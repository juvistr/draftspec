using System.Diagnostics;
using System.Text;
using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Infrastructure.Mocks;

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
