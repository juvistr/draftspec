namespace DraftSpec.Cli.Commands;

public static class NewCommand
{
    public static int Execute(CliOptions options)
    {
        var name = options.SpecName;
        if (string.IsNullOrEmpty(name))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Usage: draftspec new <Name>");
            Console.ResetColor();
            return 1;
        }

        // Security: Validate spec name doesn't contain path separators
        try
        {
            PathValidator.ValidateFileName(name);
        }
        catch (ArgumentException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Invalid spec name: {ex.Message}");
            Console.ResetColor();
            return 1;
        }

        var directory = Path.GetFullPath(options.Path);

        if (!Directory.Exists(directory))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Directory not found: {directory}");
            Console.ResetColor();
            return 1;
        }

        var specHelperPath = Path.Combine(directory, "spec_helper.csx");
        if (!File.Exists(specHelperPath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: spec_helper.csx not found. Run 'draftspec init' first.");
            Console.ResetColor();
        }

        var specPath = Path.Combine(directory, $"{name}.spec.csx");
        if (File.Exists(specPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{name}.spec.csx already exists");
            Console.ResetColor();
            return 1;
        }

        var specContent = GenerateSpec(name);
        File.WriteAllText(specPath, specContent);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Created {name}.spec.csx");
        Console.ResetColor();

        return 0;
    }

    private static string GenerateSpec(string name)
    {
        return $$"""
                 #load "spec_helper.csx"
                 using static DraftSpec.Dsl;

                 describe("{{name}}", () => {
                     it("works", () => pending());
                 });
                 """;
    }
}