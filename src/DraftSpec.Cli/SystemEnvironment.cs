namespace DraftSpec.Cli;

/// <summary>
/// Production implementation using System.Environment and System.IO.
/// </summary>
public class SystemEnvironment : IEnvironment
{
    public string CurrentDirectory => Directory.GetCurrentDirectory();
    public string NewLine => Environment.NewLine;
}
