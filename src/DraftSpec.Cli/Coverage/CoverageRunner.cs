using System.Diagnostics;

namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Wraps command execution with dotnet-coverage collect using server mode
/// for efficient multi-file coverage collection.
/// </summary>
public class CoverageRunner : IDisposable
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ICoverageFormatterFactory _formatterFactory;
    private readonly string _outputDirectory;
    private readonly string _format;
    private readonly string _sessionId;
    private readonly string _coverageFile;
    private IProcessHandle? _serverProcess;
    private bool _serverStarted;
    private bool _disposed;

    public CoverageRunner(
        string outputDirectory,
        string format = "cobertura",
        IProcessRunner? processRunner = null,
        IFileSystem? fileSystem = null,
        ICoverageFormatterFactory? formatterFactory = null)
    {
        _outputDirectory = outputDirectory;
        _format = format.ToLowerInvariant();
        _sessionId = $"draftspec-{Guid.NewGuid():N}";
        _coverageFile = Path.Combine(_outputDirectory, $"coverage.{GetFileExtension()}");

        // Use defaults for backward compatibility
        _processRunner = processRunner ?? new SystemProcessRunner();
        _fileSystem = fileSystem ?? new FileSystem();
        _formatterFactory = formatterFactory ?? new CoverageFormatterFactory();
    }

    /// <summary>
    /// Output directory for coverage files.
    /// </summary>
    public string OutputDirectory => _outputDirectory;

    /// <summary>
    /// Coverage output format.
    /// </summary>
    public string Format => _format;

    /// <summary>
    /// Session ID for the coverage server.
    /// </summary>
    public string SessionId => _sessionId;

    /// <summary>
    /// Whether the coverage server is running.
    /// </summary>
    public bool IsServerRunning => _serverStarted && _serverProcess is { HasExited: false };

    /// <summary>
    /// Path to the coverage output file.
    /// </summary>
    public string CoverageFile => _coverageFile;

    /// <summary>
    /// Start the coverage collection server.
    /// Must be called before RunWithCoverage.
    /// </summary>
    public bool StartServer()
    {
        if (_serverStarted)
            return IsServerRunning;

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
        startInfo.ArgumentList.Add(_sessionId);
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(_coverageFile);
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

    /// <summary>
    /// Run a dotnet command with coverage collection.
    /// Uses 'connect' to attach to running server for efficiency.
    /// Falls back to standalone 'collect' if server not running.
    /// </summary>
    public ProcessResult RunWithCoverage(
        IEnumerable<string> dotnetArguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        var commandToWrap = BuildDotnetCommand(dotnetArguments);

        // If server is running, use connect (fast)
        if (IsServerRunning)
        {
            var connectArgs = new List<string>
            {
                "connect",
                _sessionId,
                commandToWrap
            };

            return _processRunner.Run(
                "dotnet-coverage",
                connectArgs,
                workingDirectory,
                environmentVariables);
        }

        // Fallback: standalone collect (slow, but works without server)
        EnsureOutputDirectory();

        var collectArgs = new List<string>
        {
            "collect",
            "-o", _coverageFile,
            "-f", _format,
            commandToWrap
        };

        return _processRunner.Run(
            "dotnet-coverage",
            collectArgs,
            workingDirectory,
            environmentVariables);
    }

    /// <summary>
    /// Shutdown the coverage server and finalize the coverage file.
    /// </summary>
    public bool Shutdown()
    {
        if (!_serverStarted)
            return false;

        // Send shutdown command
        var result = _processRunner.Run("dotnet-coverage", ["shutdown", _sessionId]);

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
        return result.Success && _fileSystem.FileExists(_coverageFile);
    }

    /// <summary>
    /// Get the coverage file path after collection is complete.
    /// Returns null if no coverage was collected.
    /// </summary>
    public string? GetCoverageFile()
    {
        return _fileSystem.FileExists(_coverageFile) ? _coverageFile : null;
    }

    /// <summary>
    /// Generate formatted reports from the coverage file.
    /// </summary>
    /// <param name="formats">Formats to generate (e.g., "html", "json").</param>
    /// <returns>Dictionary of format name to output file path.</returns>
    public Dictionary<string, string> GenerateReports(IEnumerable<string> formats)
    {
        var result = new Dictionary<string, string>();

        if (!_fileSystem.FileExists(_coverageFile))
            return result;

        var report = CoberturaParser.TryParseFile(_coverageFile);
        if (report == null)
            return result;

        foreach (var format in formats)
        {
            var formatter = _formatterFactory.GetFormatter(format);
            if (formatter == null)
                continue;

            var outputPath = Path.Combine(_outputDirectory, $"coverage{formatter.FileExtension}");
            var content = formatter.Format(report);
            _fileSystem.WriteAllText(outputPath, content);
            result[format] = outputPath;
        }

        return result;
    }

    /// <summary>
    /// Generate formatted reports from a specific coverage file path.
    /// </summary>
    public Dictionary<string, string> GenerateReports(string coverageFilePath, IEnumerable<string> formats)
    {
        var result = new Dictionary<string, string>();
        var report = CoberturaParser.TryParseFile(coverageFilePath);

        if (report == null)
            return result;

        foreach (var format in formats)
        {
            var formatter = _formatterFactory.GetFormatter(format);
            if (formatter == null)
                continue;

            var outputPath = Path.Combine(_outputDirectory, $"coverage{formatter.FileExtension}");
            var content = formatter.Format(report);
            _fileSystem.WriteAllText(outputPath, content);
            result[format] = outputPath;
        }

        return result;
    }

    private void EnsureOutputDirectory()
    {
        if (!_fileSystem.DirectoryExists(_outputDirectory))
            _fileSystem.CreateDirectory(_outputDirectory);
    }

    /// <summary>
    /// Get file extension for the configured format.
    /// </summary>
    internal string GetFileExtension() => _format switch
    {
        "cobertura" => "cobertura.xml",
        "xml" => "xml",
        "coverage" => "coverage",
        _ => "cobertura.xml"
    };

    /// <summary>
    /// Build a dotnet command string from arguments.
    /// </summary>
    internal static string BuildDotnetCommand(IEnumerable<string> arguments)
    {
        var args = arguments.Select(QuoteIfNeeded);
        return $"dotnet {string.Join(" ", args)}";
    }

    /// <summary>
    /// Quote an argument if it contains special characters.
    /// </summary>
    internal static string QuoteIfNeeded(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return "\"\"";

        if (arg.Contains(' ') || arg.Contains('"') || arg.Contains('\\'))
        {
            // Escape backslashes and quotes
            var escaped = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }

        return arg;
    }

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
