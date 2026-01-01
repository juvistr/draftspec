using FsCheck;
using FsCheck.Fluent;

namespace DraftSpec.Tests.Properties;

/// <summary>
/// Property-based tests for spec execution state invariants.
/// These tests verify execution properties that must hold for all inputs.
/// </summary>
public class ExecutionStatePropertyTests
{
    [Test]
    public async Task SpecStatus_HasExactlyFourValues()
    {
        // Property: SpecStatus enum has exactly 4 values
        var values = Enum.GetValues<SpecStatus>();
        await Assert.That(values.Length).IsEqualTo(4);
        await Assert.That(values).Contains(SpecStatus.Passed);
        await Assert.That(values).Contains(SpecStatus.Failed);
        await Assert.That(values).Contains(SpecStatus.Pending);
        await Assert.That(values).Contains(SpecStatus.Skipped);
    }

    [Test]
    public void SpecResult_StatusIsExclusive()
    {
        // Property: A SpecResult has exactly one status
        var spec = new SpecDefinition("test", () => { });
        var contextPath = new[] { "context" };

        foreach (var status in Enum.GetValues<SpecStatus>())
        {
            var result = new SpecResult(spec, status, contextPath);

            // Count how many status checks would be "true"
            var statusChecks = new[]
            {
                result.Status == SpecStatus.Passed,
                result.Status == SpecStatus.Failed,
                result.Status == SpecStatus.Pending,
                result.Status == SpecStatus.Skipped
            };

            if (statusChecks.Count(x => x) != 1)
                throw new Exception($"Expected exactly one status to be true for {status}");
        }
    }

    [Test]
    public void SpecDefinition_IsPendingWhenNoBody()
    {
        // Property: IsPending is true if and only if Body is null
        Prop.ForAll<bool>(hasBody =>
        {
            SpecDefinition spec;
            if (hasBody)
            {
                spec = new SpecDefinition("test", () => { });
            }
            else
            {
                spec = new SpecDefinition("test"); // No body
            }

            return spec.IsPending == !hasBody;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void SpecDefinition_FocusedAndSkipped_AreIndependent()
    {
        // Property: IsFocused and IsSkipped can be set independently
        var combinations = new[]
        {
            (focused: false, skipped: false),
            (focused: true, skipped: false),
            (focused: false, skipped: true),
            (focused: true, skipped: true) // Both can be true (though semantically odd)
        };

        foreach (var (focused, skipped) in combinations)
        {
            var spec = new SpecDefinition("test", () => { })
            {
                IsFocused = focused,
                IsSkipped = skipped
            };

            if (spec.IsFocused != focused || spec.IsSkipped != skipped)
                throw new Exception($"Expected IsFocused={focused}, IsSkipped={skipped}");
        }
    }

    [Test]
    public void SpecResult_TotalDuration_IsSumOfParts()
    {
        // Property: TotalDuration = BeforeEachDuration + Duration + AfterEachDuration
        Prop.ForAll<int, int, int>((before, spec, after) =>
        {
            var beforeMs = Math.Abs(before % 1000);
            var specMs = Math.Abs(spec % 1000);
            var afterMs = Math.Abs(after % 1000);

            var specDef = new SpecDefinition("test", () => { });
            var result = new SpecResult(specDef, SpecStatus.Passed, ["ctx"])
            {
                BeforeEachDuration = TimeSpan.FromMilliseconds(beforeMs),
                AfterEachDuration = TimeSpan.FromMilliseconds(afterMs)
            } with
            {
                Duration = TimeSpan.FromMilliseconds(specMs)
            };

            var expectedTotal = beforeMs + specMs + afterMs;
            var actualTotal = result.TotalDuration.TotalMilliseconds;

            return Math.Abs(actualTotal - expectedTotal) < 0.001;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void SpecResult_FullDescription_CombinesPathAndSpec()
    {
        // Property: FullDescription joins context path and spec description
        Prop.ForAll<NonNull<string>, NonNull<string>>((ctx1, ctx2) =>
        {
            var path = new[] { ctx1.Get, ctx2.Get };
            var spec = new SpecDefinition("specDesc", () => { });
            var result = new SpecResult(spec, SpecStatus.Passed, path);

            var expected = $"{ctx1.Get} {ctx2.Get} specDesc";
            return result.FullDescription == expected;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task SpecResult_FullDescription_IsCached()
    {
        // Property: Multiple accesses to FullDescription return same string reference
        var spec = new SpecDefinition("test spec", () => { });
        var result = new SpecResult(spec, SpecStatus.Passed, ["context"]);

        var desc1 = result.FullDescription;
        var desc2 = result.FullDescription;

        await Assert.That(ReferenceEquals(desc1, desc2)).IsTrue();
    }

    [Test]
    public void SpecResult_ContextPath_IsPreserved()
    {
        // Property: Context path passed to result is preserved exactly
        Prop.ForAll<int>(depth =>
        {
            var normalizedDepth = Math.Abs(depth % 5) + 1;
            var path = Enumerable.Range(0, normalizedDepth)
                .Select(i => $"context_{i}")
                .ToArray();

            var spec = new SpecDefinition("test", () => { });
            var result = new SpecResult(spec, SpecStatus.Passed, path);

            return result.ContextPath.Count == normalizedDepth &&
                   result.ContextPath.SequenceEqual(path);
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task SpecResult_WithException_HasFailedStatus()
    {
        // Property: Results with exceptions should have Failed status for consistency
        var spec = new SpecDefinition("test", () => { });
        var exception = new InvalidOperationException("Test error");
        var result = new SpecResult(spec, SpecStatus.Failed, ["ctx"], Exception: exception);

        await Assert.That(result.Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(result.Exception).IsNotNull();
        await Assert.That(result.Exception!.Message).IsEqualTo("Test error");
    }

    [Test]
    public async Task SpecDefinition_Tags_DefaultsToEmpty()
    {
        // Property: Tags defaults to empty list if not specified
        var spec = new SpecDefinition("test", () => { });
        await Assert.That(spec.Tags.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SpecDefinition_Tags_PreservesOrder()
    {
        // Property: Tags preserve insertion order
        var tags = new[] { "unit", "fast", "integration" };
        var spec = new SpecDefinition("test", () => { }) { Tags = tags };

        await Assert.That(spec.Tags.Count).IsEqualTo(3);
        await Assert.That(spec.Tags[0]).IsEqualTo("unit");
        await Assert.That(spec.Tags[1]).IsEqualTo("fast");
        await Assert.That(spec.Tags[2]).IsEqualTo("integration");
    }

    [Test]
    public async Task SpecResult_RecordEquality_BasedOnValues()
    {
        // Property: SpecResult record equality is based on values
        var spec = new SpecDefinition("test", () => { });
        var path = new[] { "ctx" };

        var result1 = new SpecResult(spec, SpecStatus.Passed, path);
        var result2 = new SpecResult(spec, SpecStatus.Passed, path);

        // Same spec, status, path => equal
        await Assert.That(result1).IsEqualTo(result2);

        // Different status => not equal
        var result3 = new SpecResult(spec, SpecStatus.Failed, path);
        await Assert.That(result1).IsNotEqualTo(result3);
    }
}
