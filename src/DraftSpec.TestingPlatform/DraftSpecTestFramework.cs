using Microsoft.Testing.Extensions.VSTestBridge;
using Microsoft.Testing.Extensions.VSTestBridge.Requests;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Microsoft.Testing.Platform adapter for DraftSpec.
/// Inherits from VSTestBridgedTestFrameworkBase to support both MTP and VSTest modes.
/// Acts as a thin adapter that delegates orchestration to ITestOrchestrator.
/// </summary>
internal class DraftSpecTestFramework : VSTestBridgedTestFrameworkBase
{
    // Lazy-initialized components (created on first use with project directory)
    private ISpecDiscoverer? _discoverer;
    private IMtpSpecExecutor? _executor;
    private ITestOrchestrator? _orchestrator;
    private string? _projectDirectory;

    /// <summary>
    /// Unique identifier for this test framework.
    /// </summary>
    public override string Uid => "DraftSpec.TestingPlatform";

    /// <summary>
    /// Version of the test framework adapter.
    /// </summary>
    public override string Version => typeof(DraftSpecTestFramework).Assembly.GetName().Version?.ToString() ?? "1.0.0";

    /// <summary>
    /// Display name shown in test output.
    /// </summary>
    public override string DisplayName => "DraftSpec";

    /// <summary>
    /// Description of the test framework.
    /// </summary>
    public override string Description => "RSpec-inspired testing framework for .NET";

    /// <summary>
    /// Creates a new instance of the DraftSpec test framework adapter.
    /// </summary>
    /// <param name="capabilities">Test framework capabilities.</param>
    /// <param name="serviceProvider">Service provider for dependency resolution.</param>
    /// <param name="discoverer">Optional discoverer for testing. Defaults to SpecDiscoverer.</param>
    /// <param name="executor">Optional executor for testing. Defaults to MtpSpecExecutor.</param>
    /// <param name="orchestrator">Optional orchestrator for testing. Defaults to TestOrchestrator.</param>
    /// <param name="projectDirectory">Optional project directory for testing.</param>
    public DraftSpecTestFramework(
        ITestFrameworkCapabilities capabilities,
        IServiceProvider serviceProvider,
        ISpecDiscoverer? discoverer = null,
        IMtpSpecExecutor? executor = null,
        ITestOrchestrator? orchestrator = null,
        string? projectDirectory = null)
        : base(serviceProvider, capabilities)
    {
        _discoverer = discoverer;
        _executor = executor;
        _orchestrator = orchestrator;
        _projectDirectory = projectDirectory;
    }

    /// <summary>
    /// Whether this framework is enabled.
    /// </summary>
    public override Task<bool> IsEnabledAsync() => Task.FromResult(true);

    /// <summary>
    /// Called at the start of a test session to initialize the framework.
    /// </summary>
    public override Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        EnsureInitialized();
        return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
    }

    /// <summary>
    /// Processes standard MTP test execution requests.
    /// </summary>
    protected override async Task ExecuteRequestAsync(
        TestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        switch (request)
        {
            case DiscoverTestExecutionRequest discoverRequest:
                await DiscoverTestsInternalAsync(discoverRequest, messageBus, cancellationToken).ConfigureAwait(false);
                break;

            case RunTestExecutionRequest runRequest:
                await RunTestsInternalAsync(runRequest, messageBus, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    /// <summary>
    /// Called at the end of a test session for cleanup.
    /// </summary>
    public override Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        ResetState();
        return Task.FromResult(new CloseTestSessionResult { IsSuccess = true });
    }

    /// <summary>
    /// Discovers tests via VSTest bridge (called by IDE test explorers).
    /// </summary>
    protected override async Task DiscoverTestsAsync(
        VSTestDiscoverTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();

        if (_orchestrator == null)
            return;

        var publisher = new MessageBusPublisher(messageBus, this, request.Session.SessionUid);
        await _orchestrator.DiscoverTestsAsync(publisher, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs tests via VSTest bridge (called by IDE test explorers).
    /// </summary>
    protected override async Task RunTestsAsync(
        VSTestRunTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();

        if (_orchestrator == null)
            return;

        var publisher = new MessageBusPublisher(messageBus, this, request.Session.SessionUid);
        await _orchestrator.RunTestsAsync(requestedTestIds: null, publisher, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Ensures the framework is initialized with discoverer, executor, and orchestrator.
    /// Uses assembly location as project directory if not provided.
    /// Internal for testing.
    /// </summary>
    internal void EnsureInitialized()
    {
        if (_projectDirectory == null)
        {
            var assemblyLocation = typeof(DraftSpecTestFramework).Assembly.Location;
            _projectDirectory = Path.GetDirectoryName(assemblyLocation) ?? Environment.CurrentDirectory;
        }

        _discoverer ??= new SpecDiscoverer(_projectDirectory);
        _executor ??= new MtpSpecExecutor(_projectDirectory);
        _orchestrator ??= new TestOrchestrator(_discoverer, _executor);
    }

    /// <summary>
    /// Resets the framework state (used during close and for testing).
    /// Internal for testing.
    /// </summary>
    internal void ResetState()
    {
        _discoverer = null;
        _executor = null;
        _orchestrator = null;
    }

    /// <summary>
    /// Discovers tests from CSX spec files (internal MTP path).
    /// </summary>
    private async Task DiscoverTestsInternalAsync(
        DiscoverTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        if (_orchestrator == null)
            return;

        var publisher = new MessageBusPublisher(messageBus, this, request.Session.SessionUid);
        await _orchestrator.DiscoverTestsAsync(publisher, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes tests based on the run request (internal MTP path).
    /// </summary>
    private async Task RunTestsInternalAsync(
        RunTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        if (_orchestrator == null)
            return;

        var requestedTestIds = ExtractTestIds(request.Filter);
        var publisher = new MessageBusPublisher(messageBus, this, request.Session.SessionUid);
        await _orchestrator.RunTestsAsync(requestedTestIds, publisher, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Extracts test IDs from the request filter.
    /// Internal static for testing.
    /// </summary>
    /// <param name="filter">The test filter from the request.</param>
    /// <returns>Set of test IDs to run, or null if all tests should run.</returns>
    internal static IReadOnlySet<string>? ExtractTestIds(ITestExecutionFilter? filter)
    {
        if (filter is TestNodeUidListFilter uidFilter && uidFilter.TestNodeUids.Length > 0)
        {
            return uidFilter.TestNodeUids.Select(uid => uid.Value).ToHashSet();
        }

        return null;
    }
}
