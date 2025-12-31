using DraftSpec.Mcp.DependencyInjection;
using DraftSpec.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Tests.Mcp.DependencyInjection;

/// <summary>
/// Tests for MCP service registration.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Test]
    public async Task AddDraftSpecMcp_RegistersTempFileManager()
    {
        var services = new ServiceCollection();

        services.AddDraftSpecMcp();

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<TempFileManager>();

        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task AddDraftSpecMcp_RegistersAsyncProcessRunner()
    {
        var services = new ServiceCollection();

        services.AddDraftSpecMcp();

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IAsyncProcessRunner>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<SystemAsyncProcessRunner>();
    }

    [Test]
    public async Task AddDraftSpecMcp_RegistersSessionManager()
    {
        var services = new ServiceCollection();

        services.AddDraftSpecMcp();

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<SessionManager>();

        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task AddDraftSpecMcp_RegistersSpecExecutionService()
    {
        var services = new ServiceCollection();

        services.AddDraftSpecMcp();

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<SpecExecutionService>();

        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task AddDraftSpecMcp_RegistersISpecExecutionService()
    {
        var services = new ServiceCollection();

        services.AddDraftSpecMcp();

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<ISpecExecutionService>();

        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task AddDraftSpecMcp_ISpecExecutionService_ResolvesSameInstanceAsConcreteType()
    {
        var services = new ServiceCollection();

        services.AddDraftSpecMcp();

        var provider = services.BuildServiceProvider();
        var concrete = provider.GetService<SpecExecutionService>();
        var interfaceImpl = provider.GetService<ISpecExecutionService>();

        await Assert.That(concrete).IsNotNull();
        await Assert.That(interfaceImpl).IsNotNull();
        await Assert.That(ReferenceEquals(concrete, interfaceImpl)).IsTrue();
    }

    [Test]
    public async Task AddDraftSpecMcp_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddDraftSpecMcp();

        await Assert.That(result).IsEqualTo(services);
    }

    [Test]
    public async Task AddDraftSpecMcp_AllServicesAreSingletons()
    {
        var services = new ServiceCollection();

        services.AddDraftSpecMcp();

        var provider = services.BuildServiceProvider();

        // Get services twice and verify same instance
        var tempFile1 = provider.GetService<TempFileManager>();
        var tempFile2 = provider.GetService<TempFileManager>();
        await Assert.That(ReferenceEquals(tempFile1, tempFile2)).IsTrue();

        var runner1 = provider.GetService<IAsyncProcessRunner>();
        var runner2 = provider.GetService<IAsyncProcessRunner>();
        await Assert.That(ReferenceEquals(runner1, runner2)).IsTrue();
    }
}
