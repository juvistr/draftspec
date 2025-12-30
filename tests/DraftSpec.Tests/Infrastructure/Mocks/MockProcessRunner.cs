using System.Diagnostics;
using DraftSpec.Cli;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock process runner for unit testing process execution without actually spawning processes.
/// </summary>
public class MockProcessRunner : IProcessRunner
{
    private readonly Queue<ProcessResult> _results = new();
    private MockProcessHandle? _processHandle;

    /// <summary>
    /// Recorded Run calls for assertions.
    /// </summary>
    public List<(string FileName, List<string> Arguments, string? WorkingDir)> RunCalls { get; } = [];

    /// <summary>
    /// Number of times StartProcess was called.
    /// </summary>
    public int StartProcessCalls { get; private set; }

    /// <summary>
    /// When true, Run() throws an exception.
    /// </summary>
    public bool ThrowOnRun { get; set; }

    /// <summary>
    /// When true, StartProcess() throws an exception.
    /// </summary>
    public bool ThrowOnStartProcess { get; set; }

    /// <summary>
    /// Queue a result to be returned by the next Run() call.
    /// </summary>
    public MockProcessRunner AddResult(ProcessResult result)
    {
        _results.Enqueue(result);
        return this;
    }

    /// <summary>
    /// Set the process handle returned by StartProcess().
    /// </summary>
    public MockProcessRunner SetProcessHandle(MockProcessHandle handle)
    {
        _processHandle = handle;
        return this;
    }

    public ProcessResult Run(
        string fileName,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        if (ThrowOnRun)
            throw new InvalidOperationException("Mock exception on Run");

        RunCalls.Add((fileName, arguments.ToList(), workingDirectory));
        return _results.Count > 0 ? _results.Dequeue() : new ProcessResult("", "", 0);
    }

    public ProcessResult RunDotnet(
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        return Run("dotnet", arguments, workingDirectory, environmentVariables);
    }

    public IProcessHandle StartProcess(ProcessStartInfo startInfo)
    {
        if (ThrowOnStartProcess)
            throw new InvalidOperationException("Mock exception on StartProcess");

        StartProcessCalls++;
        return _processHandle ?? new MockProcessHandle { HasExited = false };
    }
}

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
    /// Whether Kill() was called.
    /// </summary>
    public bool KillCalled { get; private set; }

    /// <summary>
    /// Whether Dispose() was called.
    /// </summary>
    public bool DisposeCalled { get; private set; }

    public bool WaitForExit(int milliseconds)
    {
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
