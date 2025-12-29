using DraftSpec.Cli;

namespace DraftSpec.Tests.TestHelpers;

/// <summary>
/// Mock console for unit testing.
/// Captures all output for verification.
/// </summary>
public class MockConsole : IConsole
{
    private readonly List<string> _output = [];
    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];

    /// <summary>
    /// Gets all output written to the console.
    /// </summary>
    public string Output => string.Join("", _output);

    /// <summary>
    /// Gets all errors written to the console.
    /// </summary>
    public string Errors => string.Join("", _errors);

    /// <summary>
    /// Gets all warnings written to the console.
    /// </summary>
    public string Warnings => string.Join("", _warnings);

    /// <summary>
    /// Gets the raw output lines.
    /// </summary>
    public IReadOnlyList<string> OutputLines => _output;

    /// <summary>
    /// Gets the raw error lines.
    /// </summary>
    public IReadOnlyList<string> ErrorLines => _errors;

    public void Write(string text) => _output.Add(text);
    public void WriteLine(string text) => _output.Add(text + "\n");
    public void WriteLine() => _output.Add("\n");

    public ConsoleColor ForegroundColor { get; set; }

    public void ResetColor() { }
    public void Clear() { }

    public void WriteWarning(string text)
    {
        _warnings.Add(text + "\n");
        WriteLine(text);
    }

    public void WriteSuccess(string text) => WriteLine(text);

    public void WriteError(string text)
    {
        _errors.Add(text + "\n");
        WriteLine(text);
    }
}
