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
/// </summary>
internal sealed class DraftSpecTestFramework : VSTestBridgedTestFrameworkBase
{
    // Lazy-initialized components (created on first use with project directory)
    private SpecDiscoverer? _discoverer;
    private MtpSpecExecutor? _executor;
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
    public DraftSpecTestFramework(
        ITestFrameworkCapabilities capabilities,
        IServiceProvider serviceProvider)
        : base(serviceProvider, capabilities)
    {
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
        // Use the test assembly location as the base directory for finding CSX files.
        // CSX files are copied to the output directory by MSBuild targets.
        // Environment.CurrentDirectory is unreliable when running from IDE.
        var assemblyLocation = typeof(DraftSpecTestFramework).Assembly.Location;
        _projectDirectory = Path.GetDirectoryName(assemblyLocation) ?? Environment.CurrentDirectory;

        _discoverer = new SpecDiscoverer(_projectDirectory);
        _executor = new MtpSpecExecutor(_projectDirectory);

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
                await DiscoverTestsInternalAsync(discoverRequest, messageBus, cancellationToken);
                break;

            case RunTestExecutionRequest runRequest:
                await RunTestsInternalAsync(runRequest, messageBus, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Called at the end of a test session for cleanup.
    /// </summary>
    public override Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        _discoverer = null;
        _executor = null;
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
        // Initialize components if not already done
        EnsureInitialized();

        if (_discoverer == null)
        {
            return;
        }

        var result = await _discoverer.DiscoverAsync(cancellationToken);

        // Publish discovered specs
        foreach (var spec in result.Specs)
        {
            var testNode = TestNodeMapper.CreateDiscoveryNode(spec);

            await messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
        }

        // Publish discovery errors as error nodes
        foreach (var error in result.Errors)
        {
            var testNode = TestNodeMapper.CreateErrorNode(error);

            await messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
        }
    }

    /// <summary>
    /// Runs tests via VSTest bridge (called by IDE test explorers).
    /// </summary>
    protected override async Task RunTestsAsync(
        VSTestRunTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        // Initialize components if not already done
        EnsureInitialized();

        if (_executor == null || _discoverer == null)
        {
            return;
        }

        // Run all discovered tests (VSTest provides filtering via its own mechanisms)
        var discoveryResult = await _discoverer.DiscoverAsync(cancellationToken);

        // Report discovery errors
        foreach (var error in discoveryResult.Errors)
        {
            var testNode = TestNodeMapper.CreateErrorNode(error);

            await messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
        }

        // Separate specs with compilation errors from executable specs
        var executableSpecs = discoveryResult.Specs.Where(s => !s.HasCompilationError).ToList();
        var compilationErrorSpecs = discoveryResult.Specs.Where(s => s.HasCompilationError).ToList();

        // Report specs with compilation errors as failed
        foreach (var spec in compilationErrorSpecs)
        {
            var testNode = TestNodeMapper.CreateCompilationErrorResultNode(spec);

            await messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
        }

        // Execute discovered specs (only those without compilation errors)
        var fileGroups = executableSpecs.GroupBy(s => s.SourceFile);

        foreach (var group in fileGroups)
        {
            var result = await _executor.ExecuteFileAsync(group.Key, cancellationToken);

            foreach (var specResult in result.Results)
            {
                var testNode = TestNodeMapper.CreateResultNode(
                    result.RelativeSourceFile,
                    result.AbsoluteSourceFile,
                    specResult);

                await messageBus.PublishAsync(
                    this,
                    new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
            }
        }
    }

    private void EnsureInitialized()
    {
        if (_projectDirectory == null)
        {
            var assemblyLocation = typeof(DraftSpecTestFramework).Assembly.Location;
            _projectDirectory = Path.GetDirectoryName(assemblyLocation) ?? Environment.CurrentDirectory;
            _discoverer = new SpecDiscoverer(_projectDirectory);
            _executor = new MtpSpecExecutor(_projectDirectory);
        }
    }

    /// <summary>
    /// Discovers tests from CSX spec files (internal MTP path).
    /// </summary>
    private async Task DiscoverTestsInternalAsync(
        DiscoverTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        if (_discoverer == null)
        {
            return;
        }

        var result = await _discoverer.DiscoverAsync(cancellationToken);

        // Publish discovered specs
        foreach (var spec in result.Specs)
        {
            var testNode = TestNodeMapper.CreateDiscoveryNode(spec);

            await messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
        }

        // Publish discovery errors as error nodes
        foreach (var error in result.Errors)
        {
            var testNode = TestNodeMapper.CreateErrorNode(error);

            await messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
        }
    }

    /// <summary>
    /// Executes tests based on the run request (internal MTP path).
    /// </summary>
    private async Task RunTestsInternalAsync(
        RunTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        if (_executor == null || _discoverer == null)
        {
            return;
        }

        // Check if specific tests are requested via filter
        var filter = request.Filter;
        IReadOnlyList<ExecutionResult> executionResults;
        IReadOnlyList<DiscoveryError> discoveryErrors = [];
        IReadOnlyList<DiscoveredSpec> compilationErrorSpecs = [];

        if (filter is TestNodeUidListFilter uidFilter && uidFilter.TestNodeUids.Length > 0)
        {
            // Run specific tests by ID - discover to identify compilation errors,
            // but only report results for requested specs
            var discoveryResult = await _discoverer.DiscoverAsync(cancellationToken);

            // Filter to only requested IDs
            var requestedIds = uidFilter.TestNodeUids.Select(uid => uid.Value).ToHashSet();
            var requestedSpecs = discoveryResult.Specs.Where(s => requestedIds.Contains(s.Id)).ToList();

            // Separate into executable and compilation error specs
            var executableIds = requestedSpecs
                .Where(s => !s.HasCompilationError)
                .Select(s => s.Id)
                .ToHashSet();
            compilationErrorSpecs = requestedSpecs.Where(s => s.HasCompilationError).ToList();

            // Execute only executable specs (results are filtered to requested IDs in executor)
            executionResults = executableIds.Count > 0
                ? await _executor.ExecuteByIdsAsync(executableIds, cancellationToken)
                : [];
        }
        else
        {
            // Run all tests - discover and execute all files
            var discoveryResult = await _discoverer.DiscoverAsync(cancellationToken);
            discoveryErrors = discoveryResult.Errors;

            // Separate specs with compilation errors from executable specs
            var executableSpecs = discoveryResult.Specs.Where(s => !s.HasCompilationError).ToList();
            compilationErrorSpecs = discoveryResult.Specs.Where(s => s.HasCompilationError).ToList();

            // Group executable specs by file and execute
            var fileGroups = executableSpecs.GroupBy(s => s.SourceFile);
            var results = new List<ExecutionResult>();

            foreach (var group in fileGroups)
            {
                var result = await _executor.ExecuteFileAsync(group.Key, cancellationToken);
                results.Add(result);
            }

            executionResults = results;
        }

        // Publish discovery errors as error nodes
        foreach (var error in discoveryErrors)
        {
            var testNode = TestNodeMapper.CreateErrorNode(error);

            await messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
        }

        // Publish specs with compilation errors as failed test nodes
        foreach (var spec in compilationErrorSpecs)
        {
            var testNode = TestNodeMapper.CreateCompilationErrorResultNode(spec);

            await messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
        }

        // Publish results to MTP
        foreach (var execResult in executionResults)
        {
            foreach (var specResult in execResult.Results)
            {
                var testNode = TestNodeMapper.CreateResultNode(
                    execResult.RelativeSourceFile,
                    execResult.AbsoluteSourceFile,
                    specResult);

                await messageBus.PublishAsync(
                    this,
                    new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
            }
        }
    }
}
