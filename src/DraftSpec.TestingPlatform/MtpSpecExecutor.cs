using DraftSpec.Configuration;
using DraftSpec.Scripting;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Executes specs for MTP integration with filtering and result capture.
/// </summary>
internal sealed class MtpSpecExecutor : IMtpSpecExecutor
{
    private readonly string _projectDirectory;
    private readonly IScriptHost _scriptHost;
    private readonly ISpecStateManager _stateManager;

    /// <summary>
    /// Creates a new MTP spec executor.
    /// </summary>
    /// <param name="projectDirectory">The project root directory.</param>
    /// <param name="scriptHost">Optional script host for testing. Defaults to CsxScriptHost.</param>
    /// <param name="stateManager">Optional state manager for testing. Defaults to DefaultSpecStateManager.</param>
    public MtpSpecExecutor(
        string projectDirectory,
        IScriptHost? scriptHost = null,
        ISpecStateManager? stateManager = null)
    {
        _projectDirectory = Path.GetFullPath(projectDirectory);
        _scriptHost = scriptHost ?? new CsxScriptHost(_projectDirectory);
        _stateManager = stateManager ?? new DefaultSpecStateManager();
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
        return await ExecuteFileAsync(csxFilePath, requestedIds: null, cancellationToken).ConfigureAwait(false);
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
        _stateManager.ResetState();

        try
        {
            // Execute script to build spec tree
            var rootContext = await _scriptHost.ExecuteAsync(absolutePath, cancellationToken).ConfigureAwait(false);

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
            await captureReporter.OnRunCompletedAsync(report).ConfigureAwait(false);

            // When filtering, only return results for requested specs
            // (filtered-out specs get Skipped status but shouldn't be reported to IDE)
            var finalResults = requestedIds != null && requestedIds.Count > 0
                ? captureReporter.Results.Where(r =>
                {
                    var id = TestNodeMapper.GenerateStableId(
                        relativePath,
                        r.ContextPath,
                        r.Spec.Description);
                    return requestedIds.Contains(id);
                }).ToList()
                : captureReporter.Results;

            return new ExecutionResult(relativePath, absolutePath, finalResults);
        }
        finally
        {
            _stateManager.ResetState();
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

            try
            {
                var result = await ExecuteFileAsync(absolutePath, fileIds, cancellationToken).ConfigureAwait(false);
                results.Add(result);
            }
            catch (Exception)
            {
                // File failed to compile - skip it silently since discovery should have
                // already identified these specs as having compilation errors.
                // If discovery missed it (e.g., file changed), the specs just won't be reported.
            }
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
