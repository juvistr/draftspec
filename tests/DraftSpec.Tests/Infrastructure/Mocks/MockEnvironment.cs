namespace DraftSpec.Tests.Infrastructure.Mocks;

using DraftSpec.Cli;

/// <summary>
/// Mock environment for unit testing.
/// Defaults to actual current directory for compatibility with Path.GetFullPath.
/// </summary>
public class MockEnvironment : IEnvironment
{
    /// <summary>
    /// Gets or sets the current directory.
    /// Defaults to actual CWD for compatibility with relative path resolution.
    /// </summary>
    public string CurrentDirectory { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// Gets or sets the newline string.
    /// </summary>
    public string NewLine { get; set; } = Environment.NewLine;
}
