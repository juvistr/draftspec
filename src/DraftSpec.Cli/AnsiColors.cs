using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace DraftSpec.Cli;

/// <summary>
/// ANSI escape codes for terminal colors.
/// </summary>
public static class AnsiColors
{
    public const string Reset = "\x1b[0m";
    public const string Red = "\x1b[31m";
    public const string Green = "\x1b[32m";
    public const string Yellow = "\x1b[33m";
    public const string Cyan = "\x1b[36m";
    public const string Dim = "\x1b[2m";
    public const string Bold = "\x1b[1m";
}
