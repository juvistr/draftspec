using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Tests.Cli.Options;

/// <summary>
/// Tests for command-specific option classes.
/// </summary>
public class CommandOptionsTests
{
    #region RunOptions

    [Test]
    public async Task RunOptions_DefaultValues()
    {
        var options = new RunOptions();

        await Assert.That(options.Path).IsEqualTo(".");
        await Assert.That(options.Format).IsEqualTo(OutputFormat.Console);
        await Assert.That(options.OutputFile).IsNull();
        await Assert.That(options.CssUrl).IsNull();
        await Assert.That(options.Parallel).IsFalse();
        await Assert.That(options.NoCache).IsFalse();
        await Assert.That(options.Bail).IsFalse();
        await Assert.That(options.NoStats).IsFalse();
        await Assert.That(options.StatsOnly).IsFalse();
        await Assert.That(options.Reporters).IsNull();
    }

    [Test]
    public async Task RunOptions_ComposedFilterOptions_Initialized()
    {
        var options = new RunOptions();

        await Assert.That(options.Filter).IsNotNull();
        await Assert.That(options.Filter.HasActiveFilters).IsFalse();
    }

    [Test]
    public async Task RunOptions_ComposedCoverageOptions_Initialized()
    {
        var options = new RunOptions();

        await Assert.That(options.Coverage).IsNotNull();
        await Assert.That(options.Coverage.Enabled).IsFalse();
        await Assert.That(options.Coverage.Format).IsEqualTo(CoverageFormat.Cobertura);
    }

    [Test]
    public async Task RunOptions_ComposedPartitionOptions_Initialized()
    {
        var options = new RunOptions();

        await Assert.That(options.Partition).IsNotNull();
        await Assert.That(options.Partition.IsEnabled).IsFalse();
    }

    [Test]
    public async Task RunOptions_AllPropertiesSet()
    {
        var options = new RunOptions
        {
            Path = "./specs",
            Format = OutputFormat.Html,
            OutputFile = "results.html",
            CssUrl = "https://example.com/style.css",
            Parallel = true,
            NoCache = true,
            Bail = true,
            NoStats = true,
            StatsOnly = false,
            Reporters = "json,file"
        };

        await Assert.That(options.Path).IsEqualTo("./specs");
        await Assert.That(options.Format).IsEqualTo(OutputFormat.Html);
        await Assert.That(options.OutputFile).IsEqualTo("results.html");
        await Assert.That(options.CssUrl).IsEqualTo("https://example.com/style.css");
        await Assert.That(options.Parallel).IsTrue();
        await Assert.That(options.NoCache).IsTrue();
        await Assert.That(options.Bail).IsTrue();
        await Assert.That(options.NoStats).IsTrue();
        await Assert.That(options.StatsOnly).IsFalse();
        await Assert.That(options.Reporters).IsEqualTo("json,file");
    }

    [Test]
    public async Task RunOptions_ComposedOptions_CanBeConfigured()
    {
        var options = new RunOptions
        {
            Filter = new FilterOptions { FilterTags = "unit" },
            Coverage = new CoverageOptions { Enabled = true },
            Partition = new PartitionOptions { Total = 4, Index = 0 }
        };

        await Assert.That(options.Filter.HasActiveFilters).IsTrue();
        await Assert.That(options.Coverage.Enabled).IsTrue();
        await Assert.That(options.Partition.IsEnabled).IsTrue();
    }

    #endregion

    #region ListOptions

    [Test]
    public async Task ListOptions_DefaultValues()
    {
        var options = new ListOptions();

        await Assert.That(options.Path).IsEqualTo(".");
        await Assert.That(options.Format).IsEqualTo(ListFormat.Tree);
        await Assert.That(options.ShowLineNumbers).IsTrue();
        await Assert.That(options.FocusedOnly).IsFalse();
        await Assert.That(options.PendingOnly).IsFalse();
        await Assert.That(options.SkippedOnly).IsFalse();
    }

    [Test]
    public async Task ListOptions_ComposedFilterOptions_Initialized()
    {
        var options = new ListOptions();

        await Assert.That(options.Filter).IsNotNull();
        await Assert.That(options.Filter.HasActiveFilters).IsFalse();
    }

    [Test]
    public async Task ListOptions_AllFormats()
    {
        var treeOptions = new ListOptions { Format = ListFormat.Tree };
        var flatOptions = new ListOptions { Format = ListFormat.Flat };
        var jsonOptions = new ListOptions { Format = ListFormat.Json };

        await Assert.That(treeOptions.Format).IsEqualTo(ListFormat.Tree);
        await Assert.That(flatOptions.Format).IsEqualTo(ListFormat.Flat);
        await Assert.That(jsonOptions.Format).IsEqualTo(ListFormat.Json);
    }

    [Test]
    public async Task ListOptions_FilterFlags_CanBeSet()
    {
        var options = new ListOptions
        {
            FocusedOnly = true,
            PendingOnly = true,
            SkippedOnly = true
        };

        await Assert.That(options.FocusedOnly).IsTrue();
        await Assert.That(options.PendingOnly).IsTrue();
        await Assert.That(options.SkippedOnly).IsTrue();
    }

    [Test]
    public async Task ListOptions_ShowLineNumbers_CanBeDisabled()
    {
        var options = new ListOptions { ShowLineNumbers = false };
        await Assert.That(options.ShowLineNumbers).IsFalse();
    }

    #endregion

    #region ValidateOptions

    [Test]
    public async Task ValidateOptions_DefaultValues()
    {
        var options = new ValidateOptions();

        await Assert.That(options.Path).IsEqualTo(".");
        await Assert.That(options.Static).IsFalse();
        await Assert.That(options.Strict).IsFalse();
        await Assert.That(options.Quiet).IsFalse();
        Assert.Null(options.Files);
    }

    [Test]
    public async Task ValidateOptions_AllFlagsSet()
    {
        var options = new ValidateOptions
        {
            Static = true,
            Strict = true,
            Quiet = true
        };

        await Assert.That(options.Static).IsTrue();
        await Assert.That(options.Strict).IsTrue();
        await Assert.That(options.Quiet).IsTrue();
    }

    [Test]
    public async Task ValidateOptions_FilesCanBeSet()
    {
        var options = new ValidateOptions
        {
            Files = ["file1.spec.csx", "file2.spec.csx"]
        };

        await Assert.That(options.Files).IsNotNull();
        await Assert.That(options.Files).Count().IsEqualTo(2);
        await Assert.That(options.Files).Contains("file1.spec.csx");
        await Assert.That(options.Files).Contains("file2.spec.csx");
    }

    [Test]
    public async Task ValidateOptions_EmptyFiles()
    {
        var options = new ValidateOptions { Files = [] };
        await Assert.That(options.Files).IsNotNull();
        await Assert.That(options.Files).IsEmpty();
    }

    #endregion

    #region WatchOptions

    [Test]
    public async Task WatchOptions_DefaultValues()
    {
        var options = new WatchOptions();

        await Assert.That(options.Path).IsEqualTo(".");
        await Assert.That(options.Format).IsEqualTo(OutputFormat.Console);
        await Assert.That(options.Incremental).IsFalse();
        await Assert.That(options.Parallel).IsFalse();
        await Assert.That(options.NoCache).IsFalse();
        await Assert.That(options.Bail).IsFalse();
    }

    [Test]
    public async Task WatchOptions_ComposedFilterOptions_Initialized()
    {
        var options = new WatchOptions();

        await Assert.That(options.Filter).IsNotNull();
        await Assert.That(options.Filter.HasActiveFilters).IsFalse();
    }

    [Test]
    public async Task WatchOptions_Incremental_CanBeEnabled()
    {
        var options = new WatchOptions { Incremental = true };
        await Assert.That(options.Incremental).IsTrue();
    }

    [Test]
    public async Task WatchOptions_RunOptions_CanBeSet()
    {
        var options = new WatchOptions
        {
            Parallel = true,
            NoCache = true,
            Bail = true
        };

        await Assert.That(options.Parallel).IsTrue();
        await Assert.That(options.NoCache).IsTrue();
        await Assert.That(options.Bail).IsTrue();
    }

    [Test]
    public async Task WatchOptions_AllPropertiesSet()
    {
        var options = new WatchOptions
        {
            Path = "./specs",
            Format = OutputFormat.Json,
            Incremental = true,
            Parallel = true,
            NoCache = true,
            Bail = true,
            Filter = new FilterOptions { FilterTags = "fast" }
        };

        await Assert.That(options.Path).IsEqualTo("./specs");
        await Assert.That(options.Format).IsEqualTo(OutputFormat.Json);
        await Assert.That(options.Incremental).IsTrue();
        await Assert.That(options.Parallel).IsTrue();
        await Assert.That(options.NoCache).IsTrue();
        await Assert.That(options.Bail).IsTrue();
        await Assert.That(options.Filter.HasActiveFilters).IsTrue();
    }

    #endregion
}
