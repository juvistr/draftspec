using DraftSpec.Middleware;

namespace DraftSpec.Tests.Infrastructure.Builders;

/// <summary>
/// Extension methods for <see cref="SpecContextBuilder"/> to provide additional convenience.
/// </summary>
public static class SpecContextBuilderExtensions
{
    /// <summary>
    /// Creates a <see cref="SpecExecutionContext"/> from a built <see cref="SpecContext"/>.
    /// </summary>
    /// <param name="context">The spec context.</param>
    /// <param name="specIndex">The index of the spec to create an execution context for. Defaults to 0.</param>
    /// <param name="hasFocused">Whether focus mode is active. Defaults to false.</param>
    /// <returns>A new <see cref="SpecExecutionContext"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the context has no specs or the index is out of range.</exception>
    public static SpecExecutionContext ToExecutionContext(
        this SpecContext context,
        int specIndex = 0,
        bool hasFocused = false)
    {
        if (context.Specs.Count == 0)
        {
            throw new InvalidOperationException("Context has no specs. Add at least one spec before creating an execution context.");
        }

        if (specIndex < 0 || specIndex >= context.Specs.Count)
        {
            throw new InvalidOperationException($"Spec index {specIndex} is out of range. Context has {context.Specs.Count} spec(s).");
        }

        return new SpecExecutionContext
        {
            Spec = context.Specs[specIndex],
            Context = context,
            ContextPath = BuildContextPath(context),
            HasFocused = hasFocused || context.HasFocusedDescendants
        };
    }

    /// <summary>
    /// Builds the context path by traversing from the context to its root.
    /// </summary>
    private static IReadOnlyList<string> BuildContextPath(SpecContext context)
    {
        var path = new List<string>();
        var current = context;

        while (current != null)
        {
            path.Insert(0, current.Description);
            current = current.Parent;
        }

        return path;
    }
}
