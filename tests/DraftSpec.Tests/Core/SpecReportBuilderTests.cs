using DraftSpec.Formatters;

namespace DraftSpec.Tests.Core;

/// <summary>
/// Comprehensive tests for SpecReportBuilder that builds formatted reports from execution results.
/// </summary>
public class SpecReportBuilderTests
{
    #region Basic Report Building

    [Test]
    public async Task Build_WithSinglePassedSpec_CreatesCorrectReport()
    {
        var context = new SpecContext("test context");
        var spec = new SpecDefinition("passes", () => { });
        context.AddSpec(spec);

        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Passed, ["test context"], TimeSpan.FromMilliseconds(10))
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.Total).IsEqualTo(1);
        await Assert.That(report.Summary.Passed).IsEqualTo(1);
        await Assert.That(report.Summary.Failed).IsEqualTo(0);
        await Assert.That(report.Summary.Pending).IsEqualTo(0);
        await Assert.That(report.Summary.Skipped).IsEqualTo(0);
        await Assert.That(report.Summary.Success).IsTrue();
        await Assert.That(report.Summary.DurationMs).IsEqualTo(10.0);
    }

    [Test]
    public async Task Build_WithSingleFailedSpec_CreatesCorrectReport()
    {
        var context = new SpecContext("test context");
        var spec = new SpecDefinition("fails", () => throw new InvalidOperationException("test error"));
        context.AddSpec(spec);

        var exception = new InvalidOperationException("test error");
        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Failed, ["test context"], TimeSpan.FromMilliseconds(5), exception)
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.Total).IsEqualTo(1);
        await Assert.That(report.Summary.Passed).IsEqualTo(0);
        await Assert.That(report.Summary.Failed).IsEqualTo(1);
        await Assert.That(report.Summary.Pending).IsEqualTo(0);
        await Assert.That(report.Summary.Skipped).IsEqualTo(0);
        await Assert.That(report.Summary.Success).IsFalse();
        await Assert.That(report.Summary.DurationMs).IsEqualTo(5.0);
    }

    [Test]
    public async Task Build_WithPendingSpec_CreatesCorrectReport()
    {
        var context = new SpecContext("test context");
        var spec = new SpecDefinition("pending");
        context.AddSpec(spec);

        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Pending, ["test context"], TimeSpan.Zero)
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.Total).IsEqualTo(1);
        await Assert.That(report.Summary.Passed).IsEqualTo(0);
        await Assert.That(report.Summary.Failed).IsEqualTo(0);
        await Assert.That(report.Summary.Pending).IsEqualTo(1);
        await Assert.That(report.Summary.Skipped).IsEqualTo(0);
    }

    [Test]
    public async Task Build_WithSkippedSpec_CreatesCorrectReport()
    {
        var context = new SpecContext("test context");
        var spec = new SpecDefinition("skipped", () => { }) { IsSkipped = true };
        context.AddSpec(spec);

        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Skipped, ["test context"], TimeSpan.Zero)
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.Total).IsEqualTo(1);
        await Assert.That(report.Summary.Passed).IsEqualTo(0);
        await Assert.That(report.Summary.Failed).IsEqualTo(0);
        await Assert.That(report.Summary.Pending).IsEqualTo(0);
        await Assert.That(report.Summary.Skipped).IsEqualTo(1);
    }

    #endregion

    #region Multiple Specs

    [Test]
    public async Task Build_WithMixedResults_AggregatesCorrectly()
    {
        var context = new SpecContext("test context");
        var spec1 = new SpecDefinition("passes", () => { });
        var spec2 = new SpecDefinition("fails", () => throw new Exception());
        var spec3 = new SpecDefinition("pending");
        var spec4 = new SpecDefinition("skipped", () => { }) { IsSkipped = true };
        var spec5 = new SpecDefinition("also passes", () => { });

        context.AddSpec(spec1);
        context.AddSpec(spec2);
        context.AddSpec(spec3);
        context.AddSpec(spec4);
        context.AddSpec(spec5);

        var results = new List<SpecResult>
        {
            new(spec1, SpecStatus.Passed, ["test context"], TimeSpan.FromMilliseconds(10)),
            new(spec2, SpecStatus.Failed, ["test context"], TimeSpan.FromMilliseconds(5), new Exception("fail")),
            new(spec3, SpecStatus.Pending, ["test context"], TimeSpan.Zero),
            new(spec4, SpecStatus.Skipped, ["test context"], TimeSpan.Zero),
            new(spec5, SpecStatus.Passed, ["test context"], TimeSpan.FromMilliseconds(15))
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.Total).IsEqualTo(5);
        await Assert.That(report.Summary.Passed).IsEqualTo(2);
        await Assert.That(report.Summary.Failed).IsEqualTo(1);
        await Assert.That(report.Summary.Pending).IsEqualTo(1);
        await Assert.That(report.Summary.Skipped).IsEqualTo(1);
        await Assert.That(report.Summary.DurationMs).IsEqualTo(30.0);
        await Assert.That(report.Summary.Success).IsFalse();
    }

    #endregion

    #region Duration Aggregation

    [Test]
    public async Task Build_AggregatesDurationsCorrectly()
    {
        var context = new SpecContext("test context");
        var spec1 = new SpecDefinition("first", () => { });
        var spec2 = new SpecDefinition("second", () => { });
        var spec3 = new SpecDefinition("third", () => { });

        context.AddSpec(spec1);
        context.AddSpec(spec2);
        context.AddSpec(spec3);

        var results = new List<SpecResult>
        {
            new(spec1, SpecStatus.Passed, ["test context"], TimeSpan.FromMilliseconds(100.5)),
            new(spec2, SpecStatus.Passed, ["test context"], TimeSpan.FromMilliseconds(200.3)),
            new(spec3, SpecStatus.Passed, ["test context"], TimeSpan.FromMilliseconds(50.2))
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.DurationMs).IsEqualTo(351.0);
    }

    [Test]
    public async Task Build_WithZeroDuration_HandlesCorrectly()
    {
        var context = new SpecContext("test context");
        var spec = new SpecDefinition("instant", () => { });
        context.AddSpec(spec);

        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Passed, ["test context"], TimeSpan.Zero)
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.DurationMs).IsEqualTo(0.0);
    }

    [Test]
    public async Task Build_WithLargeDuration_HandlesCorrectly()
    {
        var context = new SpecContext("test context");
        var spec = new SpecDefinition("slow", () => { });
        context.AddSpec(spec);

        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Passed, ["test context"], TimeSpan.FromSeconds(30))
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.DurationMs).IsEqualTo(30000.0);
    }

    #endregion

    #region Nested Context Report Building

    [Test]
    public async Task Build_WithNestedContext_CreatesCorrectHierarchy()
    {
        var root = new SpecContext("Calculator");
        var child = new SpecContext("add", root);

        var spec = new SpecDefinition("returns sum", () => { });
        child.AddSpec(spec);

        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Passed, ["Calculator", "add"], TimeSpan.FromMilliseconds(5))
        };

        var report = SpecReportBuilder.Build(root, results);

        await Assert.That(report.Contexts).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Description).IsEqualTo("Calculator");
        await Assert.That(report.Contexts[0].Contexts).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Contexts[0].Description).IsEqualTo("add");
        await Assert.That(report.Contexts[0].Contexts[0].Specs).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Contexts[0].Specs[0].Description).IsEqualTo("returns sum");
    }

    [Test]
    public async Task Build_WithDeeplyNestedContexts_CreatesFullTree()
    {
        var level1 = new SpecContext("Level1");
        var level2 = new SpecContext("Level2", level1);
        var level3 = new SpecContext("Level3", level2);
        var level4 = new SpecContext("Level4", level3);

        var spec = new SpecDefinition("deep spec", () => { });
        level4.AddSpec(spec);

        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Passed, ["Level1", "Level2", "Level3", "Level4"], TimeSpan.FromMilliseconds(1))
        };

        var report = SpecReportBuilder.Build(level1, results);

        var current = report.Contexts[0];
        await Assert.That(current.Description).IsEqualTo("Level1");

        current = current.Contexts[0];
        await Assert.That(current.Description).IsEqualTo("Level2");

        current = current.Contexts[0];
        await Assert.That(current.Description).IsEqualTo("Level3");

        current = current.Contexts[0];
        await Assert.That(current.Description).IsEqualTo("Level4");
        await Assert.That(current.Specs).Count().IsEqualTo(1);
        await Assert.That(current.Specs[0].Description).IsEqualTo("deep spec");
    }

    [Test]
    public async Task Build_WithMultipleSiblingContexts_IncludesAllSiblings()
    {
        var root = new SpecContext("Math");
        var add = new SpecContext("add", root);
        var subtract = new SpecContext("subtract", root);
        var multiply = new SpecContext("multiply", root);

        var addSpec = new SpecDefinition("adds numbers", () => { });
        var subtractSpec = new SpecDefinition("subtracts numbers", () => { });
        var multiplySpec = new SpecDefinition("multiplies numbers", () => { });

        add.AddSpec(addSpec);
        subtract.AddSpec(subtractSpec);
        multiply.AddSpec(multiplySpec);

        var results = new List<SpecResult>
        {
            new(addSpec, SpecStatus.Passed, ["Math", "add"], TimeSpan.FromMilliseconds(1)),
            new(subtractSpec, SpecStatus.Passed, ["Math", "subtract"], TimeSpan.FromMilliseconds(2)),
            new(multiplySpec, SpecStatus.Passed, ["Math", "multiply"], TimeSpan.FromMilliseconds(3))
        };

        var report = SpecReportBuilder.Build(root, results);

        await Assert.That(report.Contexts).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Description).IsEqualTo("Math");
        await Assert.That(report.Contexts[0].Contexts).Count().IsEqualTo(3);

        var childDescriptions = report.Contexts[0].Contexts.Select(c => c.Description).ToList();
        await Assert.That(childDescriptions).Contains("add");
        await Assert.That(childDescriptions).Contains("subtract");
        await Assert.That(childDescriptions).Contains("multiply");
    }

    [Test]
    public async Task Build_WithSpecsInParentAndChild_IncludesAllSpecs()
    {
        var root = new SpecContext("Parent");
        var child = new SpecContext("Child", root);

        var rootSpec = new SpecDefinition("root spec", () => { });
        var childSpec = new SpecDefinition("child spec", () => { });

        root.AddSpec(rootSpec);
        child.AddSpec(childSpec);

        var results = new List<SpecResult>
        {
            new(rootSpec, SpecStatus.Passed, ["Parent"], TimeSpan.FromMilliseconds(1)),
            new(childSpec, SpecStatus.Passed, ["Parent", "Child"], TimeSpan.FromMilliseconds(2))
        };

        var report = SpecReportBuilder.Build(root, results);

        await Assert.That(report.Contexts).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Description).IsEqualTo("Parent");
        await Assert.That(report.Contexts[0].Specs).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Specs[0].Description).IsEqualTo("root spec");
        await Assert.That(report.Contexts[0].Contexts).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Contexts[0].Specs).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Contexts[0].Specs[0].Description).IsEqualTo("child spec");
    }

    #endregion

    #region Empty Context Handling

    [Test]
    public async Task Build_WithEmptyContext_ReturnsEmptyContextList()
    {
        var context = new SpecContext("empty");
        var results = new List<SpecResult>();

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.Total).IsEqualTo(0);
        await Assert.That(report.Contexts).IsEmpty();
    }

    [Test]
    public async Task Build_WithEmptyNestedContext_ExcludesEmptyContext()
    {
        var root = new SpecContext("root");
        var _ = new SpecContext("empty child", root);

        var results = new List<SpecResult>();

        var report = SpecReportBuilder.Build(root, results);

        await Assert.That(report.Contexts).IsEmpty();
    }

    [Test]
    public async Task Build_WithOnlyEmptyChildren_ExcludesAllEmptyContexts()
    {
        var root = new SpecContext("root");
        var _ = new SpecContext("empty1", root);
        var __ = new SpecContext("empty2", root);
        var ___ = new SpecContext("empty3", root);

        var results = new List<SpecResult>();

        var report = SpecReportBuilder.Build(root, results);

        await Assert.That(report.Contexts).IsEmpty();
    }

    [Test]
    public async Task Build_WithMixedEmptyAndNonEmpty_IncludesOnlyNonEmpty()
    {
        var root = new SpecContext("root");
        var empty = new SpecContext("empty", root);
        var withSpec = new SpecContext("with spec", root);

        var spec = new SpecDefinition("test", () => { });
        withSpec.AddSpec(spec);

        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Passed, ["root", "with spec"], TimeSpan.FromMilliseconds(1))
        };

        var report = SpecReportBuilder.Build(root, results);

        await Assert.That(report.Contexts).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Contexts).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Contexts[0].Description).IsEqualTo("with spec");
    }

    #endregion

    #region Spec Result Details

    [Test]
    public async Task Build_IncludesSpecDescription()
    {
        var context = new SpecContext("test");
        var spec = new SpecDefinition("should do something amazing", () => { });
        context.AddSpec(spec);

        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Passed, ["test"], TimeSpan.FromMilliseconds(1))
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Contexts[0].Specs[0].Description).IsEqualTo("should do something amazing");
    }

    [Test]
    public async Task Build_IncludesStatusAsLowercaseString()
    {
        var context = new SpecContext("test");
        var spec1 = new SpecDefinition("passed", () => { });
        var spec2 = new SpecDefinition("failed", () => throw new Exception());
        var spec3 = new SpecDefinition("pending");
        var spec4 = new SpecDefinition("skipped", () => { }) { IsSkipped = true };

        context.AddSpec(spec1);
        context.AddSpec(spec2);
        context.AddSpec(spec3);
        context.AddSpec(spec4);

        var results = new List<SpecResult>
        {
            new(spec1, SpecStatus.Passed, ["test"], TimeSpan.Zero),
            new(spec2, SpecStatus.Failed, ["test"], TimeSpan.Zero, new Exception()),
            new(spec3, SpecStatus.Pending, ["test"], TimeSpan.Zero),
            new(spec4, SpecStatus.Skipped, ["test"], TimeSpan.Zero)
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Contexts[0].Specs[0].Status).IsEqualTo("passed");
        await Assert.That(report.Contexts[0].Specs[1].Status).IsEqualTo("failed");
        await Assert.That(report.Contexts[0].Specs[2].Status).IsEqualTo("pending");
        await Assert.That(report.Contexts[0].Specs[3].Status).IsEqualTo("skipped");
    }

    [Test]
    public async Task Build_IncludesDurationForEachSpec()
    {
        var context = new SpecContext("test");
        var spec1 = new SpecDefinition("fast", () => { });
        var spec2 = new SpecDefinition("slow", () => { });

        context.AddSpec(spec1);
        context.AddSpec(spec2);

        var results = new List<SpecResult>
        {
            new(spec1, SpecStatus.Passed, ["test"], TimeSpan.FromMilliseconds(5.5)),
            new(spec2, SpecStatus.Passed, ["test"], TimeSpan.FromMilliseconds(100.3))
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Contexts[0].Specs[0].DurationMs).IsEqualTo(5.5);
        await Assert.That(report.Contexts[0].Specs[1].DurationMs).IsEqualTo(100.3);
    }

    [Test]
    public async Task Build_IncludesErrorMessageForFailedSpec()
    {
        var context = new SpecContext("test");
        var spec = new SpecDefinition("fails", () => throw new InvalidOperationException());
        context.AddSpec(spec);

        var exception = new InvalidOperationException("Something went wrong");
        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Failed, ["test"], TimeSpan.Zero, exception)
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Contexts[0].Specs[0].Error).IsEqualTo("Something went wrong");
    }

    [Test]
    public async Task Build_NoErrorForPassedSpec()
    {
        var context = new SpecContext("test");
        var spec = new SpecDefinition("passes", () => { });
        context.AddSpec(spec);

        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Passed, ["test"], TimeSpan.Zero)
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Contexts[0].Specs[0].Error).IsNull();
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Build_WithMissingResult_MarksAsUnknown()
    {
        var context = new SpecContext("test");
        var spec1 = new SpecDefinition("has result", () => { });
        var spec2 = new SpecDefinition("missing result", () => { });

        context.AddSpec(spec1);
        context.AddSpec(spec2);

        // Only provide result for spec1
        var results = new List<SpecResult>
        {
            new(spec1, SpecStatus.Passed, ["test"], TimeSpan.FromMilliseconds(1))
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Contexts[0].Specs).Count().IsEqualTo(2);
        await Assert.That(report.Contexts[0].Specs[0].Status).IsEqualTo("passed");
        await Assert.That(report.Contexts[0].Specs[1].Status).IsEqualTo("unknown");
        await Assert.That(report.Contexts[0].Specs[1].DurationMs).IsNull();
    }

    [Test]
    public async Task Build_WithDuplicateResultsForSameSpec_UsesFirstResult()
    {
        var context = new SpecContext("test");
        var spec = new SpecDefinition("test spec", () => { });
        context.AddSpec(spec);

        // This shouldn't happen in practice, but test the behavior
        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Passed, ["test"], TimeSpan.FromMilliseconds(1)),
            new(spec, SpecStatus.Failed, ["test"], TimeSpan.FromMilliseconds(2), new Exception("error"))
        };

        // Build uses ToDictionary which will throw on duplicate keys
        await Assert.That(() => SpecReportBuilder.Build(context, results))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Build_SetsTimestamp()
    {
        var context = new SpecContext("test");
        var results = new List<SpecResult>();

        var before = DateTime.UtcNow;
        var report = SpecReportBuilder.Build(context, results);
        var after = DateTime.UtcNow;

        await Assert.That(report.Timestamp).IsGreaterThanOrEqualTo(before);
        await Assert.That(report.Timestamp).IsLessThanOrEqualTo(after);
    }

    [Test]
    public async Task Build_WithComplexHierarchy_BuildsCorrectly()
    {
        // Build a complex tree with multiple branches
        var root = new SpecContext("App");

        var auth = new SpecContext("Auth", root);
        var authLogin = new SpecContext("Login", auth);
        var authLogout = new SpecContext("Logout", auth);

        var api = new SpecContext("API", root);
        var apiUsers = new SpecContext("Users", api);
        var apiPosts = new SpecContext("Posts", api);

        var loginSpec1 = new SpecDefinition("validates credentials", () => { });
        var loginSpec2 = new SpecDefinition("returns token", () => { });
        var logoutSpec = new SpecDefinition("clears session", () => { });
        var usersSpec = new SpecDefinition("lists users", () => { });
        var postsSpec = new SpecDefinition("creates post", () => { });

        authLogin.AddSpec(loginSpec1);
        authLogin.AddSpec(loginSpec2);
        authLogout.AddSpec(logoutSpec);
        apiUsers.AddSpec(usersSpec);
        apiPosts.AddSpec(postsSpec);

        var results = new List<SpecResult>
        {
            new(loginSpec1, SpecStatus.Passed, ["App", "Auth", "Login"], TimeSpan.FromMilliseconds(1)),
            new(loginSpec2, SpecStatus.Passed, ["App", "Auth", "Login"], TimeSpan.FromMilliseconds(2)),
            new(logoutSpec, SpecStatus.Passed, ["App", "Auth", "Logout"], TimeSpan.FromMilliseconds(3)),
            new(usersSpec, SpecStatus.Passed, ["App", "API", "Users"], TimeSpan.FromMilliseconds(4)),
            new(postsSpec, SpecStatus.Failed, ["App", "API", "Posts"], TimeSpan.FromMilliseconds(5), new Exception("API error"))
        };

        var report = SpecReportBuilder.Build(root, results);

        // Verify summary
        await Assert.That(report.Summary.Total).IsEqualTo(5);
        await Assert.That(report.Summary.Passed).IsEqualTo(4);
        await Assert.That(report.Summary.Failed).IsEqualTo(1);

        // Verify tree structure
        await Assert.That(report.Contexts).Count().IsEqualTo(1);
        var appContext = report.Contexts[0];
        await Assert.That(appContext.Description).IsEqualTo("App");
        await Assert.That(appContext.Contexts).Count().IsEqualTo(2);

        var authContext = appContext.Contexts.First(c => c.Description == "Auth");
        await Assert.That(authContext.Contexts).Count().IsEqualTo(2);

        var loginContext = authContext.Contexts.First(c => c.Description == "Login");
        await Assert.That(loginContext.Specs).Count().IsEqualTo(2);

        var apiContext = appContext.Contexts.First(c => c.Description == "API");
        await Assert.That(apiContext.Contexts).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Build_WithNoResults_CreatesValidEmptyReport()
    {
        var context = new SpecContext("test");
        var spec = new SpecDefinition("test", () => { });
        context.AddSpec(spec);

        var results = new List<SpecResult>();

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.Total).IsEqualTo(0);
        await Assert.That(report.Summary.Passed).IsEqualTo(0);
        await Assert.That(report.Summary.Failed).IsEqualTo(0);
        await Assert.That(report.Summary.Pending).IsEqualTo(0);
        await Assert.That(report.Summary.Skipped).IsEqualTo(0);
        await Assert.That(report.Summary.DurationMs).IsEqualTo(0.0);
        await Assert.That(report.Summary.Success).IsTrue();
    }

    [Test]
    public async Task Build_PreservesSpecOrderInContext()
    {
        var context = new SpecContext("test");
        var spec1 = new SpecDefinition("first", () => { });
        var spec2 = new SpecDefinition("second", () => { });
        var spec3 = new SpecDefinition("third", () => { });

        context.AddSpec(spec1);
        context.AddSpec(spec2);
        context.AddSpec(spec3);

        var results = new List<SpecResult>
        {
            new(spec1, SpecStatus.Passed, ["test"], TimeSpan.Zero),
            new(spec2, SpecStatus.Passed, ["test"], TimeSpan.Zero),
            new(spec3, SpecStatus.Passed, ["test"], TimeSpan.Zero)
        };

        var report = SpecReportBuilder.Build(context, results);

        var specs = report.Contexts[0].Specs;
        await Assert.That(specs[0].Description).IsEqualTo("first");
        await Assert.That(specs[1].Description).IsEqualTo("second");
        await Assert.That(specs[2].Description).IsEqualTo("third");
    }

    [Test]
    public async Task Build_WithDifferentContextPaths_HandlesCorrectly()
    {
        var root = new SpecContext("root");
        var child1 = new SpecContext("child1", root);
        var child2 = new SpecContext("child2", root);

        var spec1 = new SpecDefinition("spec1", () => { });
        var spec2 = new SpecDefinition("spec2", () => { });

        child1.AddSpec(spec1);
        child2.AddSpec(spec2);

        var results = new List<SpecResult>
        {
            new(spec1, SpecStatus.Passed, ["root", "child1"], TimeSpan.FromMilliseconds(1)),
            new(spec2, SpecStatus.Passed, ["root", "child2"], TimeSpan.FromMilliseconds(2))
        };

        var report = SpecReportBuilder.Build(root, results);

        await Assert.That(report.Contexts).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Contexts).Count().IsEqualTo(2);

        var child1Report = report.Contexts[0].Contexts.First(c => c.Description == "child1");
        await Assert.That(child1Report.Specs).Count().IsEqualTo(1);
        await Assert.That(child1Report.Specs[0].Description).IsEqualTo("spec1");

        var child2Report = report.Contexts[0].Contexts.First(c => c.Description == "child2");
        await Assert.That(child2Report.Specs).Count().IsEqualTo(1);
        await Assert.That(child2Report.Specs[0].Description).IsEqualTo("spec2");
    }

    #endregion

    #region Summary Success Property

    [Test]
    public async Task Build_Summary_SuccessIsTrueWhenNoFailures()
    {
        var context = new SpecContext("test");
        var spec1 = new SpecDefinition("pass", () => { });
        var spec2 = new SpecDefinition("pending");
        var spec3 = new SpecDefinition("skip", () => { }) { IsSkipped = true };

        context.AddSpec(spec1);
        context.AddSpec(spec2);
        context.AddSpec(spec3);

        var results = new List<SpecResult>
        {
            new(spec1, SpecStatus.Passed, ["test"], TimeSpan.Zero),
            new(spec2, SpecStatus.Pending, ["test"], TimeSpan.Zero),
            new(spec3, SpecStatus.Skipped, ["test"], TimeSpan.Zero)
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.Success).IsTrue();
    }

    [Test]
    public async Task Build_Summary_SuccessIsFalseWhenAnyFailure()
    {
        var context = new SpecContext("test");
        var spec1 = new SpecDefinition("pass", () => { });
        var spec2 = new SpecDefinition("fail", () => throw new Exception());

        context.AddSpec(spec1);
        context.AddSpec(spec2);

        var results = new List<SpecResult>
        {
            new(spec1, SpecStatus.Passed, ["test"], TimeSpan.Zero),
            new(spec2, SpecStatus.Failed, ["test"], TimeSpan.Zero, new Exception())
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.Success).IsFalse();
    }

    #endregion
}
