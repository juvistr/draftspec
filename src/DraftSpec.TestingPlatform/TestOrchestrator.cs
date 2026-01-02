namespace DraftSpec.TestingPlatform;

/// <summary>
/// Orchestrates test discovery and execution workflows.
/// Contains the testable business logic extracted from DraftSpecTestFramework.
/// </summary>
internal class TestOrchestrator : ITestOrchestrator
{
    private readonly ISpecDiscoverer _discoverer;
    private readonly IMtpSpecExecutor _executor;

    /// <summary>
    /// Creates a new test orchestrator.
    /// </summary>
    /// <param name="discoverer">The spec discoverer to use.</param>
    /// <param name="executor">The spec executor to use.</param>
    public TestOrchestrator(ISpecDiscoverer discoverer, IMtpSpecExecutor executor)
    {
        _discoverer = discoverer ?? throw new ArgumentNullException(nameof(discoverer));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <inheritdoc />
    public async Task DiscoverTestsAsync(
        ITestNodePublisher publisher,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);

        var result = await _discoverer.DiscoverAsync(cancellationToken).ConfigureAwait(false);

        // Publish discovered specs
        foreach (var spec in result.Specs)
        {
            var testNode = TestNodeMapper.CreateDiscoveryNode(spec);
            await publisher.PublishAsync(testNode, cancellationToken).ConfigureAwait(false);
        }

        // Publish discovery errors as error nodes
        foreach (var error in result.Errors)
        {
            var testNode = TestNodeMapper.CreateErrorNode(error);
            await publisher.PublishAsync(testNode, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RunTestsAsync(
        IReadOnlySet<string>? requestedTestIds,
        ITestNodePublisher publisher,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);

        IReadOnlyList<ExecutionResult> executionResults;
        IReadOnlyList<DiscoveryError> discoveryErrors = [];
        IReadOnlyList<DiscoveredSpec> compilationErrorSpecs = [];

        if (requestedTestIds is { Count: > 0 })
        {
            // Run specific tests by ID - discover to identify compilation errors,
            // but only report results for requested specs
            var discoveryResult = await _discoverer.DiscoverAsync(cancellationToken).ConfigureAwait(false);

            // Filter to only requested IDs
            var requestedSpecs = discoveryResult.Specs
                .Where(s => requestedTestIds.Contains(s.Id))
                .ToList();

            // Separate into executable and compilation error specs
            var executableIds = requestedSpecs
                .Where(s => !s.HasCompilationError)
                .Select(s => s.Id)
                .ToHashSet();
            compilationErrorSpecs = requestedSpecs.Where(s => s.HasCompilationError).ToList();

            // Execute only executable specs (results are filtered to requested IDs in executor)
            executionResults = executableIds.Count > 0
                ? await _executor.ExecuteByIdsAsync(executableIds, cancellationToken).ConfigureAwait(false)
                : [];
        }
        else
        {
            // Run all tests - discover and execute all files
            var discoveryResult = await _discoverer.DiscoverAsync(cancellationToken).ConfigureAwait(false);
            discoveryErrors = discoveryResult.Errors;

            // Separate specs with compilation errors from executable specs
            var executableSpecs = discoveryResult.Specs.Where(s => !s.HasCompilationError).ToList();
            compilationErrorSpecs = discoveryResult.Specs.Where(s => s.HasCompilationError).ToList();

            // Group executable specs by file and execute
            var fileGroups = executableSpecs.GroupBy(s => s.SourceFile);
            var results = new List<ExecutionResult>();

            foreach (var group in fileGroups)
            {
                var result = await _executor.ExecuteFileAsync(group.Key, cancellationToken).ConfigureAwait(false);
                results.Add(result);
            }

            executionResults = results;
        }

        // Publish discovery errors as error nodes
        foreach (var error in discoveryErrors)
        {
            var testNode = TestNodeMapper.CreateErrorNode(error);
            await publisher.PublishAsync(testNode, cancellationToken).ConfigureAwait(false);
        }

        // Publish specs with compilation errors as failed test nodes
        foreach (var spec in compilationErrorSpecs)
        {
            var testNode = TestNodeMapper.CreateCompilationErrorResultNode(spec);
            await publisher.PublishAsync(testNode, cancellationToken).ConfigureAwait(false);
        }

        // Publish results
        foreach (var execResult in executionResults)
        {
            foreach (var specResult in execResult.Results)
            {
                var testNode = TestNodeMapper.CreateResultNode(
                    execResult.RelativeSourceFile,
                    execResult.AbsoluteSourceFile,
                    specResult);

                await publisher.PublishAsync(testNode, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
