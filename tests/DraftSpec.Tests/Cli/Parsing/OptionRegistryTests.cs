using DraftSpec.Cli.Parsing;

namespace DraftSpec.Tests.Cli.Parsing;

/// <summary>
/// Tests for option registry lookup.
/// </summary>
public class OptionRegistryTests
{
    #region Basic Lookup

    [Test]
    public async Task TryGet_LongOption_ReturnsDefinition()
    {
        var found = OptionRegistry.TryGet("--format", out var definition);

        await Assert.That(found).IsTrue();
        await Assert.That(definition).IsNotNull();
        await Assert.That(definition.Names).Contains("--format");
    }

    [Test]
    public async Task TryGet_ShortOption_ReturnsDefinition()
    {
        var found = OptionRegistry.TryGet("-f", out var definition);

        await Assert.That(found).IsTrue();
        await Assert.That(definition).IsNotNull();
        await Assert.That(definition.Names).Contains("-f");
    }

    [Test]
    public async Task TryGet_UnknownOption_ReturnsFalse()
    {
        var found = OptionRegistry.TryGet("--unknown-option", out _);

        await Assert.That(found).IsFalse();
    }

    [Test]
    public async Task TryGet_HelpCommand_ReturnsDefinition()
    {
        var found = OptionRegistry.TryGet("help", out var definition);

        await Assert.That(found).IsTrue();
        await Assert.That(definition.Names).Contains("help");
    }

    #endregion

    #region All Options Registered

    [Test]
    [Arguments("--help")]
    [Arguments("-h")]
    [Arguments("help")]
    [Arguments("--format")]
    [Arguments("-f")]
    [Arguments("--output")]
    [Arguments("-o")]
    [Arguments("--css-url")]
    [Arguments("--force")]
    [Arguments("--parallel")]
    [Arguments("-p")]
    [Arguments("--no-cache")]
    [Arguments("--bail")]
    [Arguments("-b")]
    [Arguments("--filter-tags")]
    [Arguments("-t")]
    [Arguments("--exclude-tags")]
    [Arguments("-x")]
    [Arguments("--filter-name")]
    [Arguments("-n")]
    [Arguments("--exclude-name")]
    [Arguments("--context")]
    [Arguments("-c")]
    [Arguments("--exclude-context")]
    [Arguments("--coverage")]
    [Arguments("--coverage-output")]
    [Arguments("--coverage-format")]
    [Arguments("--coverage-report-formats")]
    [Arguments("--list-format")]
    [Arguments("--show-line-numbers")]
    [Arguments("--no-line-numbers")]
    [Arguments("--focused-only")]
    [Arguments("--pending-only")]
    [Arguments("--skipped-only")]
    [Arguments("--static")]
    [Arguments("--strict")]
    [Arguments("--quiet")]
    [Arguments("-q")]
    [Arguments("--files")]
    [Arguments("--no-stats")]
    [Arguments("--stats-only")]
    [Arguments("--partition")]
    [Arguments("--partition-index")]
    [Arguments("--partition-strategy")]
    [Arguments("--incremental")]
    [Arguments("-i")]
    [Arguments("--affected-by")]
    [Arguments("--dry-run")]
    [Arguments("--quarantine")]
    [Arguments("--no-history")]
    [Arguments("--interactive")]
    [Arguments("-I")]
    [Arguments("--min-changes")]
    [Arguments("--window-size")]
    [Arguments("--clear")]
    [Arguments("--percentile")]
    [Arguments("--output-seconds")]
    [Arguments("--docs-format")]
    [Arguments("--docs-context")]
    [Arguments("--with-results")]
    [Arguments("--results-file")]
    [Arguments("--gaps-only")]
    [Arguments("--specs")]
    [Arguments("--namespace")]
    [Arguments("--coverage-map-format")]
    public async Task TryGet_AllOptions_AreRegistered(string option)
    {
        var found = OptionRegistry.TryGet(option, out _);

        await Assert.That(found).IsTrue();
    }

    #endregion

    #region Short/Long Option Equivalence

    [Test]
    public async Task TryGet_ShortAndLongFormat_ReturnSameHandler()
    {
        OptionRegistry.TryGet("--format", out var longDef);
        OptionRegistry.TryGet("-f", out var shortDef);

        await Assert.That(longDef.Handler).IsEqualTo(shortDef.Handler);
    }

    [Test]
    public async Task TryGet_ShortAndLongParallel_ReturnSameHandler()
    {
        OptionRegistry.TryGet("--parallel", out var longDef);
        OptionRegistry.TryGet("-p", out var shortDef);

        await Assert.That(longDef.Handler).IsEqualTo(shortDef.Handler);
    }

    [Test]
    public async Task TryGet_ShortAndLongBail_ReturnSameHandler()
    {
        OptionRegistry.TryGet("--bail", out var longDef);
        OptionRegistry.TryGet("-b", out var shortDef);

        await Assert.That(longDef.Handler).IsEqualTo(shortDef.Handler);
    }

    #endregion
}
