using System.Text.RegularExpressions;
using DraftSpec.Middleware;

namespace DraftSpec.Tests.Middleware;

public class FilterMiddlewareTests
{
    #region Basic Filtering

    [Test]
    public async Task Execute_WhenPredicateReturnsTrue_RunsSpec()
    {
        var middleware = new FilterMiddleware(_ => true);
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);
        var nextCalled = false;

        var result = await middleware.ExecuteAsync(context, ctx =>
        {
            nextCalled = true;
            return Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath));
        });

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task Execute_WhenPredicateReturnsFalse_SkipsSpec()
    {
        var middleware = new FilterMiddleware(_ => false);
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);
        var nextCalled = false;

        var result = await middleware.ExecuteAsync(context, ctx =>
        {
            nextCalled = true;
            return Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath));
        });

        await Assert.That(nextCalled).IsFalse();
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task Constructor_WithNullPredicate_Throws()
    {
        var action = () => new FilterMiddleware(null!);

        await Assert.That(action).Throws<ArgumentNullException>();
    }

    #endregion

    #region Name Pattern Filtering

    [Test]
    public async Task WithNameFilter_MatchingPattern_RunsSpec()
    {
        var context = new SpecContext("Calculator");
        context.AddSpec(new SpecDefinition("adds numbers", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithNameFilter("add")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task WithNameFilter_NonMatchingPattern_SkipsSpec()
    {
        var context = new SpecContext("Calculator");
        context.AddSpec(new SpecDefinition("adds numbers", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithNameFilter("subtract")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithNameFilter_MatchesFullDescription()
    {
        var context = new SpecContext("Calculator");
        var child = new SpecContext("addition", context);
        child.AddSpec(new SpecDefinition("adds two numbers", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithNameFilter("Calculator.*addition.*two")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task WithNameFilter_CaseInsensitiveByDefault()
    {
        var context = new SpecContext("Calculator");
        context.AddSpec(new SpecDefinition("Adds Numbers", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithNameFilter("adds numbers")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    #endregion

    #region ReDoS Protection

    [Test]
    [Timeout(5000)] // Test should complete well under 5 seconds
    public async Task WithNameFilter_MaliciousPattern_ThrowsTimeoutException(CancellationToken ct)
    {
        // This pattern causes catastrophic backtracking on non-matching input
        // Without timeout protection, it would hang for hours/days
        var evilPattern = "(a+)+$";
        var evilInput = new string('a', 30) + "!"; // 30 'a's followed by '!'

        var context = new SpecContext(evilInput);
        context.AddSpec(new SpecDefinition("test", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithNameFilter(evilPattern)
            .Build();

        // The regex should timeout and throw RegexMatchTimeoutException
        var action = () => runner.Run(context);
        await Assert.That(action).Throws<RegexMatchTimeoutException>();
    }

    [Test]
    [Timeout(5000)]
    public async Task WithNameExcludeFilter_MaliciousPattern_ThrowsTimeoutException(CancellationToken ct)
    {
        var evilPattern = "(a+)+$";
        var evilInput = new string('a', 30) + "!";

        var context = new SpecContext(evilInput);
        context.AddSpec(new SpecDefinition("test", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithNameExcludeFilter(evilPattern)
            .Build();

        var action = () => runner.Run(context);
        await Assert.That(action).Throws<RegexMatchTimeoutException>();
    }

    #endregion

    #region Name Pattern Exclusion

    [Test]
    public async Task WithNameExcludeFilter_MatchingPattern_SkipsSpec()
    {
        var context = new SpecContext("Calculator");
        context.AddSpec(new SpecDefinition("adds numbers slowly", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithNameExcludeFilter("slow")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithNameExcludeFilter_NonMatchingPattern_RunsSpec()
    {
        var context = new SpecContext("Calculator");
        context.AddSpec(new SpecDefinition("adds numbers", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithNameExcludeFilter("slow")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task WithNameExcludeFilter_MatchesFullDescription()
    {
        var context = new SpecContext("Calculator");
        var child = new SpecContext("deprecated", context);
        child.AddSpec(new SpecDefinition("old add method", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithNameExcludeFilter("deprecated")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithNameExcludeFilter_CaseInsensitiveByDefault()
    {
        var context = new SpecContext("Calculator");
        context.AddSpec(new SpecDefinition("SLOW operation", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithNameExcludeFilter("slow")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithNameExcludeFilter_RegexPattern_Works()
    {
        var context = new SpecContext("Calculator");
        context.AddSpec(new SpecDefinition("test_001_slow", () => { }));
        context.AddSpec(new SpecDefinition("test_002_fast", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithNameExcludeFilter(".*_001_.*")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Skipped);
        await Assert.That(results[1].Status).IsEqualTo(SpecStatus.Passed);
    }

    #endregion

    #region Tag Filtering

    [Test]
    public async Task WithTagFilter_MatchingTag_RunsSpec()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("tagged spec", () => { }) { Tags = ["slow"] });

        var runner = new SpecRunnerBuilder()
            .WithTagFilter("slow")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task WithTagFilter_NonMatchingTag_SkipsSpec()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("tagged spec", () => { }) { Tags = ["fast"] });

        var runner = new SpecRunnerBuilder()
            .WithTagFilter("slow")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithTagFilter_NoTags_SkipsSpec()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("untagged spec", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithTagFilter("slow")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithTagFilter_AnyTagMatches_RunsSpec()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("tagged spec", () => { }) { Tags = ["integration"] });

        var runner = new SpecRunnerBuilder()
            .WithTagFilter("slow", "integration")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task WithTagFilter_CaseInsensitive()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("tagged spec", () => { }) { Tags = ["SLOW"] });

        var runner = new SpecRunnerBuilder()
            .WithTagFilter("slow")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task WithTagFilter_EmptyTags_Throws()
    {
        var action = () => new SpecRunnerBuilder().WithTagFilter();

        await Assert.That(action).Throws<ArgumentException>();
    }

    #endregion

    #region Tag Exclusion

    [Test]
    public async Task WithoutTags_MatchingTag_SkipsSpec()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("tagged spec", () => { }) { Tags = ["slow"] });

        var runner = new SpecRunnerBuilder()
            .WithoutTags("slow")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithoutTags_NonMatchingTag_RunsSpec()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("tagged spec", () => { }) { Tags = ["fast"] });

        var runner = new SpecRunnerBuilder()
            .WithoutTags("slow")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task WithoutTags_NoTags_RunsSpec()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("untagged spec", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithoutTags("slow")
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    #endregion

    #region Custom Predicate

    [Test]
    public async Task WithFilter_CustomPredicate_Works()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec one", () => { }));
        context.AddSpec(new SpecDefinition("spec two", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithFilter(ctx => ctx.Spec.Description.Contains("one"))
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(results[1].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithFilter_AccessesContextPath()
    {
        var context = new SpecContext("Calculator");
        var child = new SpecContext("math", context);
        child.AddSpec(new SpecDefinition("test", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithFilter(ctx => ctx.ContextPath.Contains("math"))
            .Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    #endregion

    private static SpecExecutionContext CreateContext(SpecDefinition spec)
    {
        var specContext = new SpecContext("test");
        return new SpecExecutionContext
        {
            Spec = spec,
            Context = specContext,
            ContextPath = ["test"],
            HasFocused = false
        };
    }
}