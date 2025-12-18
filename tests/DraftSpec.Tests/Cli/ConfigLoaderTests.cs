using DraftSpec.Cli;
using DraftSpec.Cli.Configuration;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for configuration file loading and merging.
/// </summary>
public class ConfigLoaderTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec-config-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); }
            catch { /* ignore cleanup errors */ }
        }
    }

    #region ConfigLoader.Load Tests

    [Test]
    public async Task Load_ReturnsNullConfig_WhenNoConfigFileExists()
    {
        var result = ConfigLoader.Load(_tempDir);

        await Assert.That(result.Config).IsNull();
        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Found).IsFalse();
    }

    [Test]
    public async Task Load_LoadsConfig_WhenFileExists()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        File.WriteAllText(configPath, """
            {
              "parallel": true,
              "bail": true,
              "timeout": 5000
            }
            """);

        var result = ConfigLoader.Load(_tempDir);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Config).IsNotNull();
        await Assert.That(result.Config!.Parallel).IsEqualTo(true);
        await Assert.That(result.Config.Bail).IsEqualTo(true);
        await Assert.That(result.Config.Timeout).IsEqualTo(5000);
    }

    [Test]
    public async Task Load_SupportsAllConfigOptions()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        File.WriteAllText(configPath, """
            {
              "specPattern": "**/*.spec.csx",
              "timeout": 10000,
              "parallel": true,
              "maxParallelism": 4,
              "reporters": ["console", "json"],
              "outputDirectory": "./test-results",
              "tags": {
                "include": ["unit", "fast"],
                "exclude": ["slow", "integration"]
              },
              "bail": true,
              "noCache": true,
              "format": "json"
            }
            """);

        var result = ConfigLoader.Load(_tempDir);

        await Assert.That(result.Success).IsTrue();
        var config = result.Config!;
        await Assert.That(config.SpecPattern).IsEqualTo("**/*.spec.csx");
        await Assert.That(config.Timeout).IsEqualTo(10000);
        await Assert.That(config.Parallel).IsEqualTo(true);
        await Assert.That(config.MaxParallelism).IsEqualTo(4);
        await Assert.That(config.Reporters).Contains("console");
        await Assert.That(config.Reporters).Contains("json");
        await Assert.That(config.OutputDirectory).IsEqualTo("./test-results");
        await Assert.That(config.Tags!.Include).Contains("unit");
        await Assert.That(config.Tags.Exclude).Contains("slow");
        await Assert.That(config.Bail).IsEqualTo(true);
        await Assert.That(config.NoCache).IsEqualTo(true);
        await Assert.That(config.Format).IsEqualTo("json");
    }

    [Test]
    public async Task Load_ReturnsError_WhenJsonIsInvalid()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        File.WriteAllText(configPath, "{ invalid json }");

        var result = ConfigLoader.Load(_tempDir);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsNotNull();
        await Assert.That(result.Error!).Contains("Error parsing");
        await Assert.That(result.Found).IsTrue();
    }

    [Test]
    public async Task Load_ReturnsError_WhenTimeoutIsInvalid()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        File.WriteAllText(configPath, """{ "timeout": -1 }""");

        var result = ConfigLoader.Load(_tempDir);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error!).Contains("timeout must be a positive number");
    }

    [Test]
    public async Task Load_ReturnsError_WhenMaxParallelismIsInvalid()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        File.WriteAllText(configPath, """{ "maxParallelism": 0 }""");

        var result = ConfigLoader.Load(_tempDir);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error!).Contains("maxParallelism must be a positive number");
    }

    [Test]
    public async Task Load_IgnoresUnknownProperties()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        File.WriteAllText(configPath, """
            {
              "parallel": true,
              "unknownProperty": "value",
              "anotherUnknown": 123
            }
            """);

        var result = ConfigLoader.Load(_tempDir);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Config!.Parallel).IsEqualTo(true);
    }

    [Test]
    public async Task Load_SupportsCaseInsensitivePropertyNames()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        File.WriteAllText(configPath, """
            {
              "Parallel": true,
              "BAIL": true,
              "NoCache": true
            }
            """);

        var result = ConfigLoader.Load(_tempDir);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Config!.Parallel).IsEqualTo(true);
        await Assert.That(result.Config.Bail).IsEqualTo(true);
        await Assert.That(result.Config.NoCache).IsEqualTo(true);
    }

    [Test]
    public async Task Load_SupportsJsonComments()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        File.WriteAllText(configPath, """
            {
              // This is a comment
              "parallel": true,
              /* Block comment */
              "bail": true
            }
            """);

        var result = ConfigLoader.Load(_tempDir);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Config!.Parallel).IsEqualTo(true);
    }

    [Test]
    public async Task Load_SupportsTrailingCommas()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        File.WriteAllText(configPath, """
            {
              "parallel": true,
              "bail": true,
            }
            """);

        var result = ConfigLoader.Load(_tempDir);

        await Assert.That(result.Success).IsTrue();
    }

    #endregion

    #region ConfigLoader.FindConfigFile Tests

    [Test]
    public async Task FindConfigFile_ReturnsNull_WhenNotFound()
    {
        var result = ConfigLoader.FindConfigFile(_tempDir);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task FindConfigFile_ReturnsPath_WhenFound()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        File.WriteAllText(configPath, "{}");

        var result = ConfigLoader.FindConfigFile(_tempDir);

        await Assert.That(result).IsEqualTo(configPath);
    }

    [Test]
    public async Task FindConfigFile_WorksWithFilePath()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        File.WriteAllText(configPath, "{}");
        var specFile = Path.Combine(_tempDir, "test.spec.csx");
        File.WriteAllText(specFile, "");

        var result = ConfigLoader.FindConfigFile(specFile);

        await Assert.That(result).IsEqualTo(configPath);
    }

    #endregion

    #region CliOptions.ApplyDefaults Tests

    [Test]
    public async Task ApplyDefaults_AppliesConfigValues()
    {
        var options = new CliOptions();
        var config = new DraftSpecProjectConfig
        {
            Parallel = true,
            Bail = true,
            NoCache = true,
            Format = "json"
        };

        options.ApplyDefaults(config);

        await Assert.That(options.Parallel).IsTrue();
        await Assert.That(options.Bail).IsTrue();
        await Assert.That(options.NoCache).IsTrue();
        await Assert.That(options.Format).IsEqualTo("json");
    }

    [Test]
    public async Task ApplyDefaults_DoesNotOverrideExplicitCliOptions()
    {
        var options = new CliOptions { Parallel = false };
        options.ExplicitlySet.Add(nameof(CliOptions.Parallel));

        var config = new DraftSpecProjectConfig { Parallel = true };

        options.ApplyDefaults(config);

        // CLI value should be preserved
        await Assert.That(options.Parallel).IsFalse();
    }

    [Test]
    public async Task ApplyDefaults_AppliesTagFilters()
    {
        var options = new CliOptions();
        var config = new DraftSpecProjectConfig
        {
            Tags = new TagsConfig
            {
                Include = ["unit", "fast"],
                Exclude = ["slow"]
            }
        };

        options.ApplyDefaults(config);

        await Assert.That(options.FilterTags).IsEqualTo("unit,fast");
        await Assert.That(options.ExcludeTags).IsEqualTo("slow");
    }

    [Test]
    public async Task ApplyDefaults_AppliesReporters()
    {
        var options = new CliOptions();
        var config = new DraftSpecProjectConfig
        {
            Reporters = ["console", "json"]
        };

        options.ApplyDefaults(config);

        await Assert.That(options.Reporters).IsEqualTo("console,json");
    }

    [Test]
    public async Task ApplyDefaults_DoesNotApplyNullValues()
    {
        var options = new CliOptions { Format = "console" };
        var config = new DraftSpecProjectConfig { Format = null };

        options.ApplyDefaults(config);

        await Assert.That(options.Format).IsEqualTo("console");
    }

    #endregion

    #region CliOptionsParser ExplicitlySet Tests

    [Test]
    public async Task Parser_TracksExplicitParallelFlag()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--parallel"]);

        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.Parallel));
    }

    [Test]
    public async Task Parser_TracksExplicitBailFlag()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-b"]);

        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.Bail));
    }

    [Test]
    public async Task Parser_TracksExplicitFormatFlag()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-f", "json"]);

        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.Format));
    }

    [Test]
    public async Task Parser_DoesNotTrackUnspecifiedFlags()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.ExplicitlySet).IsEmpty();
    }

    #endregion
}
