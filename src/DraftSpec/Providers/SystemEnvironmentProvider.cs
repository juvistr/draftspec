namespace DraftSpec.Providers;

/// <summary>
/// Default implementation that delegates to System.Environment.
/// </summary>
public sealed class SystemEnvironmentProvider : IEnvironmentProvider
{
    /// <summary>
    /// Singleton instance for use throughout the application.
    /// </summary>
    public static SystemEnvironmentProvider Instance { get; } = new();

    private SystemEnvironmentProvider() { }

    /// <inheritdoc />
    public string? GetEnvironmentVariable(string variable)
    {
        return System.Environment.GetEnvironmentVariable(variable);
    }
}
