namespace DraftSpec.Cli;

/// <summary>
/// Abstraction for environment-related operations.
/// Enables testing code that depends on environment state.
/// </summary>
public interface IEnvironment
{
    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    string CurrentDirectory { get; }

    /// <summary>
    /// Gets the newline string for the current platform.
    /// </summary>
    string NewLine { get; }
}

/// <summary>
/// Production implementation using System.Environment and System.IO.
/// </summary>
public class SystemEnvironment : IEnvironment
{
    public string CurrentDirectory => Directory.GetCurrentDirectory();
    public string NewLine => Environment.NewLine;
}
