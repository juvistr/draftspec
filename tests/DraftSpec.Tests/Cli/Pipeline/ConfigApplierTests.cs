using DraftSpec.Cli;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.Pipeline;

namespace DraftSpec.Tests.Cli.Pipeline;

/// <summary>
/// Tests for ConfigApplier.
/// </summary>
public class ConfigApplierTests
{
    #region ApplyConfig Tests

    [Test]
    public async Task ApplyConfig_NoConfigFound_DoesNotModifyOptions()
    {
        var mockLoader = new MockConfigLoader(new ConfigLoadResult(null, null, null));
        var applier = new ConfigApplier(mockLoader);
        var options = new CliOptions { Parallel = false, Bail = false };

        applier.ApplyConfig(options);

        // Options should remain unchanged
        await Assert.That(options.Parallel).IsFalse();
        await Assert.That(options.Bail).IsFalse();
    }

    [Test]
    public async Task ApplyConfig_ConfigFound_AppliesDefaults()
    {
        var config = new DraftSpecProjectConfig { Parallel = true, Bail = true };
        var mockLoader = new MockConfigLoader(new ConfigLoadResult(config, null, "/test/draftspec.json"));
        var applier = new ConfigApplier(mockLoader);
        var options = new CliOptions();

        applier.ApplyConfig(options);

        await Assert.That(options.Parallel).IsTrue();
        await Assert.That(options.Bail).IsTrue();
    }

    [Test]
    public async Task ApplyConfig_ConfigError_ThrowsInvalidOperationException()
    {
        var mockLoader = new MockConfigLoader(new ConfigLoadResult(null, "Invalid JSON", "/test/draftspec.json"));
        var applier = new ConfigApplier(mockLoader);
        var options = new CliOptions();

        await Assert.That(() => applier.ApplyConfig(options))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ApplyConfig_ExplicitlySetValue_NotOverridden()
    {
        var config = new DraftSpecProjectConfig { Parallel = true };
        var mockLoader = new MockConfigLoader(new ConfigLoadResult(config, null, "/test/draftspec.json"));
        var applier = new ConfigApplier(mockLoader);
        var options = new CliOptions { Parallel = false };
        options.ExplicitlySet.Add(nameof(CliOptions.Parallel));

        applier.ApplyConfig(options);

        // ExplicitlySet should prevent config from overriding
        await Assert.That(options.Parallel).IsFalse();
    }

    [Test]
    public async Task ApplyConfig_UsesPathFromOptions()
    {
        var mockLoader = new MockConfigLoader(new ConfigLoadResult(null, null, null));
        var applier = new ConfigApplier(mockLoader);
        var options = new CliOptions { Path = "/custom/path" };

        applier.ApplyConfig(options);

        await Assert.That(mockLoader.LastLoadPath).IsEqualTo("/custom/path");
    }

    #endregion

    #region Helper Classes

    private class MockConfigLoader : IConfigLoader
    {
        private readonly ConfigLoadResult _result;

        public string? LastLoadPath { get; private set; }

        public MockConfigLoader(ConfigLoadResult result)
        {
            _result = result;
        }

        public ConfigLoadResult Load(string? path = null)
        {
            LastLoadPath = path;
            return _result;
        }
    }

    #endregion
}
