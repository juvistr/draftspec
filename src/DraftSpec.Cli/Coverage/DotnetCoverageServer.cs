using System.Diagnostics;

namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Implementation that manages a dotnet-coverage server process.
/// </summary>
public class DotnetCoverageServer : ICoverageServer
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly string _outputDirectory;
    private readonly string _format;
    private IProcessHandle? _serverProcess;
    private bool _serverStarted;
    private bool _disposed;

    public DotnetCoverageServer(
        IProcessRunner processRunner,
        IFileSystem fileSystem,
        string outputDirectory,
        string format = "cobertura")
    {
        _processRunner = processRunner;
        _fileSystem = fileSystem;
        _outputDirectory = outputDirectory;
        _format = format.ToLowerInvariant();
        SessionId = $"draftspec-{Guid.NewGuid():N}";
        CoverageFile = Path.Combine(_outputDirectory, $"coverage.{GetFileExtension()}");
    }

    public string SessionId { get; }
    public string CoverageFile { get; }

    public bool IsRunning => _serverStarted && _serverProcess is { HasExited: false };

    public bool Start()
    {
        if (_serverStarted)
            return IsRunning;

        EnsureOutputDirectory();

        // Start: dotnet-coverage collect --server-mode --session-id {id} -o {file} -f {format}
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet-coverage",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("collect");
        startInfo.ArgumentList.Add("--server-mode");
        startInfo.ArgumentList.Add("--session-id");
        startInfo.ArgumentList.Add(SessionId);
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(CoverageFile);
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add(_format);

        try
        {
            _serverProcess = _processRunner.StartProcess(startInfo);
            _serverStarted = true;

            // Give the server a moment to initialize
            Thread.Sleep(500);

            return _serverProcess is { HasExited: false };
        }
        catch
        {
            _serverStarted = false;
            return false;
        }
    }

    public bool Shutdown()
    {
        if (!_serverStarted)
            return false;

        // Send shutdown command
        var result = _processRunner.Run("dotnet-coverage", ["shutdown", SessionId]);

        // Wait for server process to exit
        if (_serverProcess != null)
        {
            try
            {
                _serverProcess.WaitForExit(5000);
            }
            catch
            {
                // Ignore timeout
            }
        }

        _serverStarted = false;
        return result.Success && _fileSystem.FileExists(CoverageFile);
    }

    private void EnsureOutputDirectory()
    {
        if (!_fileSystem.DirectoryExists(_outputDirectory))
            _fileSystem.CreateDirectory(_outputDirectory);
    }

    private string GetFileExtension() => _format switch
    {
        "cobertura" => "cobertura.xml",
        "xml" => "xml",
        "coverage" => "coverage",
        _ => "cobertura.xml"
    };

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_serverStarted)
        {
            try
            {
                Shutdown();
            }
            catch
            {
                // Best-effort cleanup
            }
        }

        if (_serverProcess != null)
        {
            try
            {
                if (!_serverProcess.HasExited)
                    _serverProcess.Kill();
                _serverProcess.Dispose();
            }
            catch
            {
                // Best-effort cleanup
            }
        }
        GC.SuppressFinalize(this);
    }
}
