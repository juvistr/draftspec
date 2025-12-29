using DraftSpec.Cli;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Html;
using DraftSpec.Formatters.JUnit;
using DraftSpec.Formatters.Markdown;

namespace DraftSpec.Tests.Cli.DependencyInjection;

/// <summary>
/// Tests for CliFormatterRegistry class.
/// </summary>
public class CliFormatterRegistryTests
{
    #region GetFormatter Built-in Formatters

    [Test]
    public async Task GetFormatter_Json_ReturnsJsonFormatter()
    {
        var registry = new CliFormatterRegistry();

        var formatter = registry.GetFormatter(OutputFormats.Json);

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<JsonFormatter>();
    }

    [Test]
    public async Task GetFormatter_Markdown_ReturnsMarkdownFormatter()
    {
        var registry = new CliFormatterRegistry();

        var formatter = registry.GetFormatter(OutputFormats.Markdown);

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<MarkdownFormatter>();
    }

    [Test]
    public async Task GetFormatter_Html_ReturnsHtmlFormatter()
    {
        var registry = new CliFormatterRegistry();

        var formatter = registry.GetFormatter(OutputFormats.Html);

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<HtmlFormatter>();
    }

    [Test]
    public async Task GetFormatter_Junit_ReturnsJUnitFormatter()
    {
        var registry = new CliFormatterRegistry();

        var formatter = registry.GetFormatter(OutputFormats.JUnit);

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<JUnitFormatter>();
    }

    [Test]
    public async Task GetFormatter_Unknown_ReturnsNull()
    {
        var registry = new CliFormatterRegistry();

        var formatter = registry.GetFormatter("unknown-format");

        await Assert.That(formatter).IsNull();
    }

    #endregion

    #region Case Insensitivity

    [Test]
    [Arguments("JSON")]
    [Arguments("Json")]
    [Arguments("json")]
    [Arguments("jSoN")]
    public async Task GetFormatter_CaseInsensitive_ReturnsCorrectFormatter(string name)
    {
        var registry = new CliFormatterRegistry();

        var formatter = registry.GetFormatter(name);

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<JsonFormatter>();
    }

    #endregion

    #region Custom Formatter Registration

    [Test]
    public async Task Register_CustomFormatter_Succeeds()
    {
        var registry = new CliFormatterRegistry();

        // Should not throw
        registry.Register("custom", _ => new TestFormatter());

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Register_CustomFormatter_CanRetrieve()
    {
        var registry = new CliFormatterRegistry();
        var customFormatter = new TestFormatter();

        registry.Register("custom", _ => customFormatter);
        var retrieved = registry.GetFormatter("custom");

        await Assert.That(retrieved).IsSameReferenceAs(customFormatter);
    }

    [Test]
    public async Task Register_OverwritesExisting_Succeeds()
    {
        var registry = new CliFormatterRegistry();
        var newFormatter = new TestFormatter();

        // Overwrite built-in json formatter
        registry.Register(OutputFormats.Json, _ => newFormatter);
        var retrieved = registry.GetFormatter(OutputFormats.Json);

        await Assert.That(retrieved).IsSameReferenceAs(newFormatter);
    }

    #endregion

    #region Names Property

    [Test]
    public async Task Names_ReturnsAllRegisteredNames()
    {
        var registry = new CliFormatterRegistry();

        var names = registry.Names.ToList();

        // Should contain all built-in formatters
        await Assert.That(names).Contains(OutputFormats.Json);
        await Assert.That(names).Contains(OutputFormats.Markdown);
        await Assert.That(names).Contains(OutputFormats.Html);
        await Assert.That(names).Contains(OutputFormats.JUnit);
    }

    [Test]
    public async Task Names_IncludesCustomRegistrations()
    {
        var registry = new CliFormatterRegistry();

        registry.Register("custom-format", _ => new TestFormatter());
        var names = registry.Names.ToList();

        await Assert.That(names).Contains("custom-format");
    }

    [Test]
    public async Task Names_ReturnsAtLeastFourBuiltInFormatters()
    {
        var registry = new CliFormatterRegistry();

        var count = registry.Names.Count();

        await Assert.That(count).IsGreaterThanOrEqualTo(4);
    }

    #endregion

    #region HTML Formatter CSS Options

    [Test]
    public async Task Html_UsesDefaultCssUrl_WhenNotProvided()
    {
        var registry = new CliFormatterRegistry();
        var options = new CliOptions(); // No CssUrl set

        var formatter = registry.GetFormatter(OutputFormats.Html, options) as HtmlFormatter;

        await Assert.That(formatter).IsNotNull();

        // Format a report and check the output contains the default CSS URL
        var report = CreateMinimalReport();
        var output = formatter!.Format(report);

        await Assert.That(output).Contains("https://cdnjs.cloudflare.com/ajax/libs/simpledotcss/2.3.7/simple.min.css");
    }

    [Test]
    public async Task Html_UsesCustomCssUrl_WhenProvided()
    {
        var registry = new CliFormatterRegistry();
        var customCssUrl = "https://example.com/custom.css";
        var options = new CliOptions { CssUrl = customCssUrl };

        var formatter = registry.GetFormatter(OutputFormats.Html, options) as HtmlFormatter;

        await Assert.That(formatter).IsNotNull();

        // Format a report and check the output contains the custom CSS URL
        var report = CreateMinimalReport();
        var output = formatter!.Format(report);

        await Assert.That(output).Contains(customCssUrl);
        await Assert.That(output).DoesNotContain("simpledotcss");
    }

    [Test]
    public async Task Html_NullOptions_UsesDefaultCssUrl()
    {
        var registry = new CliFormatterRegistry();

        var formatter = registry.GetFormatter(OutputFormats.Html, null) as HtmlFormatter;

        await Assert.That(formatter).IsNotNull();

        var report = CreateMinimalReport();
        var output = formatter!.Format(report);

        await Assert.That(output).Contains("https://cdnjs.cloudflare.com/ajax/libs/simpledotcss/2.3.7/simple.min.css");
    }

    #endregion

    #region Factory Pattern Tests

    [Test]
    public async Task GetFormatter_ReturnsNewInstance_EachCall()
    {
        var registry = new CliFormatterRegistry();

        var formatter1 = registry.GetFormatter(OutputFormats.Json);
        var formatter2 = registry.GetFormatter(OutputFormats.Json);

        // Factory should create new instances each time
        await Assert.That(formatter1).IsNotSameReferenceAs(formatter2);
    }

    [Test]
    public async Task Register_FactoryReceivesOptions()
    {
        var registry = new CliFormatterRegistry();
        CliOptions? receivedOptions = null;

        registry.Register("test", opts =>
        {
            receivedOptions = opts;
            return new TestFormatter();
        });

        var options = new CliOptions { Path = "/test/path" };
        registry.GetFormatter("test", options);

        await Assert.That(receivedOptions).IsSameReferenceAs(options);
    }

    [Test]
    public async Task Register_FactoryReceivesNull_WhenNoOptions()
    {
        var registry = new CliFormatterRegistry();
        CliOptions? receivedOptions = new CliOptions(); // Non-null initial value

        registry.Register("test", opts =>
        {
            receivedOptions = opts;
            return new TestFormatter();
        });

        registry.GetFormatter("test"); // No options provided

        await Assert.That(receivedOptions).IsNull();
    }

    #endregion

    #region Helper Methods

    private static SpecReport CreateMinimalReport()
    {
        return new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Source = "test",
            Summary = new SpecSummary
            {
                Total = 1,
                Passed = 1,
                Failed = 0,
                Pending = 0,
                Skipped = 0
            }
        };
    }

    #endregion

    #region Test Helpers

    private class TestFormatter : IFormatter
    {
        public string Format(SpecReport report) => "test output";
        public string FileExtension => ".test";
    }

    #endregion
}
