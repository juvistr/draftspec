namespace DraftSpec.TestingPlatform;

/// <summary>
/// Discovers specs from CSX files in a project directory.
/// </summary>
/// <remarks>
/// The discoverer finds all *.spec.csx files, executes them to build the spec tree,
/// then flattens the tree into a list of DiscoveredSpec instances with stable IDs.
/// </remarks>
internal sealed class SpecDiscoverer
{
    private readonly string _projectDirectory;
    private readonly CsxScriptHost _scriptHost;

    /// <summary>
    /// Creates a new spec discoverer.
    /// </summary>
    /// <param name="projectDirectory">The project root directory for finding CSX files and computing relative paths.</param>
    public SpecDiscoverer(string projectDirectory)
    {
        _projectDirectory = Path.GetFullPath(projectDirectory);
        _scriptHost = new CsxScriptHost(_projectDirectory);
    }

    /// <summary>
    /// Discovers all specs from CSX files in the project directory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Discovery result containing specs and any errors.</returns>
    public async Task<DiscoveryResult> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var csxFiles = FindSpecFiles();
        var allSpecs = new List<DiscoveredSpec>();
        var errors = new List<DiscoveryError>();

        foreach (var csxFile in csxFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Reset state before each file to ensure isolation
            Dsl.Reset();

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
                // Collect error for reporting through MTP
                var relativePath = GetRelativePath(csxFile);
                errors.Add(new DiscoveryError
                {
                    SourceFile = csxFile,
                    RelativeSourceFile = relativePath,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            finally
            {
                // Always reset after processing
                Dsl.Reset();
            }
        }

        return new DiscoveryResult
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
        var absolutePath = Path.GetFullPath(csxFilePath, _projectDirectory);

        // Reset state before execution
        Dsl.Reset();

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
            Dsl.Reset();
        }
    }

    /// <summary>
    /// Finds all *.spec.csx files in the project directory.
    /// </summary>
    private IEnumerable<string> FindSpecFiles()
    {
        return Directory.EnumerateFiles(_projectDirectory, "*.spec.csx", SearchOption.AllDirectories);
    }

    /// <summary>
    /// Gets the relative path from the project directory.
    /// </summary>
    private string GetRelativePath(string absolutePath)
    {
        return Path.GetRelativePath(_projectDirectory, absolutePath);
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

}
