using DraftSpec.Formatters;

namespace DraftSpec.Plugins.Reporters;

/// <summary>
/// Reporter that writes the spec report to a file using a formatter.
/// </summary>
public class FileReporter : IReporter
{
    private readonly string _filePath;
    private readonly IFormatter _formatter;

    /// <summary>
    /// Create a FileReporter that writes JSON to the specified file.
    /// </summary>
    /// <param name="filePath">Path to the output file</param>
    public FileReporter(string filePath)
        : this(filePath, new JsonFormatter())
    {
    }

    /// <summary>
    /// Create a FileReporter with a custom formatter.
    /// </summary>
    /// <param name="filePath">Path to the output file</param>
    /// <param name="formatter">The formatter to use</param>
    public FileReporter(string filePath, IFormatter formatter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(formatter);
        _filePath = filePath;
        _formatter = formatter;
    }

    public string Name => $"file:{Path.GetFileName(_filePath)}";

    public async Task OnRunCompletedAsync(SpecReport report)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var content = _formatter.Format(report);
        await File.WriteAllTextAsync(_filePath, content);
    }
}
