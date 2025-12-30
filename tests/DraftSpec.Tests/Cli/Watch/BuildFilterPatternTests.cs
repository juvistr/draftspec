using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Watch;

namespace DraftSpec.Tests.Cli.Watch;

/// <summary>
/// Tests for WatchCommand.BuildFilterPattern static method.
/// </summary>
public class BuildFilterPatternTests
{
    #region Empty Input

    [Test]
    public async Task BuildFilterPattern_EmptyList_ReturnsMatchNothing()
    {
        var specs = new List<SpecChange>();

        var pattern = WatchCommand.BuildFilterPattern(specs);

        await Assert.That(pattern).IsEqualTo("^$");
    }

    #endregion

    #region Single Spec

    [Test]
    public async Task BuildFilterPattern_SingleSpec_ReturnsExactMatch()
    {
        var specs = new List<SpecChange>
        {
            new("creates a todo", ["TodoService"], SpecChangeType.Added)
        };

        var pattern = WatchCommand.BuildFilterPattern(specs);

        // Regex.Escape escapes spaces as "\ "
        await Assert.That(pattern).IsEqualTo(@"^(creates\ a\ todo)$");
    }

    #endregion

    #region Multiple Specs

    [Test]
    public async Task BuildFilterPattern_MultipleSpecs_ReturnsAlternation()
    {
        var specs = new List<SpecChange>
        {
            new("creates a todo", ["TodoService"], SpecChangeType.Added),
            new("deletes a todo", ["TodoService"], SpecChangeType.Modified),
            new("updates a todo", ["TodoService"], SpecChangeType.Added)
        };

        var pattern = WatchCommand.BuildFilterPattern(specs);

        await Assert.That(pattern).IsEqualTo(@"^(creates\ a\ todo|deletes\ a\ todo|updates\ a\ todo)$");
    }

    #endregion

    #region Special Characters

    [Test]
    public async Task BuildFilterPattern_SpecialRegexCharacters_AreEscaped()
    {
        var specs = new List<SpecChange>
        {
            new("handles [brackets] and (parens)", ["Test"], SpecChangeType.Added)
        };

        var pattern = WatchCommand.BuildFilterPattern(specs);

        // Regex.Escape converts [ to \[, ( to \(, ) to \), spaces to \, but NOT ]
        await Assert.That(pattern).IsEqualTo(@"^(handles\ \[brackets]\ and\ \(parens\))$");
    }

    [Test]
    public async Task BuildFilterPattern_Dots_AreEscaped()
    {
        var specs = new List<SpecChange>
        {
            new("returns 3.14", ["Math"], SpecChangeType.Added)
        };

        var pattern = WatchCommand.BuildFilterPattern(specs);

        await Assert.That(pattern).IsEqualTo(@"^(returns\ 3\.14)$");
    }

    [Test]
    public async Task BuildFilterPattern_Asterisks_AreEscaped()
    {
        var specs = new List<SpecChange>
        {
            new("matches *anything*", ["Glob"], SpecChangeType.Added)
        };

        var pattern = WatchCommand.BuildFilterPattern(specs);

        await Assert.That(pattern).IsEqualTo(@"^(matches\ \*anything\*)$");
    }

    [Test]
    public async Task BuildFilterPattern_PlusSign_IsEscaped()
    {
        var specs = new List<SpecChange>
        {
            new("adds 1+1", ["Calculator"], SpecChangeType.Added)
        };

        var pattern = WatchCommand.BuildFilterPattern(specs);

        await Assert.That(pattern).IsEqualTo(@"^(adds\ 1\+1)$");
    }

    [Test]
    public async Task BuildFilterPattern_QuestionMark_IsEscaped()
    {
        var specs = new List<SpecChange>
        {
            new("is valid?", ["Validator"], SpecChangeType.Added)
        };

        var pattern = WatchCommand.BuildFilterPattern(specs);

        await Assert.That(pattern).IsEqualTo(@"^(is\ valid\?)$");
    }

    #endregion

    #region Pattern Validity

    [Test]
    public async Task BuildFilterPattern_ResultIsValidRegex()
    {
        var specs = new List<SpecChange>
        {
            new("test with [special] chars (here)", ["Test"], SpecChangeType.Added),
            new("another.test", ["Test"], SpecChangeType.Modified)
        };

        var pattern = WatchCommand.BuildFilterPattern(specs);

        // Should not throw when compiled as regex
        var regex = new System.Text.RegularExpressions.Regex(pattern);
        await Assert.That(regex).IsNotNull();
    }

    [Test]
    public async Task BuildFilterPattern_PatternMatchesExactDescriptions()
    {
        // Use descriptions without spaces to avoid escaping complexity
        var specs = new List<SpecChange>
        {
            new("creates-todo", ["TodoService"], SpecChangeType.Added),
            new("deletes-todo", ["TodoService"], SpecChangeType.Modified)
        };

        var pattern = WatchCommand.BuildFilterPattern(specs);
        var regex = new System.Text.RegularExpressions.Regex(pattern);

        // Should match exact descriptions
        await Assert.That(regex.IsMatch("creates-todo")).IsTrue();
        await Assert.That(regex.IsMatch("deletes-todo")).IsTrue();

        // Should NOT match partial or different strings
        await Assert.That(regex.IsMatch("creates")).IsFalse();
        await Assert.That(regex.IsMatch("creates-todo-item")).IsFalse();
        await Assert.That(regex.IsMatch("updates-todo")).IsFalse();
    }

    #endregion
}
