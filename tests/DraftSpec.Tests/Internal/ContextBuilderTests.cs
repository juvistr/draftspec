using DraftSpec.Internal;

namespace DraftSpec.Tests.Internal;

/// <summary>
/// Tests for ContextBuilder shared helper.
/// </summary>
public class ContextBuilderTests
{
    [Test]
    public async Task CreateNestedContext_CreatesChildWithParent()
    {
        var parent = new SpecContext("parent");
        var child = ContextBuilder.CreateNestedContext("child", parent);

        await Assert.That(child.Description).IsEqualTo("child");
        await Assert.That(parent.Children).Contains(child);
    }

    [Test]
    public async Task AddSpec_WithValidContext_AddsSpec()
    {
        var context = new SpecContext("test");
        var spec = ContextBuilder.CreateSpec("spec", () => { });

        ContextBuilder.AddSpec(context, spec);

        await Assert.That(context.Specs).Contains(spec);
    }

    [Test]
    public async Task AddSpec_WithNullContext_ThrowsInvalidOperationException()
    {
        var spec = ContextBuilder.CreateSpec("spec", () => { });

        await Assert.That(() => ContextBuilder.AddSpec(null, spec))
            .Throws<InvalidOperationException>()
            .WithMessage("Must be called inside a describe() block");
    }

    [Test]
    public async Task CreateSpec_SetsDescriptionAndBody()
    {
        var called = false;
        var spec = ContextBuilder.CreateSpec("test spec", () => called = true);

        await Assert.That(spec.Description).IsEqualTo("test spec");
        await Assert.That(spec.IsPending).IsFalse(); // Body is not null means not pending

        spec.Body!();
        await Assert.That(called).IsTrue();
    }

    [Test]
    public async Task CreatePendingSpec_HasNullBody()
    {
        var spec = ContextBuilder.CreatePendingSpec("pending spec");

        await Assert.That(spec.Description).IsEqualTo("pending spec");
        await Assert.That(spec.IsPending).IsTrue(); // IsPending means Body is null
    }

    [Test]
    public async Task CreateFocusedSpec_SetsFocusedFlag()
    {
        var spec = ContextBuilder.CreateFocusedSpec("focused spec", () => { });

        await Assert.That(spec.IsFocused).IsTrue();
        await Assert.That(spec.IsSkipped).IsFalse();
    }

    [Test]
    public async Task CreateSkippedSpec_SetsSkippedFlag()
    {
        var spec = ContextBuilder.CreateSkippedSpec("skipped spec", () => { });

        await Assert.That(spec.IsSkipped).IsTrue();
        await Assert.That(spec.IsFocused).IsFalse();
    }

    [Test]
    public async Task CreateSkippedSpec_WithNullBody_Works()
    {
        var spec = ContextBuilder.CreateSkippedSpec("skipped pending", null);

        await Assert.That(spec.IsSkipped).IsTrue();
        await Assert.That(spec.IsPending).IsTrue(); // IsPending means Body is null
    }

    [Test]
    public async Task SetBeforeEach_WithValidContext_SetsHook()
    {
        var context = new SpecContext("test");
        var called = false;
        Action hook = () => called = true;

        ContextBuilder.SetBeforeEach(context, hook);

        // Verify hook was set by checking it's in the hook chain
        var chain = context.GetBeforeEachChain();
        await Assert.That(chain).Count().IsEqualTo(1);

        chain[0]();
        await Assert.That(called).IsTrue();
    }

    [Test]
    public async Task SetBeforeEach_WithNullContext_ThrowsInvalidOperationException()
    {
        await Assert.That(() => ContextBuilder.SetBeforeEach(null, () => { }))
            .Throws<InvalidOperationException>()
            .WithMessage("Must be called inside a describe() block");
    }

    [Test]
    public async Task SetAfterEach_WithValidContext_SetsHook()
    {
        var context = new SpecContext("test");
        var called = false;
        Action hook = () => called = true;

        ContextBuilder.SetAfterEach(context, hook);

        // Verify hook was set by checking it's in the hook chain
        var chain = context.GetAfterEachChain();
        await Assert.That(chain).Count().IsEqualTo(1);

        chain[0]();
        await Assert.That(called).IsTrue();
    }

    [Test]
    public async Task SetAfterEach_WithNullContext_ThrowsInvalidOperationException()
    {
        await Assert.That(() => ContextBuilder.SetAfterEach(null, () => { }))
            .Throws<InvalidOperationException>()
            .WithMessage("Must be called inside a describe() block");
    }

    [Test]
    public async Task SetBeforeAll_WithValidContext_SetsHook()
    {
        var context = new SpecContext("test");
        var called = false;
        Action hook = () => called = true;

        ContextBuilder.SetBeforeAll(context, hook);

        // Call the hook directly from the context
        context.BeforeAll!();
        await Assert.That(called).IsTrue();
    }

    [Test]
    public async Task SetBeforeAll_WithNullContext_ThrowsInvalidOperationException()
    {
        await Assert.That(() => ContextBuilder.SetBeforeAll(null, () => { }))
            .Throws<InvalidOperationException>()
            .WithMessage("Must be called inside a describe() block");
    }

    [Test]
    public async Task SetAfterAll_WithValidContext_SetsHook()
    {
        var context = new SpecContext("test");
        var called = false;
        Action hook = () => called = true;

        ContextBuilder.SetAfterAll(context, hook);

        // Call the hook directly from the context
        context.AfterAll!();
        await Assert.That(called).IsTrue();
    }

    [Test]
    public async Task SetAfterAll_WithNullContext_ThrowsInvalidOperationException()
    {
        await Assert.That(() => ContextBuilder.SetAfterAll(null, () => { }))
            .Throws<InvalidOperationException>()
            .WithMessage("Must be called inside a describe() block");
    }

    [Test]
    public async Task EnsureContext_WithNullContext_ThrowsInvalidOperationException()
    {
        await Assert.That(() => ContextBuilder.EnsureContext(null))
            .Throws<InvalidOperationException>()
            .WithMessage("Must be called inside a describe() block");
    }

    [Test]
    public async Task EnsureContext_WithValidContext_DoesNotThrow()
    {
        var context = new SpecContext("test");

        await Assert.That(() => ContextBuilder.EnsureContext(context))
            .ThrowsNothing();
    }
}
