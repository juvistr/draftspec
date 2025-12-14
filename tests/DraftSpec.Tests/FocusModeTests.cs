namespace DraftSpec.Tests;

public class FocusModeTests
{
    [Test]
    public async Task Fit_runs_only_focused_spec()
    {
        var spec = new FocusedSpecTest();
        var runner = new SpecRunner();
        var results = runner.Run(spec);

        await Assert.That(results).Count().IsEqualTo(4);

        var first = results.First(r => r.Spec.Description == "first");
        var focused = results.First(r => r.Spec.Description == "focused");
        var third = results.First(r => r.Spec.Description == "third");
        var fourth = results.First(r => r.Spec.Description == "fourth");

        await Assert.That(first.Status).IsEqualTo(SpecStatus.Skipped);
        await Assert.That(focused.Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(third.Status).IsEqualTo(SpecStatus.Skipped);
        await Assert.That(fourth.Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task Multiple_fit_specs_all_run()
    {
        var spec = new MultipleFocusedSpecsTest();
        var runner = new SpecRunner();
        var results = runner.Run(spec);

        var focused1 = results.First(r => r.Spec.Description == "focused 1");
        var focused2 = results.First(r => r.Spec.Description == "focused 2");
        var unfocused = results.First(r => r.Spec.Description == "unfocused");

        await Assert.That(focused1.Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(focused2.Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(unfocused.Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task Fit_in_nested_context_works()
    {
        var spec = new NestedFocusedSpecTest();
        var runner = new SpecRunner();
        var results = runner.Run(spec);

        var outer = results.First(r => r.Spec.Description == "outer spec");
        var inner = results.First(r => r.Spec.Description == "inner focused");

        await Assert.That(outer.Status).IsEqualTo(SpecStatus.Skipped);
        await Assert.That(inner.Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task No_fit_specs_means_all_run()
    {
        var spec = new NoFocusedSpecsTest();
        var runner = new SpecRunner();
        var results = runner.Run(spec);

        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();
    }

    [Test]
    public async Task Xit_stays_skipped_even_without_focus()
    {
        var spec = new SkippedSpecTest();
        var runner = new SpecRunner();
        var results = runner.Run(spec);

        var normal = results.First(r => r.Spec.Description == "normal");
        var skipped = results.First(r => r.Spec.Description == "skipped");

        await Assert.That(normal.Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(skipped.Status).IsEqualTo(SpecStatus.Skipped);
    }

    private class FocusedSpecTest : Spec
    {
        public FocusedSpecTest()
        {
            describe("focus test", () =>
            {
                it("first", () => { });
                fit("focused", () => { });
                it("third", () => { });
                it("fourth", () => { });
            });
        }
    }

    private class MultipleFocusedSpecsTest : Spec
    {
        public MultipleFocusedSpecsTest()
        {
            describe("multi focus", () =>
            {
                fit("focused 1", () => { });
                it("unfocused", () => { });
                fit("focused 2", () => { });
            });
        }
    }

    private class NestedFocusedSpecTest : Spec
    {
        public NestedFocusedSpecTest()
        {
            describe("outer", () =>
            {
                it("outer spec", () => { });

                describe("inner", () =>
                {
                    fit("inner focused", () => { });
                });
            });
        }
    }

    private class NoFocusedSpecsTest : Spec
    {
        public NoFocusedSpecsTest()
        {
            describe("all run", () =>
            {
                it("one", () => { });
                it("two", () => { });
            });
        }
    }

    private class SkippedSpecTest : Spec
    {
        public SkippedSpecTest()
        {
            describe("skip test", () =>
            {
                it("normal", () => { });
                xit("skipped", () => { });
            });
        }
    }
}
