using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Globals object passed to Roslyn scripts for state sharing.
/// </summary>
public class ScriptGlobals
{
    /// <summary>
    /// Action to capture the root context after spec definitions.
    /// Called at the end of the script to transfer state back to the host.
    /// </summary>
    public Action<SpecContext?>? CaptureRootContext { get; set; }
}

/// <summary>
/// Roslyn-based script host for compiling and executing CSX spec files in-process.
/// </summary>
/// <remarks>
/// For MTP integration, spec files should NOT call run() - just define describe/it blocks.
/// The MTP adapter controls discovery and execution.
/// </remarks>
internal sealed partial class CsxScriptHost
{
    private readonly string _baseDirectory;
    private readonly IReadOnlyList<Assembly> _referenceAssemblies;
    private readonly ConcurrentDictionary<string, Script<object>> _scriptCache = new();

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
    /// Creates a new script host.
    /// </summary>
    /// <param name="baseDirectory">Base directory for resolving relative paths (typically output directory).</param>
    /// <param name="referenceAssemblies">Additional assemblies to reference in scripts.</param>
    public CsxScriptHost(string baseDirectory, IEnumerable<Assembly>? referenceAssemblies = null)
    {
        _baseDirectory = baseDirectory;
        _referenceAssemblies = referenceAssemblies?.ToList() ?? [];
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

        try
        {
            var script = await GetOrCreateScriptAsync(absolutePath, cancellationToken);
            await script.RunAsync(globals, cancellationToken: cancellationToken);
            return capturedContext;
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Gets a cached script or creates and caches a new one.
    /// </summary>
    private async Task<Script<object>> GetOrCreateScriptAsync(string absolutePath, CancellationToken cancellationToken)
    {
        // Check cache first
        if (_scriptCache.TryGetValue(absolutePath, out var cached))
        {
            return cached;
        }

        // Parse and preprocess the script
        var (code, additionalReferences) = await PreprocessScriptAsync(absolutePath, cancellationToken);

        // Build script options
        var options = CreateScriptOptions(Path.GetDirectoryName(absolutePath)!, additionalReferences);

        // Create and cache the script - use ScriptGlobals as the globals type
        var script = CSharpScript.Create(code, options, typeof(ScriptGlobals));
        _scriptCache.TryAdd(absolutePath, script);

        return script;
    }

    /// <summary>
    /// Preprocesses the script to handle #load directives and extract #r references.
    /// Appends code to capture the RootContext at the end.
    /// </summary>
    private async Task<(string code, List<string> additionalReferences)> PreprocessScriptAsync(
        string absolutePath,
        CancellationToken cancellationToken)
    {
        var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var additionalReferences = new List<string>();
        var codeBuilder = new StringBuilder();

        await ProcessFileAsync(absolutePath, processedFiles, additionalReferences, codeBuilder, cancellationToken);

        // Append code to capture the root context at the end of script execution
        codeBuilder.AppendLine();
        codeBuilder.AppendLine("// --- Capture context for MTP ---");
        codeBuilder.AppendLine("CaptureRootContext?.Invoke(DraftSpec.Dsl.RootContext);");

        return (codeBuilder.ToString(), additionalReferences);
    }

    /// <summary>
    /// Recursively processes a CSX file and its #load dependencies.
    /// </summary>
    private async Task ProcessFileAsync(
        string filePath,
        HashSet<string> processedFiles,
        List<string> additionalReferences,
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
            await ProcessFileAsync(loadAbsolutePath, processedFiles, additionalReferences, codeBuilder, cancellationToken);
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

        // Remove directives from code (they're handled separately)
        var cleanedCode = LoadDirectiveRegex().Replace(content, "");
        cleanedCode = AssemblyReferenceRegex().Replace(cleanedCode, "");

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
                typeof(ScriptGlobals).Assembly);            // DraftSpec.TestingPlatform for globals

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

        return options;
    }
}
