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

        await spec.Body!();
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
    public async Task AddBeforeEach_WithValidContext_AddsHook()
    {
        var context = new SpecContext("test");
        var called = false;
        Action hook = () => called = true;

        ContextBuilder.AddBeforeEach(context, hook);

        // Verify hook was set by checking it's in the hook chain
        var chain = context.GetBeforeEachChain();
        await Assert.That(chain).Count().IsEqualTo(1);

        await chain[0]();
        await Assert.That(called).IsTrue();
    }

    [Test]
    public async Task AddBeforeEach_WithNullContext_ThrowsInvalidOperationException()
    {
        await Assert.That(() => ContextBuilder.AddBeforeEach(null, () => { }))
            .Throws<InvalidOperationException>()
            .WithMessage("Must be called inside a describe() block");
    }

    [Test]
    public async Task AddAfterEach_WithValidContext_AddsHook()
    {
        var context = new SpecContext("test");
        var called = false;
        Action hook = () => called = true;

        ContextBuilder.AddAfterEach(context, hook);

        // Verify hook was set by checking it's in the hook chain
        var chain = context.GetAfterEachChain();
        await Assert.That(chain).Count().IsEqualTo(1);

        await chain[0]();
        await Assert.That(called).IsTrue();
    }

    [Test]
    public async Task AddAfterEach_WithNullContext_ThrowsInvalidOperationException()
    {
        await Assert.That(() => ContextBuilder.AddAfterEach(null, () => { }))
            .Throws<InvalidOperationException>()
            .WithMessage("Must be called inside a describe() block");
    }

    [Test]
    public async Task AddBeforeAll_WithValidContext_AddsHook()
    {
        var context = new SpecContext("test");
        var called = false;
        Action hook = () => called = true;

        ContextBuilder.AddBeforeAll(context, hook);

        // Call the hook directly from the context
        await Assert.That(context.BeforeAllHooks).Count().IsEqualTo(1);
        await context.BeforeAllHooks[0]();
        await Assert.That(called).IsTrue();
    }

    [Test]
    public async Task AddBeforeAll_WithNullContext_ThrowsInvalidOperationException()
    {
        await Assert.That(() => ContextBuilder.AddBeforeAll(null, () => { }))
            .Throws<InvalidOperationException>()
            .WithMessage("Must be called inside a describe() block");
    }

    [Test]
    public async Task AddAfterAll_WithValidContext_AddsHook()
    {
        var context = new SpecContext("test");
        var called = false;
        Action hook = () => called = true;

        ContextBuilder.AddAfterAll(context, hook);

        // Call the hook directly from the context
        await Assert.That(context.AfterAllHooks).Count().IsEqualTo(1);
        await context.AfterAllHooks[0]();
        await Assert.That(called).IsTrue();
    }

    [Test]
    public async Task AddAfterAll_WithNullContext_ThrowsInvalidOperationException()
    {
        await Assert.That(() => ContextBuilder.AddAfterAll(null, () => { }))
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
