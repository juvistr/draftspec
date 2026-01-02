namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Detects if dotnet-coverage tool is available.
/// </summary>
public class CoverageToolDetector
{
    private readonly IProcessRunner _processRunner;
    private bool? _isAvailable;
    private string? _version;

    /// <summary>
    /// Creates a new CoverageToolDetector with an optional process runner.
    /// </summary>
    /// <param name="processRunner">Process runner for executing commands. Defaults to SystemProcessRunner.</param>
    public CoverageToolDetector(IProcessRunner? processRunner = null)
    {
        _processRunner = processRunner ?? new SystemProcessRunner();
    }

    /// <summary>
    /// Check if dotnet-coverage is installed and accessible.
    /// </summary>
    public bool IsAvailable
    {
        get
        {
            if (_isAvailable.HasValue)
                return _isAvailable.Value;

            CheckAvailability();
            return _isAvailable!.Value;
        }
    }

    /// <summary>
    /// Get the installed dotnet-coverage version, or null if not installed.
    /// </summary>
    public string? Version
    {
        get
        {
            if (!_isAvailable.HasValue)
                CheckAvailability();

            return _version;
        }
    }

    private void CheckAvailability()
    {
        try
        {
            var result = _processRunner.Run("dotnet-coverage", ["--version"]);
            _isAvailable = result.Success;
            _version = result.Success ? result.Output.Trim() : null;
        }
        catch
        {
            _isAvailable = false;
            _version = null;
        }
    }
}
