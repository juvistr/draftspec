namespace DraftSpec.Providers;

/// <summary>
/// Abstraction for accessing environment variables.
/// Enables testing without modifying actual environment variables.
/// </summary>
public interface IEnvironmentProvider
{
    /// <summary>
    /// Gets the value of an environment variable.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    /// <returns>The value, or null if the variable is not set.</returns>
    string? GetEnvironmentVariable(string variable);
}
