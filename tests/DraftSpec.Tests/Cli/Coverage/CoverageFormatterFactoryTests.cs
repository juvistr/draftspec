using DraftSpec.Cli.Coverage;

namespace DraftSpec.Tests.Cli.Coverage;

/// <summary>
/// Tests for CoverageFormatterFactory.
/// </summary>
public class CoverageFormatterFactoryTests
{
    [Test]
    public async Task GetFormatter_Html_ReturnsCoverageHtmlFormatter()
    {
        var factory = new CoverageFormatterFactory();

        var formatter = factory.GetFormatter("html");

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<CoverageHtmlFormatter>();
    }

    [Test]
    public async Task GetFormatter_Json_ReturnsCoverageJsonFormatter()
    {
        var factory = new CoverageFormatterFactory();

        var formatter = factory.GetFormatter("json");

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<CoverageJsonFormatter>();
    }

    [Test]
    public async Task GetFormatter_HtmlUppercase_ReturnsCoverageHtmlFormatter()
    {
        var factory = new CoverageFormatterFactory();

        var formatter = factory.GetFormatter("HTML");

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<CoverageHtmlFormatter>();
    }

    [Test]
    public async Task GetFormatter_JsonUppercase_ReturnsCoverageJsonFormatter()
    {
        var factory = new CoverageFormatterFactory();

        var formatter = factory.GetFormatter("JSON");

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<CoverageJsonFormatter>();
    }

    [Test]
    public async Task GetFormatter_MixedCase_ReturnsCoverageHtmlFormatter()
    {
        var factory = new CoverageFormatterFactory();

        var formatter = factory.GetFormatter("HtMl");

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<CoverageHtmlFormatter>();
    }

    [Test]
    public async Task GetFormatter_UnknownFormat_ReturnsNull()
    {
        var factory = new CoverageFormatterFactory();

        var formatter = factory.GetFormatter("unknown");

        await Assert.That(formatter).IsNull();
    }

    [Test]
    public async Task GetFormatter_EmptyString_ReturnsNull()
    {
        var factory = new CoverageFormatterFactory();

        var formatter = factory.GetFormatter("");

        await Assert.That(formatter).IsNull();
    }

    [Test]
    public async Task GetFormatter_Xml_ReturnsNull()
    {
        var factory = new CoverageFormatterFactory();

        var formatter = factory.GetFormatter("xml");

        await Assert.That(formatter).IsNull();
    }
}
