using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Mcp;

/// <summary>
/// Tests for Session class.
/// </summary>
public class SessionTests
{
    [Test]
    public async Task Constructor_SetsPropertiesCorrectly()
    {
        var id = "test-session-123";
        var timeout = TimeSpan.FromMinutes(15);
        var tempDir = Path.Combine(Path.GetTempPath(), "test-session");

        using var session = new Session(id, timeout, tempDir);

        await Assert.That(session.Id).IsEqualTo(id);
        await Assert.That(session.Timeout).IsEqualTo(timeout);
        await Assert.That(session.TempDirectory).IsEqualTo(tempDir);
        await Assert.That(session.AccumulatedContent).IsEqualTo("");
        await Assert.That(session.IsExpired).IsFalse();
    }

    [Test]
    public async Task Constructor_CreatesTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");

        try
        {
            using var session = new Session("test", TimeSpan.FromMinutes(30), tempDir);

            await Assert.That(Directory.Exists(tempDir)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task AppendContent_AddsContent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");
        using var session = new Session("test", TimeSpan.FromMinutes(30), tempDir);

        session.AppendContent("describe(\"Test\", () => {});");

        await Assert.That(session.AccumulatedContent).IsEqualTo("describe(\"Test\", () => {});");
    }

    [Test]
    public async Task AppendContent_AccumulatesMultipleContent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");
        using var session = new Session("test", TimeSpan.FromMinutes(30), tempDir);

        session.AppendContent("describe(\"First\", () => {});");
        session.AppendContent("describe(\"Second\", () => {});");

        await Assert.That(session.AccumulatedContent).Contains("First");
        await Assert.That(session.AccumulatedContent).Contains("Second");
    }

    [Test]
    public async Task AppendContent_IgnoresEmptyContent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");
        using var session = new Session("test", TimeSpan.FromMinutes(30), tempDir);

        session.AppendContent("describe(\"Test\", () => {});");
        session.AppendContent("");
        session.AppendContent("  ");

        await Assert.That(session.AccumulatedContent).IsEqualTo("describe(\"Test\", () => {});");
    }

    [Test]
    public async Task GetFullContent_ReturnsNewContentWhenNoAccumulated()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");
        using var session = new Session("test", TimeSpan.FromMinutes(30), tempDir);

        var result = session.GetFullContent("new content");

        await Assert.That(result).IsEqualTo("new content");
    }

    [Test]
    public async Task GetFullContent_CombinesAccumulatedWithNew()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");
        using var session = new Session("test", TimeSpan.FromMinutes(30), tempDir);

        session.AppendContent("accumulated content");
        var result = session.GetFullContent("new content");

        await Assert.That(result).Contains("accumulated content");
        await Assert.That(result).Contains("new content");
    }

    [Test]
    public async Task ClearContent_RemovesAccumulatedContent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");
        using var session = new Session("test", TimeSpan.FromMinutes(30), tempDir);

        session.AppendContent("some content");
        session.ClearContent();

        await Assert.That(session.AccumulatedContent).IsEqualTo("");
    }

    [Test]
    public async Task Touch_UpdatesLastAccessedAt()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");
        using var session = new Session("test", TimeSpan.FromMinutes(30), tempDir);

        var originalLastAccessed = session.LastAccessedAt;
        await Task.Delay(10);
        session.Touch();

        await Assert.That(session.LastAccessedAt).IsGreaterThan(originalLastAccessed);
    }

    [Test]
    public async Task IsExpired_ReturnsFalseForActiveSession()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");
        using var session = new Session("test", TimeSpan.FromMinutes(30), tempDir);

        await Assert.That(session.IsExpired).IsFalse();
    }

    [Test]
    public async Task IsExpired_ReturnsTrueAfterTimeout()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");
        using var session = new Session("test", TimeSpan.FromMilliseconds(1), tempDir);

        await Task.Delay(50);

        await Assert.That(session.IsExpired).IsTrue();
    }

    [Test]
    public async Task Dispose_CleansTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");
        var session = new Session("test", TimeSpan.FromMinutes(30), tempDir);

        await Assert.That(Directory.Exists(tempDir)).IsTrue();

        session.Dispose();

        await Assert.That(Directory.Exists(tempDir)).IsFalse();
    }

    [Test]
    public async Task Dispose_CanBeCalledMultipleTimes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");
        var session = new Session("test", TimeSpan.FromMinutes(30), tempDir);

        session.Dispose();
        session.Dispose(); // Should not throw

        await Assert.That(true).IsTrue(); // Just verifying no exception
    }
}
