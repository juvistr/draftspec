using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace DraftSpec.Mcp.Resources;

/// <summary>
/// MCP resources for accessing spec files.
/// Enables IDE integrations and spec browsing without separate file system access.
/// </summary>
[McpServerResourceType]
public static class SpecResources
{
    /// <summary>
    /// List all spec files in the working directory.
    /// </summary>
    [McpServerResource(UriTemplate = "draftspec://specs", Name = "spec_list")]
    [Description("List all DraftSpec spec files (*.spec.csx) in the working directory")]
    public static string ListSpecs(
        [Description("Directory to search for specs (default: current directory)")]
        string? directory = null,
        [Description("Glob pattern to filter specs (default: **/*.spec.csx)")]
        string? pattern = null)
    {
        var searchDir = string.IsNullOrEmpty(directory)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(directory);

        if (!Directory.Exists(searchDir))
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Directory not found: {searchDir}",
                specs = Array.Empty<object>()
            });
        }

        var searchPattern = pattern ?? "*.spec.csx";
        var specs = new List<SpecFileInfo>();

        try
        {
            var files = Directory.GetFiles(searchDir, searchPattern, SearchOption.AllDirectories)
                .OrderBy(f => f)
                .ToList();

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                specs.Add(new SpecFileInfo
                {
                    Path = Path.GetRelativePath(searchDir, file),
                    FullPath = file,
                    Name = Path.GetFileNameWithoutExtension(file).Replace(".spec", ""),
                    Size = info.Length,
                    ModifiedAt = info.LastWriteTimeUtc
                });
            }
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = ex.Message,
                specs = Array.Empty<object>()
            });
        }

        return JsonSerializer.Serialize(new
        {
            directory = searchDir,
            count = specs.Count,
            specs
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    /// <summary>
    /// Get the content of a specific spec file.
    /// </summary>
    [McpServerResource(UriTemplate = "draftspec://specs/{path}", Name = "spec_content")]
    [Description("Get the content of a specific DraftSpec spec file")]
    public static string GetSpec(
        [Description("Path to the spec file (relative or absolute)")]
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return JsonSerializer.Serialize(new
            {
                error = "Path is required"
            });
        }

        var fullPath = Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            return JsonSerializer.Serialize(new
            {
                error = $"File not found: {path}"
            });
        }

        // Security: Ensure the file has .csx extension
        if (!fullPath.EndsWith(".csx", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new
            {
                error = "Only .csx files can be accessed"
            });
        }

        try
        {
            var info = new FileInfo(fullPath);
            var content = File.ReadAllText(fullPath);

            return JsonSerializer.Serialize(new
            {
                path = path,
                fullPath,
                name = Path.GetFileNameWithoutExtension(fullPath).Replace(".spec", ""),
                size = info.Length,
                modifiedAt = info.LastWriteTimeUtc,
                content
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get metadata about a spec file without its content.
    /// </summary>
    [McpServerResource(UriTemplate = "draftspec://specs/{path}/metadata", Name = "spec_metadata")]
    [Description("Get metadata about a spec file (size, modified date) without content")]
    public static string GetSpecMetadata(
        [Description("Path to the spec file")]
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return JsonSerializer.Serialize(new
            {
                error = "Path is required"
            });
        }

        var fullPath = Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            return JsonSerializer.Serialize(new
            {
                error = $"File not found: {path}"
            });
        }

        try
        {
            var info = new FileInfo(fullPath);

            // Count describe/it blocks for a quick overview
            var content = File.ReadAllText(fullPath);
            var describeCount = System.Text.RegularExpressions.Regex.Matches(content, @"\bdescribe\s*\(").Count;
            var itCount = System.Text.RegularExpressions.Regex.Matches(content, @"\bit\s*\(").Count;

            return JsonSerializer.Serialize(new
            {
                path = path,
                fullPath,
                name = Path.GetFileNameWithoutExtension(fullPath).Replace(".spec", ""),
                size = info.Length,
                modifiedAt = info.LastWriteTimeUtc,
                createdAt = info.CreationTimeUtc,
                stats = new
                {
                    describeBlocks = describeCount,
                    specs = itCount
                }
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = ex.Message
            });
        }
    }
}

/// <summary>
/// Information about a spec file.
/// </summary>
internal class SpecFileInfo
{
    public string Path { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string Name { get; set; } = "";
    public long Size { get; set; }
    public DateTime ModifiedAt { get; set; }
}
