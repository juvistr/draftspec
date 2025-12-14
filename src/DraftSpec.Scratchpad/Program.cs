using DraftSpec;
using DraftSpec.Scratchpad;

var spec = new PatientRecordSpec();
var runner = new SpecRunner();
var results = runner.Run(spec);

Console.WriteLine();
Console.WriteLine("DraftSpec Results");
Console.WriteLine(new string('=', 50));
Console.WriteLine();

// Track printed context paths to avoid repetition
var printedPaths = new HashSet<string>();

foreach (var result in results)
{
    // Print any new context segments
    for (int i = 0; i < result.ContextPath.Count; i++)
    {
        var pathKey = string.Join("/", result.ContextPath.Take(i + 1));
        if (!printedPaths.Contains(pathKey))
        {
            printedPaths.Add(pathKey);
            var indent = new string(' ', i * 2);
            Console.WriteLine($"{indent}{result.ContextPath[i]}");
        }
    }

    // Print the spec with status
    var specIndent = new string(' ', result.ContextPath.Count * 2);
    var (symbol, color) = result.Status switch
    {
        SpecStatus.Passed => ("✓", ConsoleColor.Green),
        SpecStatus.Failed => ("✗", ConsoleColor.Red),
        SpecStatus.Pending => ("○", ConsoleColor.Yellow),
        SpecStatus.Skipped => ("-", ConsoleColor.DarkGray),
        _ => ("?", ConsoleColor.White)
    };

    Console.ForegroundColor = color;
    Console.Write($"{specIndent}{symbol} ");
    Console.ResetColor();
    Console.WriteLine(result.Spec.Description);

    if (result.Status == SpecStatus.Failed && result.Exception != null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{specIndent}  {result.Exception.Message}");
        Console.ResetColor();
    }
}

Console.WriteLine();
Console.WriteLine(new string('-', 50));

var passed = results.Count(r => r.Status == SpecStatus.Passed);
var failed = results.Count(r => r.Status == SpecStatus.Failed);
var pending = results.Count(r => r.Status == SpecStatus.Pending);
var skipped = results.Count(r => r.Status == SpecStatus.Skipped);

Console.Write($"{results.Count} specs: ");
if (passed > 0) { Console.ForegroundColor = ConsoleColor.Green; Console.Write($"{passed} passed"); Console.ResetColor(); }
if (failed > 0) { Console.Write(", "); Console.ForegroundColor = ConsoleColor.Red; Console.Write($"{failed} failed"); Console.ResetColor(); }
if (pending > 0) { Console.Write(", "); Console.ForegroundColor = ConsoleColor.Yellow; Console.Write($"{pending} pending"); Console.ResetColor(); }
if (skipped > 0) { Console.Write(", "); Console.ForegroundColor = ConsoleColor.DarkGray; Console.Write($"{skipped} skipped"); Console.ResetColor(); }
Console.WriteLine();
