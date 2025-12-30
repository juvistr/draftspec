using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Tests.Cli.Options;

/// <summary>
/// Tests for composable option classes.
/// </summary>
public class ComposableOptionsTests
{
    #region FilterOptions

    [Test]
    public async Task FilterOptions_DefaultValues_NoActiveFilters()
    {
        var options = new FilterOptions();

        await Assert.That(options.HasActiveFilters).IsFalse();
        await Assert.That(options.SpecName).IsNull();
        await Assert.That(options.FilterTags).IsNull();
        await Assert.That(options.ExcludeTags).IsNull();
        await Assert.That(options.FilterName).IsNull();
        await Assert.That(options.ExcludeName).IsNull();
        await Assert.That(options.FilterContext).IsNull();
        await Assert.That(options.ExcludeContext).IsNull();
        await Assert.That(options.LineFilters).IsNull();
    }

    [Test]
    public async Task FilterOptions_SpecName_HasActiveFilters()
    {
        var options = new FilterOptions { SpecName = "my-spec" };
        await Assert.That(options.HasActiveFilters).IsTrue();
    }

    [Test]
    public async Task FilterOptions_FilterTags_HasActiveFilters()
    {
        var options = new FilterOptions { FilterTags = "unit,fast" };
        await Assert.That(options.HasActiveFilters).IsTrue();
    }

    [Test]
    public async Task FilterOptions_ExcludeTags_HasActiveFilters()
    {
        var options = new FilterOptions { ExcludeTags = "slow" };
        await Assert.That(options.HasActiveFilters).IsTrue();
    }

    [Test]
    public async Task FilterOptions_FilterName_HasActiveFilters()
    {
        var options = new FilterOptions { FilterName = ".*Create.*" };
        await Assert.That(options.HasActiveFilters).IsTrue();
    }

    [Test]
    public async Task FilterOptions_ExcludeName_HasActiveFilters()
    {
        var options = new FilterOptions { ExcludeName = ".*Legacy.*" };
        await Assert.That(options.HasActiveFilters).IsTrue();
    }

    [Test]
    public async Task FilterOptions_FilterContext_HasActiveFilters()
    {
        var options = new FilterOptions { FilterContext = ["UserService/*"] };
        await Assert.That(options.HasActiveFilters).IsTrue();
    }

    [Test]
    public async Task FilterOptions_ExcludeContext_HasActiveFilters()
    {
        var options = new FilterOptions { ExcludeContext = ["Legacy/**"] };
        await Assert.That(options.HasActiveFilters).IsTrue();
    }

    [Test]
    public async Task FilterOptions_LineFilters_HasActiveFilters()
    {
        var options = new FilterOptions { LineFilters = [new LineFilter("test.spec.csx", [10, 20])] };
        await Assert.That(options.HasActiveFilters).IsTrue();
    }

    [Test]
    public async Task FilterOptions_EmptyCollections_NoActiveFilters()
    {
        var options = new FilterOptions
        {
            FilterContext = [],
            ExcludeContext = [],
            LineFilters = []
        };
        await Assert.That(options.HasActiveFilters).IsFalse();
    }

    #endregion

    #region CoverageOptions

    [Test]
    public async Task CoverageOptions_DefaultValues()
    {
        var options = new CoverageOptions();

        await Assert.That(options.Enabled).IsFalse();
        await Assert.That(options.Output).IsNull();
        await Assert.That(options.Format).IsEqualTo(CoverageFormat.Cobertura);
        await Assert.That(options.ReportFormats).IsNull();
        await Assert.That(options.OutputDirectory).IsEqualTo("./coverage");
    }

    [Test]
    public async Task CoverageOptions_CustomOutput_ReturnsCustomDirectory()
    {
        var options = new CoverageOptions { Output = "./my-coverage" };
        await Assert.That(options.OutputDirectory).IsEqualTo("./my-coverage");
    }

    [Test]
    public async Task CoverageOptions_AllPropertiesSet()
    {
        var options = new CoverageOptions
        {
            Enabled = true,
            Output = "./reports",
            Format = CoverageFormat.Xml,
            ReportFormats = "html,json"
        };

        await Assert.That(options.Enabled).IsTrue();
        await Assert.That(options.Output).IsEqualTo("./reports");
        await Assert.That(options.Format).IsEqualTo(CoverageFormat.Xml);
        await Assert.That(options.ReportFormats).IsEqualTo("html,json");
        await Assert.That(options.OutputDirectory).IsEqualTo("./reports");
    }

    #endregion

    #region PartitionOptions

    [Test]
    public async Task PartitionOptions_DefaultValues()
    {
        var options = new PartitionOptions();

        await Assert.That(options.Total).IsNull();
        await Assert.That(options.Index).IsNull();
        await Assert.That(options.Strategy).IsEqualTo(PartitionStrategy.File);
        await Assert.That(options.IsEnabled).IsFalse();
        await Assert.That(options.Validate()).IsNull();
    }

    [Test]
    public async Task PartitionOptions_BothSet_IsEnabled()
    {
        var options = new PartitionOptions { Total = 4, Index = 0 };

        await Assert.That(options.IsEnabled).IsTrue();
        await Assert.That(options.Validate()).IsNull();
    }

    [Test]
    public async Task PartitionOptions_OnlyTotal_ValidationError()
    {
        var options = new PartitionOptions { Total = 4 };

        await Assert.That(options.IsEnabled).IsFalse();
        await Assert.That(options.Validate()).IsEqualTo("--partition requires --partition-index");
    }

    [Test]
    public async Task PartitionOptions_OnlyIndex_ValidationError()
    {
        var options = new PartitionOptions { Index = 0 };

        await Assert.That(options.IsEnabled).IsFalse();
        await Assert.That(options.Validate()).IsEqualTo("--partition-index requires --partition");
    }

    [Test]
    public async Task PartitionOptions_TotalLessThanOne_ValidationError()
    {
        var options = new PartitionOptions { Total = 0, Index = 0 };

        await Assert.That(options.Validate()).IsEqualTo("--partition must be at least 1");
    }

    [Test]
    public async Task PartitionOptions_IndexNegative_ValidationError()
    {
        var options = new PartitionOptions { Total = 4, Index = -1 };

        await Assert.That(options.Validate()).IsEqualTo("--partition-index must be at least 0");
    }

    [Test]
    public async Task PartitionOptions_IndexExceedsTotal_ValidationError()
    {
        var options = new PartitionOptions { Total = 4, Index = 4 };

        await Assert.That(options.Validate()).IsEqualTo("--partition-index must be less than --partition (4)");
    }

    [Test]
    public async Task PartitionOptions_ValidBoundaryIndex_NoError()
    {
        var options = new PartitionOptions { Total = 4, Index = 3 };

        await Assert.That(options.IsEnabled).IsTrue();
        await Assert.That(options.Validate()).IsNull();
    }

    [Test]
    public async Task PartitionOptions_SpecCountStrategy()
    {
        var options = new PartitionOptions
        {
            Total = 2,
            Index = 1,
            Strategy = PartitionStrategy.SpecCount
        };

        await Assert.That(options.Strategy).IsEqualTo(PartitionStrategy.SpecCount);
        await Assert.That(options.Validate()).IsNull();
    }

    #endregion
}
