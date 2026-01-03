using DraftSpec.Cli.Services;

namespace DraftSpec.Tests.Cli.Services;

/// <summary>
/// Tests for <see cref="StaticSpecParserFactory"/>.
/// </summary>
public class StaticSpecParserFactoryTests
{
    [Test]
    public async Task Create_WithBaseDirectory_ReturnsParser()
    {
        var factory = new StaticSpecParserFactory();

        var parser = factory.Create("/some/directory");

        await Assert.That(parser).IsNotNull();
        await Assert.That(parser).IsAssignableTo<IStaticSpecParser>();
    }

    [Test]
    public async Task Create_WithUseCacheTrue_ReturnsParser()
    {
        var factory = new StaticSpecParserFactory();

        var parser = factory.Create("/some/directory", useCache: true);

        await Assert.That(parser).IsNotNull();
    }

    [Test]
    public async Task Create_WithUseCacheFalse_ReturnsParser()
    {
        var factory = new StaticSpecParserFactory();

        var parser = factory.Create("/some/directory", useCache: false);

        await Assert.That(parser).IsNotNull();
    }

    [Test]
    public async Task Create_ReturnsNewInstanceEachTime()
    {
        var factory = new StaticSpecParserFactory();

        var parser1 = factory.Create("/dir1");
        var parser2 = factory.Create("/dir2");

        await Assert.That(parser1).IsNotSameReferenceAs(parser2);
    }

    [Test]
    public async Task Create_DefaultUseCacheIsTrue_ReturnsParser()
    {
        var factory = new StaticSpecParserFactory();

        // Call without explicit useCache parameter - should default to true
        var parser = factory.Create("/some/directory");

        await Assert.That(parser).IsNotNull();
    }
}
