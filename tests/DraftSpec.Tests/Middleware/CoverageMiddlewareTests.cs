using DraftSpec.Coverage;
using DraftSpec.Middleware;

namespace DraftSpec.Tests.Middleware;

/// <summary>
/// Tests for CoverageMiddleware.
/// </summary>
public class CoverageMiddlewareTests
{
    #region Basic Functionality

    [Test]
    public async Task ExecuteAsync_WhenTrackerActive_AttachesCoverageInfo()
    {
        // Arrange
        var tracker = new InProcessCoverageTracker();
        tracker.Start();
        var middleware = new CoverageMiddleware(tracker);

        var spec = CreateSpec("test spec");
        var context = CreateContext(spec);

        // Simulate some coverage
        tracker.RecordLineHit("test.cs", 1);

        // Act
        var result = await middleware.ExecuteAsync(context, async ctx =>
        {
            // Simulate spec execution with more coverage
            tracker.RecordLineHit("test.cs", 2);
            tracker.RecordLineHit("test.cs", 3);
            return new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath);
        });

        // Assert
        await Assert.That(result.CoverageInfo).IsNotNull();
        await Assert.That(result.CoverageInfo!.SpecId).IsEqualTo("test spec");
        await Assert.That(result.CoverageInfo.Summary.LinesCovered).IsEqualTo(2); // Lines 2 and 3 (delta)
    }

    [Test]
    public async Task ExecuteAsync_WhenTrackerInactive_NoCoverageInfo()
    {
        // Arrange
        var tracker = new InProcessCoverageTracker();
        // Note: tracker is NOT started
        var middleware = new CoverageMiddleware(tracker);

        var spec = CreateSpec("test spec");
        var context = CreateContext(spec);

        // Act
        var result = await middleware.ExecuteAsync(context, async ctx =>
            new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath));

        // Assert
        await Assert.That(result.CoverageInfo).IsNull();
    }

    [Test]
    public async Task ExecuteAsync_StoresCoverageDataInContext()
    {
        // Arrange
        var tracker = new InProcessCoverageTracker();
        tracker.Start();
        var middleware = new CoverageMiddleware(tracker);

        var spec = CreateSpec("test spec");
        var context = CreateContext(spec);

        // Act
        await middleware.ExecuteAsync(context, async ctx =>
        {
            tracker.RecordLineHit("test.cs", 1);
            return new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath);
        });

        // Assert - use type-safe extension method
        var coverageData = context.GetCoverageData();
        await Assert.That(coverageData).IsNotNull();
        await Assert.That(coverageData!.SpecId).IsEqualTo("test spec");
    }

    [Test]
    public async Task ExecuteAsync_UpdatesCoverageIndex()
    {
        // Arrange
        var tracker = new InProcessCoverageTracker();
        tracker.Start();
        var index = new CoverageIndex();
        var middleware = new CoverageMiddleware(tracker, index);

        var spec = CreateSpec("test spec");
        var context = CreateContext(spec);

        // Act
        await middleware.ExecuteAsync(context, async ctx =>
        {
            tracker.RecordLineHit("test.cs", 10);
            tracker.RecordLineHit("test.cs", 20);
            return new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath);
        });

        // Assert
        var specs = index.GetSpecsForLine("test.cs", 10);
        await Assert.That(specs).Contains("test spec");
    }

    #endregion

    #region Context Path Handling

    [Test]
    public async Task ExecuteAsync_IncludesContextPathInSpecId()
    {
        // Arrange
        var tracker = new InProcessCoverageTracker();
        tracker.Start();
        var middleware = new CoverageMiddleware(tracker);

        var spec = CreateSpec("adds numbers");
        var context = CreateContext(spec, ["Calculator", "addition"]);

        // Act
        var result = await middleware.ExecuteAsync(context, async ctx =>
        {
            tracker.RecordLineHit("test.cs", 1);
            return new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath);
        });

        // Assert
        await Assert.That(result.CoverageInfo!.SpecId).IsEqualTo("Calculator addition adds numbers");
    }

    #endregion

    #region Helper Methods

    private static SpecDefinition CreateSpec(string description)
    {
        return new SpecDefinition(description, () => Task.CompletedTask);
    }

    private static SpecExecutionContext CreateContext(SpecDefinition spec, string[]? contextPath = null)
    {
        return new SpecExecutionContext
        {
            Spec = spec,
            Context = new SpecContext("test"),
            ContextPath = contextPath ?? [],
            HasFocused = false
        };
    }

    #endregion
}
