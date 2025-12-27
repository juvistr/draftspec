namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Wraps command execution with dotnet-coverage collect.
/// </summary>
public class CoverageRunner
{
    private readonly string _outputDirectory;
    private readonly string _format;
    private readonly List<string> _coverageFiles = [];
    private int _fileCounter;

    public CoverageRunner(string outputDirectory, string format = "cobertura")
    {
        _outputDirectory = outputDirectory;
        _format = format.ToLowerInvariant();
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
    /// Coverage files generated during this run.
    /// </summary>
    public IReadOnlyList<string> CoverageFiles => _coverageFiles;

    /// <summary>
    /// Run a dotnet command with coverage collection.
    /// </summary>
    public ProcessResult RunWithCoverage(
        IEnumerable<string> dotnetArguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        EnsureOutputDirectory();

        var coverageFile = GenerateCoverageFileName();
        _coverageFiles.Add(coverageFile);

        // Build: dotnet-coverage collect -o {file} -f {format} "dotnet {args}"
        var commandToWrap = BuildDotnetCommand(dotnetArguments);

        var args = new List<string>
        {
            "collect",
            "-o", coverageFile,
            "-f", _format,
            commandToWrap
        };

        return ProcessHelper.Run(
            "dotnet-coverage",
            args,
            workingDirectory,
            environmentVariables);
    }

    /// <summary>
    /// Merge all collected coverage files into a single report.
    /// Returns the path to the merged file, or null if no files to merge.
    /// </summary>
    public string? MergeCoverageFiles()
    {
        if (_coverageFiles.Count == 0)
            return null;

        var mergedFile = Path.Combine(_outputDirectory, $"coverage.{GetFileExtension()}");

        if (_coverageFiles.Count == 1)
        {
            // Just copy the single file to the final location
            if (_coverageFiles[0] != mergedFile)
            {
                File.Copy(_coverageFiles[0], mergedFile, overwrite: true);
            }
            return mergedFile;
        }

        // Merge multiple files
        var args = new List<string> { "merge", "-o", mergedFile, "-f", _format };
        args.AddRange(_coverageFiles);

        var result = ProcessHelper.Run("dotnet-coverage", args);

        return result.Success ? mergedFile : null;
    }

    /// <summary>
    /// Clean up intermediate coverage files after merging.
    /// </summary>
    public void CleanupIntermediateFiles()
    {
        var mergedFile = Path.Combine(_outputDirectory, $"coverage.{GetFileExtension()}");

        foreach (var file in _coverageFiles)
        {
            if (file != mergedFile && File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup failures
                }
            }
        }
    }

    /// <summary>
    /// Generate formatted reports from a coverage file.
    /// </summary>
    /// <param name="coverageFilePath">Path to the Cobertura XML coverage file.</param>
    /// <param name="formats">Formats to generate (e.g., "html", "json").</param>
    /// <returns>Dictionary of format name to output file path.</returns>
    public Dictionary<string, string> GenerateReports(string coverageFilePath, IEnumerable<string> formats)
    {
        var result = new Dictionary<string, string>();
        var report = CoberturaParser.TryParseFile(coverageFilePath);

        if (report == null)
            return result;

        foreach (var format in formats)
        {
            var formatter = GetFormatter(format);
            if (formatter == null)
                continue;

            var outputPath = Path.Combine(_outputDirectory, $"coverage{formatter.FileExtension}");
            var content = formatter.Format(report);
            File.WriteAllText(outputPath, content);
            result[format] = outputPath;
        }

        return result;
    }

    /// <summary>
    /// Get a coverage formatter by name.
    /// </summary>
    private static ICoverageFormatter? GetFormatter(string format) => format.ToLowerInvariant() switch
    {
        "html" => new CoverageHtmlFormatter(),
        "json" => new CoverageJsonFormatter(),
        _ => null
    };

    private void EnsureOutputDirectory()
    {
        if (!Directory.Exists(_outputDirectory))
            Directory.CreateDirectory(_outputDirectory);
    }

    private string GenerateCoverageFileName()
    {
        var index = Interlocked.Increment(ref _fileCounter);
        return Path.Combine(_outputDirectory, $"coverage-{index:D4}.{GetFileExtension()}");
    }

    private string GetFileExtension() => _format switch
    {
        "cobertura" => "cobertura.xml",
        "xml" => "xml",
        "coverage" => "coverage",
        _ => "cobertura.xml"
    };

    private static string BuildDotnetCommand(IEnumerable<string> arguments)
    {
        var args = arguments.Select(QuoteIfNeeded);
        return $"dotnet {string.Join(" ", args)}";
    }

    private static string QuoteIfNeeded(string arg)
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
}
