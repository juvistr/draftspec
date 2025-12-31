namespace DraftSpec.Cli;

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
