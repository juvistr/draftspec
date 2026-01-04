namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IPathComparer for testing.
/// </summary>
public sealed class MockPathComparer : IPathComparer
{
    /// <inheritdoc />
    public StringComparison Comparison { get; private set; } = StringComparison.Ordinal;

    /// <inheritdoc />
    public StringComparer Comparer { get; private set; } = StringComparer.Ordinal;

    /// <summary>
    /// Configures this mock for Windows-style case-insensitive path comparison.
    /// </summary>
    public MockPathComparer WithWindows()
    {
        Comparison = StringComparison.OrdinalIgnoreCase;
        Comparer = StringComparer.OrdinalIgnoreCase;
        return this;
    }

    /// <summary>
    /// Configures this mock for Unix-style case-sensitive path comparison.
    /// </summary>
    public MockPathComparer WithUnix()
    {
        Comparison = StringComparison.Ordinal;
        Comparer = StringComparer.Ordinal;
        return this;
    }

    /// <summary>
    /// Configures this mock with specific comparison values.
    /// </summary>
    public MockPathComparer WithComparison(StringComparison comparison)
    {
        Comparison = comparison;
        Comparer = comparison switch
        {
            StringComparison.Ordinal => StringComparer.Ordinal,
            StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
            StringComparison.CurrentCulture => StringComparer.CurrentCulture,
            StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
            StringComparison.InvariantCulture => StringComparer.InvariantCulture,
            StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
            _ => StringComparer.Ordinal
        };
        return this;
    }
}
