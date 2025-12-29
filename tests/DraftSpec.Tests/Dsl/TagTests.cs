using static DraftSpec.Dsl;

namespace DraftSpec.Tests.Dsl;

/// <summary>
/// Tests for the tag() and tags() DSL functions.
/// </summary>
public class TagTests
{
    [Before(Test)]
    public void SetUp()
    {
        Reset();
    }

    #region Single Tag

    [Test]
    public async Task Tag_AppliesTagToSpecsInBlock()
    {
        describe("test", () => { tag("slow", () => { it("tagged spec", () => { }); }); });

        var spec = RootContext!.Specs[0];
        var tags = spec.Tags.ToList();

        await Assert.That(tags).Contains("slow");
    }

    [Test]
    public async Task Tag_DoesNotApplyToSpecsOutsideBlock()
    {
        describe("test", () =>
        {
            tag("slow", () => { it("tagged spec", () => { }); });

            it("untagged spec", () => { });
        });

        var taggedSpec = RootContext!.Specs[0];
        var untaggedSpec = RootContext.Specs[1];
        var taggedTags = taggedSpec.Tags.ToList();
        var untaggedTags = untaggedSpec.Tags.ToList();

        await Assert.That(taggedTags).Contains("slow");
        await Assert.That(untaggedTags).IsEmpty();
    }

    #endregion

    #region Nested Tags

    [Test]
    public async Task NestedTags_Accumulate()
    {
        describe("test",
            () => { tag("slow", () => { tag("integration", () => { it("doubly tagged spec", () => { }); }); }); });

        var spec = RootContext!.Specs[0];
        var tags = spec.Tags.ToList();

        await Assert.That(tags).Contains("slow");
        await Assert.That(tags).Contains("integration");
    }

    [Test]
    public async Task Tag_ResetsAfterBlock()
    {
        describe("test", () =>
        {
            tag("slow", () => { it("tagged spec", () => { }); });

            // After the block, tags should be reset
            it("untagged spec", () => { });
        });

        var untaggedSpec = RootContext!.Specs[1];
        var tags = untaggedSpec.Tags.ToList();

        await Assert.That(tags).IsEmpty();
    }

    #endregion

    #region Multiple Tags

    [Test]
    public async Task Tags_AppliesMultipleTagsToSpecsInBlock()
    {
        describe("test", () => { tags(["slow", "integration"], () => { it("multi-tagged spec", () => { }); }); });

        var spec = RootContext!.Specs[0];
        var specTags = spec.Tags.ToList();

        await Assert.That(specTags).Contains("slow");
        await Assert.That(specTags).Contains("integration");
    }

    [Test]
    public async Task Tags_CombinesWithNestedTag()
    {
        describe("test",
            () =>
            {
                tags(["slow", "integration"],
                    () => { tag("database", () => { it("triple tagged spec", () => { }); }); });
            });

        var spec = RootContext!.Specs[0];
        var specTags = spec.Tags.ToList();

        await Assert.That(specTags).Contains("slow");
        await Assert.That(specTags).Contains("integration");
        await Assert.That(specTags).Contains("database");
    }

    #endregion

    #region Tags with Different Spec Types

    [Test]
    public async Task Tag_WorksWithFocusedSpecs()
    {
        describe("test", () => { tag("slow", () => { fit("focused tagged spec", () => { }); }); });

        var spec = RootContext!.Specs[0];
        var isFocused = spec.IsFocused;
        var tags = spec.Tags.ToList();

        await Assert.That(isFocused).IsTrue();
        await Assert.That(tags).Contains("slow");
    }

    [Test]
    public async Task Tag_WorksWithSkippedSpecs()
    {
        describe("test", () => { tag("slow", () => { xit("skipped tagged spec", () => { }); }); });

        var spec = RootContext!.Specs[0];
        var isSkipped = spec.IsSkipped;
        var tags = spec.Tags.ToList();

        await Assert.That(isSkipped).IsTrue();
        await Assert.That(tags).Contains("slow");
    }

    [Test]
    public async Task Tag_WorksWithPendingSpecs()
    {
        describe("test", () => { tag("slow", () => { it("pending tagged spec"); }); });

        var spec = RootContext!.Specs[0];
        var isPending = spec.IsPending;
        var tags = spec.Tags.ToList();

        await Assert.That(isPending).IsTrue();
        await Assert.That(tags).Contains("slow");
    }

    #endregion

    #region Tags in Nested Contexts

    [Test]
    public async Task Tag_WorksWithNestedDescribe()
    {
        describe("outer",
            () => { tag("slow", () => { describe("inner", () => { it("spec in nested context", () => { }); }); }); });

        var innerContext = RootContext!.Children[0];
        var spec = innerContext.Specs[0];
        var tags = spec.Tags.ToList();

        await Assert.That(tags).Contains("slow");
    }

    #endregion
}