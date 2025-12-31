using System.Diagnostics;
using DraftSpec.Cli;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock process runner for testing.
/// Supports configurable results, process handles, and error simulation.
/// </summary>
public class MockProcessRunner : IProcessRunner
{
    private readonly Queue<ProcessResult> _runResults = new();

    /// <summary>
    /// The last ProcessStartInfo passed to StartProcess().
    /// </summary>
    public ProcessStartInfo? LastStartInfo { get; private set; }

    /// <summary>
    /// The process handle to return from StartProcess().
    /// </summary>
    public IProcessHandle? ProcessHandleToReturn { get; set; }

    /// <summary>
    /// When true, StartProcess() throws InvalidOperationException.
    /// </summary>
    public bool ThrowOnStartProcess { get; set; }

    /// <summary>
    /// When true, Run() and RunDotnet() throw InvalidOperationException.
    /// </summary>
    public bool ThrowOnRun { get; set; }

    /// <summary>
    /// Count of StartProcess() calls.
    /// </summary>
    public int StartProcessCallCount { get; private set; }

    /// <summary>
    /// Records of Run() calls for assertions.
    /// </summary>
    public List<(string FileName, IEnumerable<string> Arguments)> RunCalls { get; } = [];

    /// <summary>
    /// Records of RunDotnet() calls for assertions.
    /// </summary>
    public List<(IEnumerable<string> Args, string? WorkingDir)> RunDotnetCalls { get; } = [];

    /// <summary>
    /// Records of StartProcess() calls for assertions.
    /// </summary>
    public List<ProcessStartInfo> StartProcessCalls { get; } = [];

    /// <summary>
    /// Add a result to return from Run() or RunDotnet().
    /// </summary>
    public void AddResult(ProcessResult result) => _runResults.Enqueue(result);

    /// <summary>
    /// Add a result to return from Run() or RunDotnet().
    /// </summary>
    public void AddRunResult(ProcessResult result) => _runResults.Enqueue(result);

    /// <summary>
    /// Set the process handle to return from StartProcess().
    /// </summary>
    public void SetProcessHandle(IProcessHandle handle) => ProcessHandleToReturn = handle;

    public ProcessResult Run(
        string fileName,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        RunCalls.Add((fileName, arguments.ToList()));
        if (ThrowOnRun)
            throw new InvalidOperationException("Mock exception on Run");
        return _runResults.Count > 0 ? _runResults.Dequeue() : new ProcessResult("", "", 0);
    }

    public ProcessResult RunDotnet(
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        RunDotnetCalls.Add((arguments.ToList(), workingDirectory));
        if (ThrowOnRun)
            throw new InvalidOperationException("Mock exception on RunDotnet");
        return _runResults.Count > 0 ? _runResults.Dequeue() : new ProcessResult("", "", 0);
    }

    public IProcessHandle StartProcess(ProcessStartInfo startInfo)
    {
        StartProcessCallCount++;
        StartProcessCalls.Add(startInfo);
        LastStartInfo = startInfo;

        if (ThrowOnStartProcess)
            throw new InvalidOperationException("Process start failed");

        return ProcessHandleToReturn ?? new MockProcessHandle();
    }
}
