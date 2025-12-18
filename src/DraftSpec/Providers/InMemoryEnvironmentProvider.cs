namespace DraftSpec.Providers;

/// <summary>
/// In-memory implementation for testing without affecting real environment variables.
/// </summary>
public sealed class InMemoryEnvironmentProvider : IEnvironmentProvider
{
    private readonly Dictionary<string, string> _variables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates an empty in-memory environment provider.
    /// </summary>
    public InMemoryEnvironmentProvider() { }

    /// <summary>
    /// Creates an in-memory environment provider with initial values.
    /// </summary>
    /// <param name="variables">Initial environment variables.</param>
    public InMemoryEnvironmentProvider(IDictionary<string, string> variables)
    {
        foreach (var kvp in variables)
        {
            _variables[kvp.Key] = kvp.Value;
        }
    }

    /// <inheritdoc />
    public string? GetEnvironmentVariable(string variable)
    {
        return _variables.TryGetValue(variable, out var value) ? value : null;
    }

    /// <summary>
    /// Sets an environment variable value.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    /// <param name="value">The value to set, or null to remove.</param>
    public void SetEnvironmentVariable(string variable, string? value)
    {
        if (value == null)
        {
            _variables.Remove(variable);
        }
        else
        {
            _variables[variable] = value;
        }
    }

    /// <summary>
    /// Clears all environment variables.
    /// </summary>
    public void Clear()
    {
        _variables.Clear();
    }
}
