using DraftSpec.TestingPlatform;
using Spectre.Console;

namespace DraftSpec.Cli.Interactive;

/// <summary>
/// Interactive spec selector using Spectre.Console's MultiSelectionPrompt.
/// Displays specs in a list with multi-select capabilities.
/// </summary>
public sealed class InteractiveSpecSelector : ISpecSelector
{
    private readonly IAnsiConsole _console;

    public InteractiveSpecSelector(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public Task<SpecSelectionResult> SelectAsync(
        IReadOnlyList<DiscoveredSpec> specs,
        CancellationToken ct = default)
    {
        if (specs.Count == 0)
        {
            return Task.FromResult(SpecSelectionResult.Success([], [], 0));
        }

        // Check if terminal supports interactivity
        if (!_console.Profile.Capabilities.Interactive)
        {
            throw new InvalidOperationException(
                "Interactive mode requires an interactive terminal. " +
                "Use without --interactive in non-interactive environments (CI/CD).");
        }

        var selectables = specs.Select(SelectableSpec.FromDiscoveredSpec).ToList();

        try
        {
            var prompt = new MultiSelectionPrompt<SelectableSpec>()
                .Title("[bold]Select specs to run[/]")
                .PageSize(15)
                .HighlightStyle(new Style(foreground: Color.Green))
                .InstructionsText("[grey](Space to toggle, Enter to confirm)[/]")
                .UseConverter(s => s.FormattedDisplay);

            // Group specs by their top-level context for better organization
            var grouped = selectables
                .GroupBy(s => s.ContextPath.Count > 0 ? s.ContextPath[0] : "Specs")
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var groupSpecs = group.ToList();
                if (groupSpecs.Count == 1)
                {
                    // Single spec in group - add directly
                    prompt.AddChoice(groupSpecs[0]);
                }
                else
                {
                    // Multiple specs - create a group
                    // Use the first spec as the group header (won't be selectable as a real spec)
                    prompt.AddChoiceGroup(groupSpecs[0], groupSpecs.Skip(1));
                }
            }

            var selections = _console.Prompt(prompt);

            return Task.FromResult(SpecSelectionResult.Success(
                selections.Select(s => s.Id).ToList(),
                selections.Select(s => s.DisplayName).ToList(),
                specs.Count));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("cancelled"))
        {
            // User pressed Escape or Ctrl+C
            return Task.FromResult(SpecSelectionResult.Cancel());
        }
    }
}
