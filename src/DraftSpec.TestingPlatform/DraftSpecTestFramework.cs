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
        // Session initialization will be expanded in future issues
        // to set up Roslyn scripting environment and discover CSX files
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
        return Task.FromResult(new CloseTestSessionResult { IsSuccess = true });
    }

    /// <summary>
    /// Discovers tests from CSX spec files.
    /// </summary>
    private async Task DiscoverTestsAsync(
        DiscoverTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        // TODO: Issue #120 - Implement CsxScriptHost for discovery
        // TODO: Issue #121 - Implement discovery mode

        // For now, publish a placeholder test node to verify the framework is recognized
        var testNode = new TestNode
        {
            Uid = new TestNodeUid("draftspec:placeholder"),
            DisplayName = "DraftSpec Placeholder (discovery not yet implemented)",
            Properties = new PropertyBag(DiscoveredTestNodeStateProperty.CachedInstance)
        };

        await context.MessageBus.PublishAsync(
            this,
            new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
    }

    /// <summary>
    /// Executes tests based on the run request.
    /// </summary>
    private async Task RunTestsAsync(
        RunTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        // TODO: Issue #120 - Implement CsxScriptHost for execution
        // TODO: Issue #122 - Implement filtered spec execution

        // For now, publish a passed placeholder to verify the framework is working
        var testNode = new TestNode
        {
            Uid = new TestNodeUid("draftspec:placeholder"),
            DisplayName = "DraftSpec Placeholder (execution not yet implemented)",
            Properties = new PropertyBag(PassedTestNodeStateProperty.CachedInstance)
        };

        await context.MessageBus.PublishAsync(
            this,
            new TestNodeUpdateMessage(request.Session.SessionUid, testNode));
    }
}
