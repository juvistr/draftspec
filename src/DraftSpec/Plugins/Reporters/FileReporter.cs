using System.Security;
using DraftSpec.Formatters;

namespace DraftSpec.Plugins.Reporters;

/// <summary>
/// Reporter that writes the spec report to a file using a formatter.
/// Validates that output paths are within an allowed directory to prevent path traversal attacks.
/// </summary>
public class FileReporter : IReporter
{
    private readonly string _filePath;
    private readonly string _allowedDirectory;
    private readonly IFormatter _formatter;

    /// <summary>
    /// Create a FileReporter that writes JSON to the specified file.
    /// Output path must be within the current working directory.
    /// </summary>
    /// <param name="filePath">Path to the output file</param>
    public FileReporter(string filePath)
        : this(filePath, new JsonFormatter(), null)
    {
    }

    /// <summary>
    /// Create a FileReporter with a custom formatter.
    /// </summary>
    /// <param name="filePath">Path to the output file</param>
    /// <param name="formatter">The formatter to use</param>
    /// <param name="allowedDirectory">Base directory for path validation. Defaults to current working directory.</param>
    /// <exception cref="SecurityException">Thrown when output path is outside the allowed directory</exception>
    public FileReporter(string filePath, IFormatter formatter, string? allowedDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(formatter);

        _allowedDirectory = Path.GetFullPath(allowedDirectory ?? Directory.GetCurrentDirectory());
        _filePath = Path.GetFullPath(filePath);
        _formatter = formatter;

        ValidatePathWithinAllowedDirectory();
    }

    /// <summary>
    /// Gets the reporter name identifier, including the output filename.
    /// </summary>
    public string Name => $"file:{Path.GetFileName(_filePath)}";

    /// <summary>
    /// Write the spec report to the configured file when the run completes.
    /// Creates the output directory if it doesn't exist.
    /// </summary>
    public async Task OnRunCompletedAsync(SpecReport report)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

        var content = _formatter.Format(report);
        await File.WriteAllTextAsync(_filePath, content).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates that the output file path is within the allowed directory.
    /// Uses trailing separator comparison to prevent prefix bypass attacks.
    /// </summary>
    private void ValidatePathWithinAllowedDirectory()
    {
        // Add trailing separator to prevent prefix bypass attacks
        // e.g., "/var/app/reports-evil" should NOT pass check for "/var/app/reports"
        var normalizedBase = _allowedDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        // Get the directory containing the file, or use file path directly for root-level files
        var fileDirectory = Path.GetDirectoryName(_filePath);
        var normalizedPath = string.IsNullOrEmpty(fileDirectory)
            ? _filePath + Path.DirectorySeparatorChar
            : fileDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        // Use platform-appropriate case sensitivity
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!normalizedPath.StartsWith(normalizedBase, comparison))
            // Generic error message to avoid leaking internal paths
            throw new SecurityException("Output path must be within the allowed directory");
    }
}
