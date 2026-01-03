using DraftSpec.Cli.Interactive;
using DraftSpec.TestingPlatform;
using Spectre.Console.Testing;

namespace DraftSpec.Tests.Cli.Interactive;

/// <summary>
/// Tests for InteractiveSpecSelector.
/// </summary>
public class InteractiveSpecSelectorTests
{
    #region Empty Specs

    [Test]
    public async Task SelectAsync_EmptySpecsList_ReturnsSuccessWithEmptyCollections()
    {
        var console = new TestConsole();
        var selector = new InteractiveSpecSelector(console);

        var result = await selector.SelectAsync([]);

        await Assert.That(result.Cancelled).IsFalse();
        await Assert.That(result.SelectedSpecIds).IsEmpty();
        await Assert.That(result.SelectedDisplayNames).IsEmpty();
        await Assert.That(result.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task SelectAsync_EmptySpecsList_DoesNotRequireInteractiveTerminal()
    {
        // Even non-interactive console should handle empty specs
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = false;
        var selector = new InteractiveSpecSelector(console);

        // Should not throw - empty specs is handled before interactive check
        var result = await selector.SelectAsync([]);

        await Assert.That(result.Cancelled).IsFalse();
    }

    #endregion

    #region Non-Interactive Terminal

    [Test]
    public async Task SelectAsync_NonInteractiveTerminal_ThrowsInvalidOperationException()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = false;
        var selector = new InteractiveSpecSelector(console);
        var specs = new List<DiscoveredSpec> { CreateSpec("test") };

        var act = async () => await selector.SelectAsync(specs);

        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task SelectAsync_NonInteractiveTerminal_ExceptionContainsHelpfulMessage()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = false;
        var selector = new InteractiveSpecSelector(console);
        var specs = new List<DiscoveredSpec> { CreateSpec("test") };

        try
        {
            await selector.SelectAsync(specs);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException ex)
        {
            await Assert.That(ex.Message).Contains("requires an interactive terminal");
        }
    }

    [Test]
    public async Task SelectAsync_NonInteractiveTerminal_ExceptionMentionsCiCd()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = false;
        var selector = new InteractiveSpecSelector(console);
        var specs = new List<DiscoveredSpec> { CreateSpec("test") };

        try
        {
            await selector.SelectAsync(specs);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException ex)
        {
            await Assert.That(ex.Message).Contains("CI/CD");
        }
    }

    #endregion

    #region Interactive Selection

    [Test]
    public async Task SelectAsync_UserSelectsSpec_ReturnsSelectedIds()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        // Single spec in a context (no group header to navigate)
        // Space to select, Enter to confirm
        console.Input.PushKey(ConsoleKey.Spacebar);
        console.Input.PushKey(ConsoleKey.Enter);

        var selector = new InteractiveSpecSelector(console);
        // Use a single spec so there's no grouping
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("lonely spec", ["SingleContext"])
        };

        var result = await selector.SelectAsync(specs);

        await Assert.That(result.Cancelled).IsFalse();
        await Assert.That(result.SelectedSpecIds).Count().IsEqualTo(1);
        await Assert.That(result.TotalCount).IsEqualTo(1);
    }

    [Test]
    public async Task SelectAsync_MultipleSpecsInSameContext_GroupsUnderContext()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        // With grouped items: first item is group header (first spec),
        // so we need to go down to the actual selectable spec
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Spacebar);
        console.Input.PushKey(ConsoleKey.Enter);

        var selector = new InteractiveSpecSelector(console);
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("spec1", ["Context"]),
            CreateSpec("spec2", ["Context"])
        };

        var result = await selector.SelectAsync(specs);

        await Assert.That(result.Cancelled).IsFalse();
        // Selected one spec from the group
        await Assert.That(result.SelectedSpecIds).Count().IsEqualTo(1);
        await Assert.That(result.TotalCount).IsEqualTo(2);
    }

    [Test]
    public async Task SelectAsync_ReturnsCorrectDisplayNames()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Spacebar);
        console.Input.PushKey(ConsoleKey.Enter);

        var selector = new InteractiveSpecSelector(console);
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("my test spec", ["MyContext"])
        };

        var result = await selector.SelectAsync(specs);

        await Assert.That(result.SelectedDisplayNames).Count().IsEqualTo(1);
        await Assert.That(result.SelectedDisplayNames[0]).Contains("my test spec");
    }

    #endregion

    #region Helpers

    private static DiscoveredSpec CreateSpec(
        string description,
        string[]? contextPath = null,
        bool isFocused = false,
        bool isSkipped = false,
        bool isPending = false)
    {
        contextPath ??= ["Test"];
        var displayName = string.Join(" > ", contextPath.Append(description));

        return new DiscoveredSpec
        {
            Id = $"test.spec.csx:{string.Join("/", contextPath)}/{description}",
            Description = description,
            DisplayName = displayName,
            ContextPath = contextPath.ToList(),
            SourceFile = "/path/to/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10,
            IsFocused = isFocused,
            IsSkipped = isSkipped,
            IsPending = isPending,
            Tags = []
        };
    }

    #endregion
}
