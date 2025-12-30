using DraftSpec.Cli;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock console for unit testing.
/// Captures all output, warnings, errors, and tracks Clear() calls.
/// </summary>
public class MockConsole : IConsole
{
    private readonly List<string> _output = [];
    private readonly List<string> _allOutput = [];  // Never cleared by Clear()
    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];

    /// <summary>
    /// Gets output currently visible (cleared by Clear()).
    /// </summary>
    public string Output => string.Join("", _output);

    /// <summary>
    /// Gets all output ever written, regardless of Clear() calls.
    /// Use this to verify transient messages that appear before Clear().
    /// </summary>
    public string AllOutput => string.Join("", _allOutput);

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

    /// <summary>
    /// Gets the raw warning lines.
    /// </summary>
    public IReadOnlyList<string> WarningLines => _warnings;

    /// <summary>
    /// Gets how many times Clear() was called.
    /// </summary>
    public int ClearCallCount { get; private set; }

    public void Write(string text)
    {
        _output.Add(text);
        _allOutput.Add(text);
    }

    public void WriteLine(string text)
    {
        var line = text + "\n";
        _output.Add(line);
        _allOutput.Add(line);
    }

    public void WriteLine()
    {
        _output.Add("\n");
        _allOutput.Add("\n");
    }

    public ConsoleColor ForegroundColor { get; set; }

    public void ResetColor() { }

    public void Clear()
    {
        ClearCallCount++;
        _output.Clear();
    }

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

    /// <summary>
    /// Resets all captured output and counters (including AllOutput).
    /// </summary>
    public void Reset()
    {
        _output.Clear();
        _allOutput.Clear();
        _errors.Clear();
        _warnings.Clear();
        ClearCallCount = 0;
    }
}
