using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Microsoft.Testing.Platform adapter for DraftSpec.
/// Implements ITestFramework to integrate DraftSpec specs with dotnet test.
/// </summary>
internal sealed class DraftSpecTestFramework : ITestFramework, IDataProducer
{
    private readonly ITestFrameworkCapabilities _capabilities;
    private readonly IServiceProvider _serviceProvider;

    // Lazy-initialized components (created on first use with project directory)
    private SpecDiscoverer? _discoverer;
    private MtpSpecExecutor? _executor;
    private string? _projectDirectory;

    /// <summary>
    /// Unique identifier for this test framework.
    /// </summary>
    public string Uid => "DraftSpec.TestingPlatform";

    /// <summary>
    /// Version of the test framework adapter.
    /// </summary>
    public string Version => typeof(DraftSpecTestFramework).Assembly.GetName().Version?.ToString() ?? "1.0.0";

    /// <summary>
    /// Display name shown in test output.
    /// </summary>
    public string DisplayName => "DraftSpec";

    /// <summary>
    /// Description of the test framework.
    /// </summary>
    public string Description => "RSpec-inspired testing framework for .NET";

    /// <summary>
    /// Types of data this framework produces (test results).
    /// </summary>
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    /// <summary>
    /// Creates a new instance of the DraftSpec test framework adapter.
    /// </summary>
    public DraftSpecTestFramework(
        ITestFrameworkCapabilities capabilities,
        IServiceProvider serviceProvider)
    {
        _capabilities = capabilities;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Whether this framework is enabled.
    /// </summary>
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    /// <summary>
    /// Called at the start of a test session to initialize the framework.
    /// </summary>
    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        // Initialize with the current directory as project root
        // In a real scenario, we'd get this from the test assembly location
        _projectDirectory = Environment.CurrentDirectory;
        _discoverer = new SpecDiscoverer(_projectDirectory);
        _executor = new MtpSpecExecutor(_projectDirectory);

        return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
    }

    /// <summary>
    /// Processes test discovery or execution requests.
    /// </summary>
    public async Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        switch (context.Request)
        {
            case DiscoverTestExecutionRequest discoverRequest:
                await DiscoverTestsAsync(discoverRequest, context);
                break;

            case RunTestExecutionRequest runRequest:
                await RunTestsAsync(runRequest, context);
                break;
        }

        // CRITICAL: Must call Complete() to signal request is finished
        context.Complete();
    }

    /// <summary>
    /// Called at the end of a test session for cleanup.
    /// </summary>
    public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        _discoverer = null;
        _executor = null;
        return Task.FromResult(new CloseTestSessionResult { IsSuccess = true });
    }

    /// <summary>
    /// Discovers tests from CSX spec files.
    /// </summary>
    private async Task DiscoverTestsAsync(
        DiscoverTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        if (_discoverer == null)
        {
            return;
        }

        var specs = await _discoverer.DiscoverAsync(context.CancellationToken);

        foreach (var spec in specs)
        {
            var testNode = TestNodeMapper.CreateDiscoveryNode(spec);

            await context.MessageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
        }
    }

    /// <summary>
    /// Executes tests based on the run request.
    /// </summary>
    private async Task RunTestsAsync(
        RunTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        if (_executor == null || _discoverer == null)
        {
            return;
        }

        // Check if specific tests are requested via filter
        var filter = request.Filter;
        IReadOnlyList<ExecutionResult> executionResults;

        if (filter is TestNodeUidListFilter uidFilter && uidFilter.TestNodeUids.Length > 0)
        {
            // Run specific tests by ID
            var requestedIds = uidFilter.TestNodeUids.Select(uid => uid.Value).ToHashSet();
            executionResults = await _executor.ExecuteByIdsAsync(requestedIds, context.CancellationToken);
        }
        else
        {
            // Run all tests - discover and execute all files
            var specs = await _discoverer.DiscoverAsync(context.CancellationToken);

            // Group by file and execute
            var fileGroups = specs.GroupBy(s => s.SourceFile);
            var results = new List<ExecutionResult>();

            foreach (var group in fileGroups)
            {
                var result = await _executor.ExecuteFileAsync(group.Key, context.CancellationToken);
                results.Add(result);
            }

            executionResults = results;
        }

        // Publish results to MTP
        foreach (var execResult in executionResults)
        {
            foreach (var specResult in execResult.Results)
            {
                var testNode = TestNodeMapper.CreateResultNode(execResult.RelativeSourceFile, specResult);

                await context.MessageBus.PublishAsync(
                    this,
                    new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
            }
        }
    }
}
