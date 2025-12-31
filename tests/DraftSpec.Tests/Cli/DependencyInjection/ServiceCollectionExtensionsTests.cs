using DraftSpec.Cli;
using DraftSpec.Cli.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Tests.Cli.DependencyInjection;

/// <summary>
/// Tests for ServiceCollectionExtensions.
/// Validates that the DI container is configured correctly.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Test]
    public async Task AddDraftSpec_BuildsValidServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddDraftSpec();

        var provider = services.BuildServiceProvider();

        // Validate key services can be resolved
        await Assert.That(provider.GetService<ICommandFactory>()).IsNotNull();
        await Assert.That(provider.GetService<IConsole>()).IsNotNull();
        await Assert.That(provider.GetService<ISpecFinder>()).IsNotNull();
        await Assert.That(provider.GetService<IFileSystem>()).IsNotNull();
        await Assert.That(provider.GetService<IProjectResolver>()).IsNotNull();
    }
}
