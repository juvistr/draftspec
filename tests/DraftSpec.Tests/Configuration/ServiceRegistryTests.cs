using DraftSpec.Configuration;

namespace DraftSpec.Tests.Configuration;

/// <summary>
/// Tests for ServiceRegistry.
/// </summary>
public class ServiceRegistryTests
{
    [Test]
    public async Task Register_Service_CanBeRetrieved()
    {
        var registry = new ServiceRegistry();
        var service = new TestService();

        registry.Register(service);

        await Assert.That(registry.GetService<TestService>()).IsSameReferenceAs(service);
    }

    [Test]
    public async Task GetService_NotRegistered_ReturnsNull()
    {
        var registry = new ServiceRegistry();

        await Assert.That(registry.GetService<TestService>()).IsNull();
    }

    [Test]
    public async Task HasService_WhenRegistered_ReturnsTrue()
    {
        var registry = new ServiceRegistry();
        registry.Register(new TestService());

        await Assert.That(registry.HasService<TestService>()).IsTrue();
    }

    [Test]
    public async Task HasService_NotRegistered_ReturnsFalse()
    {
        var registry = new ServiceRegistry();

        await Assert.That(registry.HasService<TestService>()).IsFalse();
    }

    [Test]
    public async Task Count_ReturnsNumberOfServices()
    {
        var registry = new ServiceRegistry();

        await Assert.That(registry.Count).IsEqualTo(0);

        registry.Register(new TestService());
        await Assert.That(registry.Count).IsEqualTo(1);

        registry.Register(new AnotherService());
        await Assert.That(registry.Count).IsEqualTo(2);
    }

    [Test]
    public void Register_NullService_ThrowsArgumentNullException()
    {
        var registry = new ServiceRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register<TestService>(null!));
    }

    [Test]
    public async Task Register_SameTypeTwice_OverwritesPrevious()
    {
        var registry = new ServiceRegistry();
        var service1 = new TestService();
        var service2 = new TestService();

        registry.Register(service1);
        registry.Register(service2);

        await Assert.That(registry.GetService<TestService>()).IsSameReferenceAs(service2);
        await Assert.That(registry.Count).IsEqualTo(1);
    }

    private class TestService;
    private class AnotherService;
}
