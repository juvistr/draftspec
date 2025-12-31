using System.Diagnostics;
using DraftSpec.Cli;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock process handle for unit testing.
/// </summary>
public class MockProcessHandle : IProcessHandle
{
    /// <summary>
    /// Get or set whether the process has exited.
    /// </summary>
    public bool HasExited { get; set; }

    /// <summary>
    /// When true, WaitForExit() throws an exception.
    /// </summary>
    public bool ThrowOnWaitForExit { get; set; }

    /// <summary>
    /// When true, Kill() throws an exception.
    /// </summary>
    public bool ThrowOnKill { get; set; }

    /// <summary>
    /// Whether WaitForExit() was called.
    /// </summary>
    public bool WaitForExitCalled { get; private set; }

    /// <summary>
    /// Whether Kill() was called.
    /// </summary>
    public bool KillCalled { get; private set; }

    /// <summary>
    /// Whether Dispose() was called.
    /// </summary>
    public bool DisposeCalled { get; private set; }

    public bool WaitForExit(int milliseconds)
    {
        WaitForExitCalled = true;
        if (ThrowOnWaitForExit)
            throw new InvalidOperationException("Mock exception on WaitForExit");
        return true;
    }

    public void Kill()
    {
        if (ThrowOnKill)
            throw new InvalidOperationException("Mock exception on Kill");
        KillCalled = true;
    }

    public void Dispose()
    {
        DisposeCalled = true;
    }
}
