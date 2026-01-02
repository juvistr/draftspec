using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Scripting;

/// <summary>
/// Roslyn-based script host for compiling and executing CSX spec files in-process.
/// </summary>
/// <remarks>
/// Spec files should NOT call run() - just define describe/it blocks.
/// The host controls discovery and execution.
/// </remarks>
public sealed partial class CsxScriptHost : IScriptHost
{
    private readonly string _baseDirectory;
    private readonly IReadOnlyList<Assembly> _referenceAssemblies;
    private readonly ScriptCompilationCache? _diskCache;
    private readonly bool _useDiskCache;

    /// <summary>
    /// Cache entry containing the compiled script, source file paths, and max modification time.
    /// </summary>
    private readonly record struct CacheEntry(
        Script<object> Script,
        IReadOnlyList<string> SourceFiles,
        DateTime MaxModifiedTimeUtc);

    private readonly ConcurrentDictionary<string, CacheEntry> _scriptCache = new();

    /// <summary>
    /// Regex to match #r directives (assembly references).
    /// Captures the path/name in group 1.
    /// </summary>
    [GeneratedRegex(@"^\s*#r\s+""([^""]+)""\s*$", RegexOptions.Multiline)]
    private static partial Regex AssemblyReferenceRegex();

    /// <summary>
    /// Regex to match #load directives (file includes).
    /// Captures the path in group 1.
    /// </summary>
    [GeneratedRegex(@"^\s*#load\s+""([^""]+)""\s*$", RegexOptions.Multiline)]
    private static partial Regex LoadDirectiveRegex();

    /// <summary>
    /// Regex to match using directives.
    /// Matches both 'using X;' and 'using static X;' forms.
    /// </summary>
    [GeneratedRegex(@"^\s*using\s+(static\s+)?[^;]+;\s*$", RegexOptions.Multiline)]
    private static partial Regex UsingDirectiveRegex();

    /// <summary>
    /// Creates a new script host.
    /// </summary>
    /// <param name="baseDirectory">Base directory for resolving relative paths (typically output directory).</param>
    /// <param name="referenceAssemblies">Additional assemblies to reference in scripts.</param>
    /// <param name="useDiskCache">Whether to use disk-based compilation cache.</param>
    /// <param name="cacheDirectory">Directory for disk cache (defaults to .draftspec in baseDirectory).</param>
    /// <param name="logger">Optional logger for cache operations.</param>
    public CsxScriptHost(
        string baseDirectory,
        IEnumerable<Assembly>? referenceAssemblies = null,
        bool useDiskCache = true,
        string? cacheDirectory = null,
        ILogger? logger = null)
    {
        _baseDirectory = baseDirectory;
        _referenceAssemblies = referenceAssemblies?.ToList() ?? [];
        _useDiskCache = useDiskCache;

        if (useDiskCache)
        {
            var cacheDir = cacheDirectory ?? FindProjectRoot(baseDirectory) ?? baseDirectory;
            _diskCache = new ScriptCompilationCache(cacheDir, logger);
        }
    }

    /// <summary>
    /// Finds the project root by looking for .draftspec or .git directory.
    /// </summary>
    private static string? FindProjectRoot(string startDirectory)
    {
        var current = startDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.Exists(Path.Combine(current, ".draftspec")) ||
                Directory.Exists(Path.Combine(current, ".git")))
            {
                return current;
            }

            var parent = Directory.GetParent(current)?.FullName;
            if (parent == current)
                break;
            current = parent;
        }
        return null;
    }

    /// <summary>
    /// Executes a CSX file and returns the spec tree.
    /// The CSX file should define describe/it blocks but NOT call run().
    /// </summary>
    /// <param name="csxFilePath">Path to the CSX spec file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The root SpecContext containing the spec tree, or null if no specs defined.</returns>
    public async Task<SpecContext?> ExecuteAsync(
        string csxFilePath,
        CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.GetFullPath(csxFilePath, _baseDirectory);

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"CSX file not found: {absolutePath}", absolutePath);
        }

        SpecContext? capturedContext = null;

        // Create globals with context capture callback
        var globals = new ScriptGlobals
        {
            CaptureRootContext = ctx => capturedContext = ctx
        };

        // 1. Check in-memory cache first (fastest path)
        if (_scriptCache.TryGetValue(absolutePath, out var cached))
        {
            var currentMaxModified = GetMaxModificationTime(cached.SourceFiles);
            if (currentMaxModified == cached.MaxModifiedTimeUtc)
            {
                await cached.Script.RunAsync(globals, cancellationToken: cancellationToken);
                return capturedContext;
            }

            // Files changed - remove stale entry
            _scriptCache.TryRemove(absolutePath, out _);
        }

        // 2. Preprocess once (needed for both disk cache check and compilation)
        var (code, additionalReferences, sourceFiles, maxModifiedTimeUtc) =
            await PreprocessScriptAsync(absolutePath, cancellationToken);

        // 3. Try disk cache with preprocessed data
        if (_useDiskCache && _diskCache != null)
        {
            var (success, _) = await _diskCache.TryExecuteCachedAsync(
                absolutePath, sourceFiles, code, globals, cancellationToken);

            if (success)
            {
                return capturedContext;
            }
        }

        // 4. Compile from already-preprocessed data (no re-preprocessing)
        var options = CreateScriptOptions(Path.GetDirectoryName(absolutePath)!, additionalReferences);
        var script = CSharpScript.Create(code, options, typeof(ScriptGlobals));

        // Cache in memory for future runs
        var entry = new CacheEntry(script, sourceFiles, maxModifiedTimeUtc);
        _scriptCache.TryAdd(absolutePath, entry);

        await script.RunAsync(globals, cancellationToken: cancellationToken);

        // Cache to disk for future cold starts
        if (_useDiskCache && _diskCache != null)
        {
            _diskCache.CacheScript(absolutePath, sourceFiles, code, script);
        }

        return capturedContext;
    }

    /// <summary>
    /// Computes the maximum modification time from a list of source files.
    /// </summary>
    private static DateTime GetMaxModificationTime(IReadOnlyList<string> sourceFiles)
    {
        var maxTime = DateTime.MinValue;
        foreach (var file in sourceFiles)
        {
            var modTime = File.GetLastWriteTimeUtc(file);
            if (modTime > maxTime)
                maxTime = modTime;
        }
        return maxTime;
    }

    /// <summary>
    /// Preprocesses the script to handle #load directives and extract #r references.
    /// Appends code to capture the RootContext at the end.
    /// Returns the combined code, additional references, source file paths, and max modification time.
    /// </summary>
    private async Task<(string code, List<string> additionalReferences, IReadOnlyList<string> sourceFiles, DateTime maxModifiedTimeUtc)> PreprocessScriptAsync(
        string absolutePath,
        CancellationToken cancellationToken)
    {
        var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var additionalReferences = new List<string>();
        var usings = new HashSet<string>(StringComparer.Ordinal);
        var codeBuilder = new StringBuilder();

        await ProcessFileAsync(absolutePath, processedFiles, additionalReferences, usings, codeBuilder, cancellationToken);

        // Convert to list and compute max modification time from all processed files
        var sourceFiles = processedFiles.ToList();
        var maxModifiedTimeUtc = sourceFiles
            .Select(f => File.GetLastWriteTimeUtc(f))
            .Max();

        // Build final code: usings first, then all other code
        var finalBuilder = new StringBuilder();

        // Add all collected using directives at the top
        foreach (var usingDirective in usings.OrderBy(u => u))
        {
            finalBuilder.AppendLine(usingDirective);
        }

        if (usings.Count > 0)
        {
            finalBuilder.AppendLine();
        }

        // Add the rest of the code
        finalBuilder.Append(codeBuilder);

        // Append code to capture the root context at the end of script execution
        finalBuilder.AppendLine();
        finalBuilder.AppendLine("// --- Capture context for runner ---");
        finalBuilder.AppendLine("CaptureRootContext?.Invoke(DraftSpec.Dsl.RootContext);");

        return (finalBuilder.ToString(), additionalReferences, sourceFiles, maxModifiedTimeUtc);
    }

    /// <summary>
    /// Recursively processes a CSX file and its #load dependencies.
    /// </summary>
    private async Task ProcessFileAsync(
        string filePath,
        HashSet<string> processedFiles,
        List<string> additionalReferences,
        HashSet<string> usings,
        StringBuilder codeBuilder,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.GetFullPath(filePath);

        // Prevent circular dependencies
        if (!processedFiles.Add(absolutePath))
        {
            return;
        }

        var content = await File.ReadAllTextAsync(absolutePath, cancellationToken);
        var fileDirectory = Path.GetDirectoryName(absolutePath)!;

        // Process #load directives first (inline the loaded files)
        var loadMatches = LoadDirectiveRegex().Matches(content);
        foreach (Match match in loadMatches)
        {
            var loadPath = match.Groups[1].Value;
            var loadAbsolutePath = Path.GetFullPath(loadPath, fileDirectory);

            // Recursively process the loaded file
            await ProcessFileAsync(loadAbsolutePath, processedFiles, additionalReferences, usings, codeBuilder, cancellationToken);
        }

        // Extract #r directives (but skip nuget references - those need special handling)
        var refMatches = AssemblyReferenceRegex().Matches(content);
        foreach (Match match in refMatches)
        {
            var reference = match.Groups[1].Value;

            // Skip NuGet references - DraftSpec is available via project reference
            if (reference.StartsWith("nuget:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Resolve relative paths
            var refPath = Path.GetFullPath(reference, fileDirectory);
            if (File.Exists(refPath))
            {
                additionalReferences.Add(refPath);
            }
        }

        // Extract using directives (to be placed at top of combined code)
        var usingMatches = UsingDirectiveRegex().Matches(content);
        foreach (Match match in usingMatches)
        {
            usings.Add(match.Value.Trim());
        }

        // Remove directives from code (they're handled separately)
        var cleanedCode = LoadDirectiveRegex().Replace(content, "");
        cleanedCode = AssemblyReferenceRegex().Replace(cleanedCode, "");
        cleanedCode = UsingDirectiveRegex().Replace(cleanedCode, "");

        // Append the cleaned code
        codeBuilder.AppendLine($"// --- {Path.GetFileName(absolutePath)} ---");
        codeBuilder.AppendLine(cleanedCode);
    }

    /// <summary>
    /// Creates script options with appropriate references and imports.
    /// </summary>
    private ScriptOptions CreateScriptOptions(string scriptDirectory, List<string> additionalReferences)
    {
        var options = ScriptOptions.Default
            .WithSourceResolver(new Microsoft.CodeAnalysis.SourceFileResolver([], scriptDirectory))
            .AddImports(
                "System",
                "System.Collections.Generic",
                "System.IO",
                "System.Linq",
                "System.Threading.Tasks",
                "DraftSpec")
            .AddReferences(
                typeof(object).Assembly,                    // System.Runtime
                typeof(Console).Assembly,                   // System.Console
                typeof(File).Assembly,                      // System.IO
                typeof(Enumerable).Assembly,                // System.Linq
                typeof(Task).Assembly,                      // System.Threading.Tasks
                typeof(Dsl).Assembly,                       // DraftSpec
                typeof(ScriptGlobals).Assembly);            // DraftSpec.Scripting for globals

        // Add reference assemblies provided at construction
        foreach (var assembly in _referenceAssemblies)
        {
            options = options.AddReferences(assembly);
        }

        // Add any assembly references from #r directives
        foreach (var refPath in additionalReferences)
        {
            options = options.AddReferences(refPath);
        }

        // Add all DLLs from the base directory (covers project references copied to output)
        foreach (var dll in Directory.EnumerateFiles(_baseDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            // Skip DLLs we've already added
            var fileName = Path.GetFileName(dll);
            if (fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("DraftSpec.dll", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("DraftSpec.Scripting.dll", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                options = options.AddReferences(dll);
            }
            catch
            {
                // Skip DLLs that can't be loaded (e.g., native DLLs)
            }
        }

        return options;
    }

    /// <summary>
    /// Resets the DSL state for isolation between executions.
    /// </summary>
    public void Reset() => Dsl.Reset();
}
