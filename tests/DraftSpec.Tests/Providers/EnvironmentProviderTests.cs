using DraftSpec.Providers;

namespace DraftSpec.Tests.Providers;

/// <summary>
/// Tests for IEnvironmentProvider implementations.
/// </summary>
public class EnvironmentProviderTests
{
    #region SystemEnvironmentProvider Tests

    [Test]
    public async Task SystemEnvironmentProvider_Instance_ReturnsSingleton()
    {
        var instance1 = SystemEnvironmentProvider.Instance;
        var instance2 = SystemEnvironmentProvider.Instance;

        await Assert.That(ReferenceEquals(instance1, instance2)).IsTrue();
    }

    [Test]
    public async Task SystemEnvironmentProvider_GetEnvironmentVariable_ReturnsRealValue()
    {
        // PATH should exist on all systems
        var value = SystemEnvironmentProvider.Instance.GetEnvironmentVariable("PATH");

        await Assert.That(value).IsNotNull();
    }

    [Test]
    public async Task SystemEnvironmentProvider_GetEnvironmentVariable_NonExistent_ReturnsNull()
    {
        var value = SystemEnvironmentProvider.Instance.GetEnvironmentVariable("DRAFTSPEC_NONEXISTENT_VAR_12345");

        await Assert.That(value).IsNull();
    }

    #endregion

    #region InMemoryEnvironmentProvider Tests

    [Test]
    public async Task InMemoryEnvironmentProvider_Empty_ReturnsNull()
    {
        var provider = new InMemoryEnvironmentProvider();

        var value = provider.GetEnvironmentVariable("ANY_KEY");

        await Assert.That(value).IsNull();
    }

    [Test]
    public async Task InMemoryEnvironmentProvider_SetAndGet_Works()
    {
        var provider = new InMemoryEnvironmentProvider();

        provider.SetEnvironmentVariable("MY_VAR", "my_value");
        var value = provider.GetEnvironmentVariable("MY_VAR");

        await Assert.That(value).IsEqualTo("my_value");
    }

    [Test]
    public async Task InMemoryEnvironmentProvider_SetNull_RemovesVariable()
    {
        var provider = new InMemoryEnvironmentProvider();
        provider.SetEnvironmentVariable("MY_VAR", "my_value");

        provider.SetEnvironmentVariable("MY_VAR", null);
        var value = provider.GetEnvironmentVariable("MY_VAR");

        await Assert.That(value).IsNull();
    }

    [Test]
    public async Task InMemoryEnvironmentProvider_Clear_RemovesAllVariables()
    {
        var provider = new InMemoryEnvironmentProvider();
        provider.SetEnvironmentVariable("VAR1", "value1");
        provider.SetEnvironmentVariable("VAR2", "value2");

        provider.Clear();

        await Assert.That(provider.GetEnvironmentVariable("VAR1")).IsNull();
        await Assert.That(provider.GetEnvironmentVariable("VAR2")).IsNull();
    }

    [Test]
    public async Task InMemoryEnvironmentProvider_InitWithDictionary_ContainsValues()
    {
        var provider = new InMemoryEnvironmentProvider(new Dictionary<string, string>
        {
            ["KEY1"] = "value1",
            ["KEY2"] = "value2"
        });

        await Assert.That(provider.GetEnvironmentVariable("KEY1")).IsEqualTo("value1");
        await Assert.That(provider.GetEnvironmentVariable("KEY2")).IsEqualTo("value2");
    }

    [Test]
    public async Task InMemoryEnvironmentProvider_IsCaseInsensitive()
    {
        var provider = new InMemoryEnvironmentProvider();
        provider.SetEnvironmentVariable("MyVar", "value");

        await Assert.That(provider.GetEnvironmentVariable("MYVAR")).IsEqualTo("value");
        await Assert.That(provider.GetEnvironmentVariable("myvar")).IsEqualTo("value");
        await Assert.That(provider.GetEnvironmentVariable("MyVar")).IsEqualTo("value");
    }

    [Test]
    public async Task InMemoryEnvironmentProvider_Overwrite_ReplacesValue()
    {
        var provider = new InMemoryEnvironmentProvider();
        provider.SetEnvironmentVariable("KEY", "original");
        provider.SetEnvironmentVariable("KEY", "updated");

        await Assert.That(provider.GetEnvironmentVariable("KEY")).IsEqualTo("updated");
    }

    #endregion
}
