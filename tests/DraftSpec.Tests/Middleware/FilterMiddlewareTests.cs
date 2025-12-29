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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

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
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    #endregion

    #region Context Filtering

    [Test]
    public async Task WithContextFilter_MatchesExactContextPath()
    {
        var root = new SpecContext("UserService");
        var child = new SpecContext("CreateAsync", root);
        child.AddSpec(new SpecDefinition("creates user", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithContextFilter("UserService/CreateAsync")
            .Build();
        var results = await runner.RunAsync(root);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task WithContextFilter_SingleWildcard_MatchesSingleSegment()
    {
        var root = new SpecContext("UserService");
        var child = new SpecContext("CreateAsync", root);
        child.AddSpec(new SpecDefinition("test", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithContextFilter("*/CreateAsync")
            .Build();
        var results = await runner.RunAsync(root);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task WithContextFilter_DoubleWildcard_MatchesMultipleSegments()
    {
        var root = new SpecContext("UserService");
        var level1 = new SpecContext("Admin", root);
        var level2 = new SpecContext("CreateAsync", level1);
        level2.AddSpec(new SpecDefinition("test", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithContextFilter("**/CreateAsync")
            .Build();
        var results = await runner.RunAsync(root);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task WithContextFilter_NoMatch_SkipsSpec()
    {
        var root = new SpecContext("UserService");
        var child = new SpecContext("CreateAsync", root);
        child.AddSpec(new SpecDefinition("test", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithContextFilter("OrderService/*")
            .Build();
        var results = await runner.RunAsync(root);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithContextFilter_MultiplePatterns_MatchesAny()
    {
        var root = new SpecContext("root");
        var user = new SpecContext("UserService", root);
        user.AddSpec(new SpecDefinition("user test", () => { }));
        var order = new SpecContext("OrderService", root);
        order.AddSpec(new SpecDefinition("order test", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithContextFilter("UserService", "OrderService")
            .Build();
        var results = await runner.RunAsync(root);

        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(2);
    }

    [Test]
    public async Task WithContextFilter_CaseInsensitive()
    {
        var root = new SpecContext("UserService");
        root.AddSpec(new SpecDefinition("test", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithContextFilter("userservice")
            .Build();
        var results = await runner.RunAsync(root);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task WithContextExcludeFilter_ExcludesMatchingContext()
    {
        var root = new SpecContext("root");
        var legacy = new SpecContext("Legacy", root);
        legacy.AddSpec(new SpecDefinition("legacy test", () => { }));
        var modern = new SpecContext("Modern", root);
        modern.AddSpec(new SpecDefinition("modern test", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithContextExcludeFilter("Legacy")
            .Build();
        var results = await runner.RunAsync(root);

        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(1);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Skipped)).IsEqualTo(1);
    }

    [Test]
    public async Task WithContextExcludeFilter_WildcardExclusion()
    {
        var root = new SpecContext("root");
        var integration = new SpecContext("Integration", root);
        var slow = new SpecContext("Slow", integration);
        slow.AddSpec(new SpecDefinition("slow test", () => { }));
        var unit = new SpecContext("Unit", root);
        unit.AddSpec(new SpecDefinition("unit test", () => { }));

        var runner = new SpecRunnerBuilder()
            .WithContextExcludeFilter("Integration/**")
            .Build();
        var results = await runner.RunAsync(root);

        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(1);
        await Assert.That(results.First(r => r.Status == SpecStatus.Passed).Spec.Description).IsEqualTo("unit test");
    }

    [Test]
    public async Task WithContextFilter_EmptyPatterns_Throws()
    {
        var action = () => new SpecRunnerBuilder().WithContextFilter();

        await Assert.That(action).Throws<ArgumentException>();
    }

    [Test]
    public async Task WithContextExcludeFilter_EmptyPatterns_Throws()
    {
        var action = () => new SpecRunnerBuilder().WithContextExcludeFilter();

        await Assert.That(action).Throws<ArgumentException>();
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