namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Detects if dotnet-coverage tool is available.
/// </summary>
public static class CoverageToolDetector
{
    private static bool? _isAvailable;
    private static string? _version;

    /// <summary>
    /// Check if dotnet-coverage is installed and accessible.
    /// </summary>
    public static bool IsAvailable
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
    public static string? Version
    {
        get
        {
            if (!_isAvailable.HasValue)
                CheckAvailability();

            return _version;
        }
    }

    private static void CheckAvailability()
    {
        try
        {
            var result = ProcessHelper.Run("dotnet-coverage", ["--version"]);
            _isAvailable = result.Success;
            _version = result.Success ? result.Output.Trim() : null;
        }
        catch
        {
            _isAvailable = false;
            _version = null;
        }
    }

    /// <summary>
    /// Reset the cached detection state. Used for testing.
    /// </summary>
    internal static void Reset()
    {
        _isAvailable = null;
        _version = null;
    }
}
