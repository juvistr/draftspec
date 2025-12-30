using DraftSpec.Cli;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for configuration file loading and merging.
/// </summary>
public class ConfigLoaderTests
{
    private string _tempDir = null!;
    private ConfigLoader _loader = null!;
    private MockEnvironment _environment = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec-config-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _environment = new MockEnvironment { CurrentDirectory = _tempDir };
        _loader = new ConfigLoader(_environment);
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
        var result = _loader.Load(_tempDir);

        await Assert.That(result.Config).IsNull();
        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Found).IsFalse();
    }

    [Test]
    public async Task Load_LoadsConfig_WhenFileExists()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        await File.WriteAllTextAsync(configPath, """
                                                 {
                                                   "parallel": true,
                                                   "bail": true,
                                                   "timeout": 5000
                                                 }
                                                 """);

        var result = _loader.Load(_tempDir);

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
        await File.WriteAllTextAsync(configPath, """
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

        var result = _loader.Load(_tempDir);

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
        await File.WriteAllTextAsync(configPath, "{ invalid json }");

        var result = _loader.Load(_tempDir);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsNotNull();
        await Assert.That(result.Error!).Contains("Error parsing");
        await Assert.That(result.Found).IsTrue();
    }

    [Test]
    public async Task Load_ReturnsError_WhenTimeoutIsInvalid()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        await File.WriteAllTextAsync(configPath, """{ "timeout": -1 }""");

        var result = _loader.Load(_tempDir);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error!).Contains("timeout must be a positive number");
    }

    [Test]
    public async Task Load_ReturnsError_WhenMaxParallelismIsInvalid()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        await File.WriteAllTextAsync(configPath, """{ "maxParallelism": 0 }""");

        var result = _loader.Load(_tempDir);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error!).Contains("maxParallelism must be a positive number");
    }

    [Test]
    public async Task Load_IgnoresUnknownProperties()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        await File.WriteAllTextAsync(configPath, """
                                                 {
                                                   "parallel": true,
                                                   "unknownProperty": "value",
                                                   "anotherUnknown": 123
                                                 }
                                                 """);

        var result = _loader.Load(_tempDir);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Config!.Parallel).IsEqualTo(true);
    }

    [Test]
    public async Task Load_SupportsCaseInsensitivePropertyNames()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        await File.WriteAllTextAsync(configPath, """
                                                 {
                                                   "Parallel": true,
                                                   "BAIL": true,
                                                   "NoCache": true
                                                 }
                                                 """);

        var result = _loader.Load(_tempDir);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Config!.Parallel).IsEqualTo(true);
        await Assert.That(result.Config.Bail).IsEqualTo(true);
        await Assert.That(result.Config.NoCache).IsEqualTo(true);
    }

    [Test]
    public async Task Load_SupportsJsonComments()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        await File.WriteAllTextAsync(configPath, """
                                                 {
                                                   // This is a comment
                                                   "parallel": true,
                                                   /* Block comment */
                                                   "bail": true
                                                 }
                                                 """);

        var result = _loader.Load(_tempDir);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Config!.Parallel).IsEqualTo(true);
    }

    [Test]
    public async Task Load_SupportsTrailingCommas()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        await File.WriteAllTextAsync(configPath, """
                                                 {
                                                   "parallel": true,
                                                   "bail": true,
                                                 }
                                                 """);

        var result = _loader.Load(_tempDir);

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
        await File.WriteAllTextAsync(configPath, "{}");

        var result = ConfigLoader.FindConfigFile(_tempDir);

        await Assert.That(result).IsEqualTo(configPath);
    }

    [Test]
    public async Task FindConfigFile_WorksWithFilePath()
    {
        var configPath = Path.Combine(_tempDir, ConfigLoader.ConfigFileName);
        await File.WriteAllTextAsync(configPath, "{}");
        var specFile = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(specFile, "");

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
        await Assert.That(options.Format).IsEqualTo(OutputFormat.Json);
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
        var options = new CliOptions { Format = OutputFormat.Console };
        var config = new DraftSpecProjectConfig { Format = null };

        options.ApplyDefaults(config);

        await Assert.That(options.Format).IsEqualTo(OutputFormat.Console);
    }

    [Test]
    public async Task ApplyDefaults_AppliesCoverageEnabled()
    {
        var options = new CliOptions();
        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig { Enabled = true }
        };

        options.ApplyDefaults(config);

        await Assert.That(options.Coverage).IsTrue();
    }

    [Test]
    public async Task ApplyDefaults_AppliesCoverageOutput()
    {
        var options = new CliOptions();
        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig { Output = "./my-coverage" }
        };

        options.ApplyDefaults(config);

        await Assert.That(options.CoverageOutput).IsEqualTo("./my-coverage");
    }

    [Test]
    public async Task ApplyDefaults_AppliesCoverageFormat()
    {
        var options = new CliOptions();
        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig { Format = "xml" }
        };

        options.ApplyDefaults(config);

        await Assert.That(options.CoverageFormat).IsEqualTo(CoverageFormat.Xml);
    }

    [Test]
    public async Task ApplyDefaults_AppliesCoverageReportFormats()
    {
        var options = new CliOptions();
        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig { ReportFormats = ["html", "json"] }
        };

        options.ApplyDefaults(config);

        await Assert.That(options.CoverageReportFormats).IsEqualTo("html,json");
    }

    [Test]
    public async Task ApplyDefaults_DoesNotOverrideExplicitCoverageOptions()
    {
        var options = new CliOptions { Coverage = false };
        options.ExplicitlySet.Add(nameof(CliOptions.Coverage));

        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig { Enabled = true }
        };

        options.ApplyDefaults(config);

        await Assert.That(options.Coverage).IsFalse();
    }

    [Test]
    public async Task ApplyDefaults_DoesNotApplyCoverageWhenNotEnabled()
    {
        var options = new CliOptions();
        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig { Enabled = false }
        };

        options.ApplyDefaults(config);

        await Assert.That(options.Coverage).IsFalse();
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

    #region DraftSpecProjectConfig.Validate Tests

    [Test]
    public async Task Validate_ValidConfig_ReturnsEmpty()
    {
        var config = new DraftSpecProjectConfig
        {
            Timeout = 5000,
            MaxParallelism = 4,
            Parallel = true,
            Coverage = new CoverageConfig
            {
                Enabled = true,
                Thresholds = new ThresholdsConfig { Line = 80, Branch = 70 },
                Formats = ["cobertura", "html"]
            }
        };

        var errors = config.Validate();

        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task Validate_NegativeTimeout_ReturnsError()
    {
        var config = new DraftSpecProjectConfig { Timeout = -1 };

        var errors = config.Validate();

        await Assert.That(errors).Contains("timeout must be a positive number");
    }

    [Test]
    public async Task Validate_ZeroTimeout_ReturnsError()
    {
        var config = new DraftSpecProjectConfig { Timeout = 0 };

        var errors = config.Validate();

        await Assert.That(errors).Contains("timeout must be a positive number");
    }

    [Test]
    public async Task Validate_NegativeMaxParallelism_ReturnsError()
    {
        var config = new DraftSpecProjectConfig { MaxParallelism = -1 };

        var errors = config.Validate();

        await Assert.That(errors).Contains("maxParallelism must be a positive number");
    }

    [Test]
    public async Task Validate_LineThresholdTooHigh_ReturnsError()
    {
        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig
            {
                Thresholds = new ThresholdsConfig { Line = 101 }
            }
        };

        var errors = config.Validate();

        await Assert.That(errors).Contains("coverage.thresholds.line must be between 0 and 100");
    }

    [Test]
    public async Task Validate_LineThresholdNegative_ReturnsError()
    {
        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig
            {
                Thresholds = new ThresholdsConfig { Line = -1 }
            }
        };

        var errors = config.Validate();

        await Assert.That(errors).Contains("coverage.thresholds.line must be between 0 and 100");
    }

    [Test]
    public async Task Validate_BranchThresholdTooHigh_ReturnsError()
    {
        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig
            {
                Thresholds = new ThresholdsConfig { Branch = 150 }
            }
        };

        var errors = config.Validate();

        await Assert.That(errors).Contains("coverage.thresholds.branch must be between 0 and 100");
    }

    [Test]
    public async Task Validate_BranchThresholdNegative_ReturnsError()
    {
        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig
            {
                Thresholds = new ThresholdsConfig { Branch = -5 }
            }
        };

        var errors = config.Validate();

        await Assert.That(errors).Contains("coverage.thresholds.branch must be between 0 and 100");
    }

    [Test]
    public async Task Validate_UnknownCoverageFormat_ReturnsError()
    {
        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig
            {
                Formats = ["cobertura", "invalid-format"]
            }
        };

        var errors = config.Validate();

        await Assert.That(errors).Contains("Unknown coverage format: invalid-format");
    }

    [Test]
    public async Task Validate_ValidCoverageFormats_ReturnsEmpty()
    {
        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig
            {
                Formats = ["cobertura", "xml", "html", "json", "coverage"]
            }
        };

        var errors = config.Validate();

        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task Validate_CoverageFormatsCaseInsensitive_ReturnsEmpty()
    {
        var config = new DraftSpecProjectConfig
        {
            Coverage = new CoverageConfig
            {
                Formats = ["COBERTURA", "Html", "JSON"]
            }
        };

        var errors = config.Validate();

        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task Validate_MultipleErrors_ReturnsAll()
    {
        var config = new DraftSpecProjectConfig
        {
            Timeout = -1,
            MaxParallelism = 0,
            Coverage = new CoverageConfig
            {
                Thresholds = new ThresholdsConfig { Line = 200, Branch = -10 },
                Formats = ["bad-format"]
            }
        };

        var errors = config.Validate();

        await Assert.That(errors.Count).IsGreaterThanOrEqualTo(4);
    }

    #endregion
}
