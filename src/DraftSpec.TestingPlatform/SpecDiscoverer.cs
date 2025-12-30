using DraftSpec.Scripting;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Discovers specs from CSX files in a project directory.
/// </summary>
/// <remarks>
/// The discoverer finds all *.spec.csx files, executes them to build the spec tree,
/// then flattens the tree into a list of DiscoveredSpec instances with stable IDs.
/// If a file fails to compile, falls back to static parsing to still discover spec structure.
/// </remarks>
public sealed class SpecDiscoverer : ISpecDiscoverer
{
    private readonly string _projectDirectory;
    private readonly IScriptHost _scriptHost;
    private readonly ISpecFileProvider _fileProvider;
    private readonly ISpecStateManager _stateManager;
    private readonly StaticSpecParser _staticParser;

    /// <summary>
    /// Creates a new spec discoverer.
    /// </summary>
    /// <param name="projectDirectory">The project root directory for finding CSX files and computing relative paths.</param>
    /// <param name="scriptHost">Optional script host for testing. Defaults to CsxScriptHost.</param>
    /// <param name="fileProvider">Optional file provider for testing. Defaults to FileSystemSpecFileProvider.</param>
    /// <param name="stateManager">Optional state manager for testing. Defaults to DefaultSpecStateManager.</param>
    public SpecDiscoverer(
        string projectDirectory,
        IScriptHost? scriptHost = null,
        ISpecFileProvider? fileProvider = null,
        ISpecStateManager? stateManager = null)
    {
        _projectDirectory = Path.GetFullPath(projectDirectory);
        _scriptHost = scriptHost ?? new CsxScriptHost(_projectDirectory);
        _fileProvider = fileProvider ?? new FileSystemSpecFileProvider();
        _stateManager = stateManager ?? new DefaultSpecStateManager();
        _staticParser = new StaticSpecParser(_projectDirectory);
    }

    /// <summary>
    /// Discovers all specs from CSX files in the project directory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Discovery result containing specs and any errors.</returns>
    public async Task<SpecDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var csxFiles = FindSpecFiles();
        var allSpecs = new List<DiscoveredSpec>();
        var errors = new List<DiscoveryError>();

        foreach (var csxFile in csxFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Reset state before each file to ensure isolation
            _stateManager.ResetState();

            try
            {
                var rootContext = await _scriptHost.ExecuteAsync(csxFile, cancellationToken);

                if (rootContext != null)
                {
                    var relativePath = GetRelativePath(csxFile);
                    var specs = FlattenContext(rootContext, csxFile, relativePath);
                    allSpecs.AddRange(specs);
                }
            }
            catch (Exception ex)
            {
                // Execution failed - try static parsing to discover spec structure
                var relativePath = GetRelativePath(csxFile);
                var staticResult = await _staticParser.ParseFileAsync(csxFile, cancellationToken);

                if (staticResult.Specs.Count > 0)
                {
                    // Convert statically-discovered specs with compilation error marker
                    var errorSpecs = ConvertStaticSpecs(
                        staticResult.Specs,
                        csxFile,
                        relativePath,
                        ex.Message);
                    allSpecs.AddRange(errorSpecs);
                }
                else
                {
                    // Couldn't discover any specs - report as error node
                    errors.Add(new DiscoveryError
                    {
                        SourceFile = csxFile,
                        RelativeSourceFile = relativePath,
                        Message = ex.Message,
                        Exception = ex
                    });
                }
            }
            finally
            {
                // Always reset after processing
                _stateManager.ResetState();
            }
        }

        return new SpecDiscoveryResult
        {
            Specs = allSpecs,
            Errors = errors
        };
    }

    /// <summary>
    /// Discovers specs from a single CSX file.
    /// </summary>
    /// <param name="csxFilePath">Path to the CSX file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of discovered specs from the file.</returns>
    public async Task<IReadOnlyList<DiscoveredSpec>> DiscoverFileAsync(
        string csxFilePath,
        CancellationToken cancellationToken = default)
    {
        var absolutePath = _fileProvider.GetAbsolutePath(_projectDirectory, csxFilePath);

        // Reset state before execution
        _stateManager.ResetState();

        try
        {
            var rootContext = await _scriptHost.ExecuteAsync(absolutePath, cancellationToken);

            if (rootContext == null)
            {
                return [];
            }

            var relativePath = GetRelativePath(absolutePath);
            return FlattenContext(rootContext, absolutePath, relativePath);
        }
        finally
        {
            _stateManager.ResetState();
        }
    }

    /// <summary>
    /// Finds all *.spec.csx files in the project directory.
    /// </summary>
    private IEnumerable<string> FindSpecFiles()
    {
        return _fileProvider.GetSpecFiles(_projectDirectory);
    }

    /// <summary>
    /// Gets the relative path from the project directory.
    /// </summary>
    private string GetRelativePath(string absolutePath)
    {
        return _fileProvider.GetRelativePath(_projectDirectory, absolutePath);
    }

    /// <summary>
    /// Flattens a SpecContext tree into a list of DiscoveredSpec instances.
    /// </summary>
    private static List<DiscoveredSpec> FlattenContext(
        SpecContext context,
        string sourceFile,
        string relativeSourceFile)
    {
        var specs = new List<DiscoveredSpec>();
        FlattenContextRecursive(context, sourceFile, relativeSourceFile, [], specs);
        return specs;
    }

    /// <summary>
    /// Recursively flattens the context tree.
    /// </summary>
    private static void FlattenContextRecursive(
        SpecContext context,
        string sourceFile,
        string relativeSourceFile,
        List<string> contextPath,
        List<DiscoveredSpec> results)
    {
        // Add current context to path
        var currentPath = new List<string>(contextPath) { context.Description };

        // Process specs in this context
        foreach (var spec in context.Specs)
        {
            var specId = TestNodeMapper.GenerateStableId(relativeSourceFile, currentPath, spec.Description);
            var displayName = TestNodeMapper.GenerateDisplayName(currentPath, spec.Description);

            results.Add(new DiscoveredSpec
            {
                Id = specId,
                Description = spec.Description,
                DisplayName = displayName,
                ContextPath = currentPath.ToArray(),
                SourceFile = sourceFile,
                RelativeSourceFile = relativeSourceFile,
                IsPending = spec.IsPending,
                IsSkipped = spec.IsSkipped,
                IsFocused = spec.IsFocused,
                Tags = spec.Tags,
                LineNumber = spec.LineNumber,
                SpecDefinition = spec,
                Context = context
            });
        }

        // Process child contexts recursively
        foreach (var child in context.Children)
        {
            FlattenContextRecursive(child, sourceFile, relativeSourceFile, currentPath, results);
        }
    }

    /// <summary>
    /// Converts statically-discovered specs to DiscoveredSpec instances with compilation error markers.
    /// </summary>
    private static List<DiscoveredSpec> ConvertStaticSpecs(
        IReadOnlyList<StaticSpec> staticSpecs,
        string sourceFile,
        string relativeSourceFile,
        string compilationError)
    {
        var results = new List<DiscoveredSpec>();

        foreach (var staticSpec in staticSpecs)
        {
            var specId = TestNodeMapper.GenerateStableId(
                relativeSourceFile,
                staticSpec.ContextPath,
                staticSpec.Description);
            var displayName = TestNodeMapper.GenerateDisplayName(
                staticSpec.ContextPath,
                staticSpec.Description);

            results.Add(new DiscoveredSpec
            {
                Id = specId,
                Description = staticSpec.Description,
                DisplayName = displayName,
                ContextPath = staticSpec.ContextPath,
                SourceFile = sourceFile,
                RelativeSourceFile = relativeSourceFile,
                IsPending = staticSpec.IsPending,
                IsSkipped = staticSpec.Type == StaticSpecType.Skipped,
                IsFocused = staticSpec.Type == StaticSpecType.Focused,
                Tags = [],
                LineNumber = staticSpec.LineNumber,
                CompilationError = compilationError,
                // No SpecDefinition or Context - can't execute
                SpecDefinition = null,
                Context = null
            });
        }

        return results;
    }
}
