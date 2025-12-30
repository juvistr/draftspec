using DraftSpec.Cli.Options;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Validates spec structure without execution.
/// Uses static parsing to detect issues in spec files.
/// </summary>
public class ValidateCommand : ICommand<ValidateOptions>
{
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Exit code when validation passes (no errors, warnings OK).
    /// </summary>
    public const int ExitSuccess = 0;

    /// <summary>
    /// Exit code when validation finds errors.
    /// </summary>
    public const int ExitErrors = 1;

    /// <summary>
    /// Exit code when validation finds warnings with --strict mode.
    /// </summary>
    public const int ExitWarnings = 2;

    public ValidateCommand(IConsole console, IFileSystem fileSystem)
    {
        _console = console;
        _fileSystem = fileSystem;
    }

    public async Task<int> ExecuteAsync(ValidateOptions options, CancellationToken ct = default)
    {
        // 1. Resolve path
        var projectPath = Path.GetFullPath(options.Path);

        if (!_fileSystem.DirectoryExists(projectPath) && !_fileSystem.FileExists(projectPath))
            throw new ArgumentException($"Path not found: {projectPath}");

        // 2. Find spec files (use --files if provided, otherwise scan directory)
        var specFiles = GetSpecFiles(projectPath, options.Files);

        if (specFiles.Count == 0)
        {
            if (!options.Quiet)
                _console.WriteLine("No spec files found.");
            return ExitSuccess;
        }

        // 3. Validate each file using static parser
        if (!options.Quiet)
            _console.WriteLine("Validating spec structure...\n");

        var parser = new StaticSpecParser(projectPath);
        var results = new List<FileValidationResult>();
        var totalSpecs = 0;
        var totalErrors = 0;
        var totalWarnings = 0;

        foreach (var specFile in specFiles)
        {
            var relativePath = Path.GetRelativePath(projectPath, specFile);
            var fileResult = new FileValidationResult { FilePath = relativePath };

            try
            {
                var parseResult = await parser.ParseFileAsync(specFile, ct);
                fileResult.SpecCount = parseResult.Specs.Count;
                totalSpecs += parseResult.Specs.Count;

                // Categorize issues from warnings
                foreach (var warning in parseResult.Warnings)
                {
                    var issue = ParseIssue(warning);

                    if (IsError(issue))
                    {
                        fileResult.Errors.Add(issue);
                        totalErrors++;
                    }
                    else
                    {
                        fileResult.Warnings.Add(issue);
                        totalWarnings++;
                    }
                }
            }
            catch (Exception ex)
            {
                fileResult.Errors.Add(new ValidationIssue
                {
                    Message = $"Parse error: {ex.Message}",
                    LineNumber = null
                });
                totalErrors++;
            }

            results.Add(fileResult);
        }

        // 4. Output results
        OutputResults(results, options.Quiet);

        // 5. Output summary
        if (!options.Quiet)
        {
            _console.WriteLine("");
            _console.WriteLine(new string('\u2501', 40)); // ━
            _console.WriteLine($"Files: {results.Count} | Specs: {totalSpecs} | Errors: {totalErrors} | Warnings: {totalWarnings}");
        }

        // 6. Determine exit code
        if (totalErrors > 0)
            return ExitErrors;

        if (totalWarnings > 0 && options.Strict)
            return ExitWarnings;

        return ExitSuccess;
    }

    private List<string> GetSpecFiles(string projectPath, List<string>? explicitFiles)
    {
        // If explicit files provided via --files, use those
        if (explicitFiles is { Count: > 0 })
        {
            return explicitFiles
                .Select(f => Path.IsPathRooted(f) ? f : Path.Combine(projectPath, f))
                .Where(f => _fileSystem.FileExists(f))
                .ToList();
        }

        // Otherwise, scan directory
        if (_fileSystem.FileExists(projectPath) && projectPath.EndsWith(".spec.csx", StringComparison.OrdinalIgnoreCase))
        {
            return [projectPath];
        }

        if (_fileSystem.DirectoryExists(projectPath))
        {
            return _fileSystem.EnumerateFiles(projectPath, "*.spec.csx", SearchOption.AllDirectories).ToList();
        }

        return [];
    }

    private static ValidationIssue ParseIssue(string warning)
    {
        // Parse warnings like "Line 15: 'describe' has dynamic description - cannot analyze statically"
        var issue = new ValidationIssue { Message = warning };

        if (warning.StartsWith("Line ", StringComparison.OrdinalIgnoreCase))
        {
            var colonIndex = warning.IndexOf(':');
            if (colonIndex > 5)
            {
                var lineStr = warning[5..colonIndex];
                if (int.TryParse(lineStr, out var lineNumber))
                {
                    issue.LineNumber = lineNumber;
                    issue.Message = warning[(colonIndex + 1)..].Trim();
                }
            }
        }

        return issue;
    }

    private static bool IsError(ValidationIssue issue)
    {
        var msg = issue.Message.ToLowerInvariant();

        // These are errors (always fail)
        if (msg.Contains("missing description argument"))
            return true;
        if (msg.Contains("empty description"))
            return true;
        if (msg.Contains("parse error"))
            return true;
        if (msg.Contains("syntax error"))
            return true;

        // Everything else is a warning
        return false;
    }

    private void OutputResults(List<FileValidationResult> results, bool quiet)
    {
        foreach (var result in results)
        {
            if (result.Errors.Count > 0)
            {
                // File with errors
                _console.WriteError($"\u2717 {result.FilePath}");
                foreach (var error in result.Errors)
                {
                    var location = error.LineNumber.HasValue ? $"Line {error.LineNumber}: " : "";
                    _console.WriteError($"  {location}{error.Message}");
                }
            }
            else if (result.Warnings.Count > 0)
            {
                // File with warnings only
                if (!quiet)
                {
                    _console.WriteLine($"\u26a0 {result.FilePath} - {result.SpecCount} specs");
                    foreach (var warning in result.Warnings)
                    {
                        var location = warning.LineNumber.HasValue ? $"Line {warning.LineNumber}: " : "";
                        _console.WriteLine($"  {location}{warning.Message}");
                    }
                }
            }
            else
            {
                // Valid file
                if (!quiet)
                {
                    _console.WriteSuccess($"\u2713 {result.FilePath} - {result.SpecCount} specs");
                }
            }
        }
    }

    private class FileValidationResult
    {
        public string FilePath { get; set; } = "";
        public int SpecCount { get; set; }
        public List<ValidationIssue> Errors { get; } = [];
        public List<ValidationIssue> Warnings { get; } = [];
    }

    private class ValidationIssue
    {
        public int? LineNumber { get; set; }
        public string Message { get; set; } = "";
    }
}
