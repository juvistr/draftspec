namespace DraftSpec.Cli;

/// <summary>
/// Abstraction over console I/O for testability.
/// </summary>
public interface IConsole
{
    /// <summary>
    /// Write text to the console without a newline.
    /// </summary>
    void Write(string text);

    /// <summary>
    /// Write text to the console followed by a newline.
    /// </summary>
    void WriteLine(string text);

    /// <summary>
    /// Write an empty line to the console.
    /// </summary>
    void WriteLine();

    /// <summary>
    /// Gets or sets the foreground color of the console.
    /// </summary>
    ConsoleColor ForegroundColor { get; set; }

    /// <summary>
    /// Resets the foreground color to the default.
    /// </summary>
    void ResetColor();

    /// <summary>
    /// Clears the console.
    /// </summary>
    void Clear();

    /// <summary>
    /// Write a warning message (yellow) followed by a newline.
    /// </summary>
    void WriteWarning(string text);

    /// <summary>
    /// Write a success message (green) followed by a newline.
    /// </summary>
    void WriteSuccess(string text);

    /// <summary>
    /// Write an error message (red) followed by a newline.
    /// </summary>
    void WriteError(string text);
}

/// <summary>
/// Implementation that delegates to System.Console.
/// </summary>
public class SystemConsole : IConsole
{
    public void Write(string text) => Console.Write(text);
    public void WriteLine(string text) => Console.WriteLine(text);
    public void WriteLine() => Console.WriteLine();

    public ConsoleColor ForegroundColor
    {
        get => Console.ForegroundColor;
        set => Console.ForegroundColor = value;
    }

    public void ResetColor() => Console.ResetColor();
    public void Clear() => Console.Clear();

    public void WriteWarning(string text)
    {
        ForegroundColor = ConsoleColor.Yellow;
        WriteLine(text);
        ResetColor();
    }

    public void WriteSuccess(string text)
    {
        ForegroundColor = ConsoleColor.Green;
        WriteLine(text);
        ResetColor();
    }

    public void WriteError(string text)
    {
        ForegroundColor = ConsoleColor.Red;
        WriteLine(text);
        ResetColor();
    }
}
