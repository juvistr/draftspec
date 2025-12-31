using System.Diagnostics;

namespace DraftSpec.Cli;

/// <summary>
/// Implementation that wraps System.Diagnostics.Process.
/// </summary>
public class SystemProcessHandle : IProcessHandle
{
    private readonly Process _process;

    public SystemProcessHandle(Process process)
    {
        _process = process;
    }

    public bool HasExited => _process.HasExited;

    public bool WaitForExit(int milliseconds) => _process.WaitForExit(milliseconds);

    public void Kill() => _process.Kill();

    public void Dispose()
    {
        _process.Dispose();
        GC.SuppressFinalize(this);
    }
}
