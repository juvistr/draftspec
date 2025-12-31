namespace DraftSpec.Cli;

/// <summary>
/// Writes CLI usage/help information.
/// </summary>
public interface IUsageWriter
{
    /// <summary>
    /// Shows usage information with optional error message.
    /// </summary>
    /// <param name="errorMessage">Optional error message to display before usage.</param>
    /// <returns>Exit code (1 if error, 0 otherwise).</returns>
    int Show(string? errorMessage = null);
}
