using Microsoft.Extensions.Logging;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Manages temporary file creation and cleanup for spec execution.
/// </summary>
public class TempFileManager
{
    private readonly ILogger<TempFileManager> _logger;
    private readonly string _tempDirectory;
    private readonly string? _localPackagesPath;

    private const string NuGetConfigTemplate = """
                                               <?xml version="1.0" encoding="utf-8"?>
                                               <configuration>
                                                 <packageSources>
                                                   <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                                   <add key="local" value="{0}" />
                                                 </packageSources>
                                               </configuration>
                                               """;

    public TempFileManager(ILogger<TempFileManager> logger)
    {
        _logger = logger;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "draftspec-mcp");
        Directory.CreateDirectory(_tempDirectory);

        // Check for local packages (development mode)
        var localPackages = "/tmp/draftspec-packages";
        if (Directory.Exists(localPackages) && Directory.GetFiles(localPackages, "*.nupkg").Length > 0)
        {
            _localPackagesPath = localPackages;
            EnsureNuGetConfig();
            _logger.LogInformation("Using local NuGet packages from {Path}", localPackages);
        }
    }

    private void EnsureNuGetConfig()
    {
        if (_localPackagesPath == null) return;

        var nugetConfigPath = Path.Combine(_tempDirectory, "NuGet.config");
        if (!File.Exists(nugetConfigPath))
        {
            var content = string.Format(NuGetConfigTemplate, _localPackagesPath);
            File.WriteAllText(nugetConfigPath, content);
            _logger.LogDebug("Created NuGet.config at {Path}", nugetConfigPath);
        }
    }

    public string TempDirectory => _tempDirectory;

    /// <summary>
    /// Creates a temporary spec file with the given content.
    /// Uses FileMode.CreateNew for atomic creation (prevents TOCTOU race conditions).
    /// </summary>
    public async Task<string> CreateTempSpecFileAsync(string content, CancellationToken cancellationToken)
    {
        var fileName = $"spec-{Guid.NewGuid():N}.cs";
        var filePath = Path.Combine(_tempDirectory, fileName);

        await using var fs = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        await using var writer = new StreamWriter(fs);
        await writer.WriteAsync(content);

        _logger.LogDebug("Created temp spec file: {Path}", filePath);
        return filePath;
    }

    /// <summary>
    /// Creates a path for the JSON output file (not the file itself).
    /// </summary>
    public string CreateTempJsonOutputPath()
    {
        var fileName = $"result-{Guid.NewGuid():N}.json";
        return Path.Combine(_tempDirectory, fileName);
    }

    /// <summary>
    /// Cleans up temporary files. Best-effort, won't throw on failure.
    /// </summary>
    public void Cleanup(params string?[] paths)
    {
        foreach (var path in paths)
        {
            if (string.IsNullOrEmpty(path)) continue;

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    _logger.LogDebug("Deleted temp file: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temp file: {Path}", path);
            }
        }
    }
}