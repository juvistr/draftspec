using DraftSpec.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Tests.Configuration;

/// <summary>
/// Tests for DraftSpecConfiguration DI integration with IServiceCollection.
/// </summary>
public class DraftSpecConfigurationDiTests
{
    [Test]
    public async Task AddService_RegistersSingleton_CanBeRetrieved()
    {
        var config = new DraftSpecConfiguration();
        var service = new TestService();

        config.AddService(service);

        await Assert.That(config.GetService<TestService>()).IsSameReferenceAs(service);
    }

    [Test]
    public async Task GetService_NotRegistered_ReturnsNull()
    {
        var config = new DraftSpecConfiguration();

        await Assert.That(config.GetService<TestService>()).IsNull();
    }

    [Test]
    public async Task Services_ExposesIServiceCollection()
    {
        var config = new DraftSpecConfiguration();

        await Assert.That(config.Services).IsAssignableTo<IServiceCollection>();
    }

    [Test]
    public async Task ServiceProvider_ExposesIServiceProvider()
    {
        var config = new DraftSpecConfiguration();

        await Assert.That(config.ServiceProvider).IsAssignableTo<IServiceProvider>();
    }

    [Test]
    public async Task Services_AddTransient_CreatesNewInstanceEachTime()
    {
        var config = new DraftSpecConfiguration();
        config.Services.AddTransient<TestService>();

        var service1 = config.ServiceProvider.GetService<TestService>();
        var service2 = config.ServiceProvider.GetService<TestService>();

        await Assert.That(service1).IsNotNull();
        await Assert.That(service2).IsNotNull();
        await Assert.That(service1).IsNotSameReferenceAs(service2);
    }

    [Test]
    public async Task Services_AddSingleton_ReturnsSameInstance()
    {
        var config = new DraftSpecConfiguration();
        config.Services.AddSingleton<TestService>();

        var service1 = config.ServiceProvider.GetService<TestService>();
        var service2 = config.ServiceProvider.GetService<TestService>();

        await Assert.That(service1).IsNotNull();
        await Assert.That(service1).IsSameReferenceAs(service2);
    }

    [Test]
    public async Task Services_AddSingletonWithInterface_ResolvesCorrectly()
    {
        var config = new DraftSpecConfiguration();
        config.Services.AddSingleton<ITestService, TestService>();

        var service = config.ServiceProvider.GetService<ITestService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<TestService>();
    }

    [Test]
    public async Task ServiceProvider_GetRequiredService_ThrowsWhenNotRegistered()
    {
        var config = new DraftSpecConfiguration();

        Assert.Throws<InvalidOperationException>(() =>
            config.ServiceProvider.GetRequiredService<TestService>());
    }

    [Test]
    public async Task Services_SupportsFactoryRegistration()
    {
        var config = new DraftSpecConfiguration();
        var callCount = 0;
        config.Services.AddTransient<TestService>(_ =>
        {
            callCount++;
            return new TestService();
        });

        config.ServiceProvider.GetService<TestService>();
        config.ServiceProvider.GetService<TestService>();

        await Assert.That(callCount).IsEqualTo(2);
    }

    private interface ITestService;
    private class TestService : ITestService;
}
