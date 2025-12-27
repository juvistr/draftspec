using DraftSpec.Configuration;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Executes specs for MTP integration with filtering and result capture.
/// </summary>
internal sealed class MtpSpecExecutor
{
    private readonly string _projectDirectory;
    private readonly CsxScriptHost _scriptHost;

    /// <summary>
    /// Creates a new MTP spec executor.
    /// </summary>
    /// <param name="projectDirectory">The project root directory.</param>
    public MtpSpecExecutor(string projectDirectory)
    {
        _projectDirectory = Path.GetFullPath(projectDirectory);
        _scriptHost = new CsxScriptHost(_projectDirectory);
    }

    /// <summary>
    /// Executes all specs from a CSX file and returns results.
    /// </summary>
    /// <param name="csxFilePath">Path to the CSX file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results paired with relative source file path.</returns>
    public async Task<ExecutionResult> ExecuteFileAsync(
        string csxFilePath,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteFileAsync(csxFilePath, requestedIds: null, cancellationToken);
    }

    /// <summary>
    /// Executes filtered specs from a CSX file and returns results.
    /// </summary>
    /// <param name="csxFilePath">Path to the CSX file.</param>
    /// <param name="requestedIds">Set of spec IDs to run, or null to run all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results paired with relative source file path.</returns>
    public async Task<ExecutionResult> ExecuteFileAsync(
        string csxFilePath,
        HashSet<string>? requestedIds,
        CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.GetFullPath(csxFilePath, _projectDirectory);
        var relativePath = GetRelativePath(absolutePath);

        // Reset state before execution
        Dsl.Reset();

        try
        {
            // Execute script to build spec tree
            var rootContext = await _scriptHost.ExecuteAsync(absolutePath, cancellationToken);

            if (rootContext == null)
            {
                return new ExecutionResult(relativePath, absolutePath, []);
            }

            // Create result capture reporter
            var captureReporter = new ResultCaptureReporter();

            // Build configuration with reporter
            var config = new DraftSpecConfiguration();
            config.AddReporter(captureReporter);

            // Build runner with optional filter
            var builder = new SpecRunnerBuilder()
                .WithConfiguration(config);

            if (requestedIds != null && requestedIds.Count > 0)
            {
                builder.WithFilter(ctx =>
                {
                    var id = TestNodeMapper.GenerateStableId(
                        relativePath,
                        ctx.ContextPath.ToList(),
                        ctx.Spec.Description);
                    return requestedIds.Contains(id);
                });
            }

            // Run specs
            var runner = builder.Build();
            var results = runner.Run(rootContext);

            // Notify reporter of completion
            var report = SpecReportBuilder.Build(rootContext, results);
            await captureReporter.OnRunCompletedAsync(report);

            return new ExecutionResult(relativePath, absolutePath, captureReporter.Results);
        }
        finally
        {
            Dsl.Reset();
        }
    }

    /// <summary>
    /// Executes specs from multiple files based on requested test IDs.
    /// Groups IDs by file and executes each file with its relevant IDs.
    /// </summary>
    /// <param name="requestedIds">Set of spec IDs to run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results from all files.</returns>
    public async Task<IReadOnlyList<ExecutionResult>> ExecuteByIdsAsync(
        IEnumerable<string> requestedIds,
        CancellationToken cancellationToken = default)
    {
        // Group IDs by source file
        var idsByFile = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in requestedIds)
        {
            var colonIndex = id.IndexOf(':');
            if (colonIndex <= 0) continue;

            var relativePath = id[..colonIndex];
            if (!idsByFile.TryGetValue(relativePath, out var fileIds))
            {
                fileIds = new HashSet<string>(StringComparer.Ordinal);
                idsByFile[relativePath] = fileIds;
            }
            fileIds.Add(id);
        }

        // Execute each file
        var results = new List<ExecutionResult>();

        foreach (var (relativePath, fileIds) in idsByFile)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var absolutePath = Path.GetFullPath(relativePath, _projectDirectory);
            if (!File.Exists(absolutePath))
            {
                continue;
            }

            var result = await ExecuteFileAsync(absolutePath, fileIds, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Gets the relative path from the project directory.
    /// </summary>
    private string GetRelativePath(string absolutePath)
    {
        return Path.GetRelativePath(_projectDirectory, absolutePath);
    }
}

/// <summary>
/// Result of executing specs from a single file.
/// </summary>
/// <param name="RelativeSourceFile">Relative path to the source file.</param>
/// <param name="AbsoluteSourceFile">Absolute path to the source file.</param>
/// <param name="Results">Spec results from execution.</param>
internal sealed record ExecutionResult(
    string RelativeSourceFile,
    string AbsoluteSourceFile,
    IReadOnlyList<SpecResult> Results);
